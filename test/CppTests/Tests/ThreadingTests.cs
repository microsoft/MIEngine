// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DebuggerTesting;
using DebuggerTesting.Compilation;
using DebuggerTesting.OpenDebug;
using DebuggerTesting.OpenDebug.CrossPlatCpp;
using DebuggerTesting.OpenDebug.Events;
using DebuggerTesting.OpenDebug.Extensions;
using DebuggerTesting.Ordering;
using Xunit;
using Xunit.Abstractions;

namespace CppTests.Tests
{

    [TestCaseOrderer(DependencyTestOrderer.TypeName, DependencyTestOrderer.AssemblyName)]
    public class ThreadingTests : TestBase
    {
        #region Constructor

        public ThreadingTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        [Theory]
        [RequiresTestSettings]
        public void CompileKitchenSinkForThreading(ITestSettings settings)
        {
            this.TestPurpose("Compiles the kitchen sink debuggee for threading.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.OpenAndCompile(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Threading);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForThreading))]
        [RequiresTestSettings]
        [UnsupportedDebugger(SupportedDebugger.Gdb_MinGW | SupportedDebugger.Gdb_Gnu, SupportedArchitecture.x86)]
        // TODO: Re-enable for vsdbg
        [UnsupportedDebugger(SupportedDebugger.VsDbg, SupportedArchitecture.x64)]
        public void ThreadingBasic(ITestSettings settings)
        {
            this.TestPurpose("Test basic multithreading scenario.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Threading);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Launching debuggee. Run until multiple threads are running.");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fThreading");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Threading, 37));

                runner.Expects.HitBreakpointEvent()
                              .AfterConfigurationDone();

                IEnumerable<IThreadInfo> threads = runner.GetThreads();
                List<string> loopCounts = new List<string>();

                this.Comment("Inspect threads and find 'loopCount' variable on each worker thread.");
                this.WriteLine("Threads:");
                foreach (var threadInfo in threads)
                {
                    IThreadInspector threadInspector = threadInfo.GetThreadInspector();

                    // Don't look at main thread, just workers
                    if (threadInspector.ThreadId == runner.StoppedThreadId)
                        continue;

                    this.Comment("Thread '{0}', Id: {1}".FormatInvariantWithArgs(threadInfo.Name, threadInspector.ThreadId));
                    IFrameInspector threadLoopFrame = threadInspector.Stack.FirstOrDefault(s => s.Name.Contains("ThreadLoop"));

                    // Fail the test if the ThreadLoop frame could not be found
                    if (threadLoopFrame == null)
                    {
                        this.WriteLine("This thread's stack did not contain a frame with 'ThreadLoop'");
                        this.WriteLine("Stack Trace:");
                        foreach (var frame in threadInspector.Stack)
                        {
                            this.WriteLine(frame.Name);
                        }
                        continue;
                    }

                    string variables = threadLoopFrame.Variables.ToReadableString();
                    this.WriteLine("Variables in 'ThreadLoop' frame:");
                    this.WriteLine(variables);

                    // Put the different loopCounts in a list, so they can be verified order agnostic
                    string loopCountValue = threadLoopFrame.GetVariable("loopCount").Value;
                    this.WriteLine("loopCount = {0}", loopCountValue);
                    loopCounts.Add(loopCountValue);
                }

                //Verify all the worker threads were observed
                Assert.True(loopCounts.Contains("0"), "Could not find thread with loop count 0");
                Assert.True(loopCounts.Contains("1"), "Could not find thread with loop count 1");
                Assert.True(loopCounts.Contains("2"), "Could not find thread with loop count 2");
                Assert.True(loopCounts.Contains("3"), "Could not find thread with loop count 3");
                Assert.True(4 == loopCounts.Count, "Expected to find 4 threads, but found " + loopCounts.Count.ToString(CultureInfo.InvariantCulture));

                this.Comment("Run to end.");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForThreading))]
        [RequiresTestSettings]
        public void ThreadingBreakpoint(ITestSettings settings)
        {
            this.TestPurpose("Test breakpoint on multiple threads.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Threading);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Launching debuggee. Set breakpoint in worker thread code.");
                runner.Launch(settings.DebuggerSettings, true, debuggee, "-fThreading");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Threading, 16));

                // Turned on stop at entry, so should stop before anything is executed.
                // On Concord, this is a reason of "entry" versus "step" when it is hit.
                if (settings.DebuggerSettings.DebuggerType == SupportedDebugger.VsDbg)
                {
                    runner.Expects.HitEntryEvent().AfterConfigurationDone();
                }
                else
                {
                    runner.Expects.HitStepEvent()
                                  .AfterConfigurationDone();
                }
                // Since there are 4 worker threads, expect to hit the
                // breakpoint 4 times, once on each thread.
                for (int i = 1; i <= 4; i++)
                {
                    StoppedEvent breakpointEvent = new StoppedEvent(StoppedReason.Breakpoint, SinkHelper.Threading, 16);
                    this.Comment("Run until breakpoint #{0}.", i);
                    runner.Expects.Event(breakpointEvent).AfterContinue();
                    this.WriteLine("Stopped on thread: {0}", breakpointEvent.ThreadId);

                    this.WriteLine("Ensure stopped thread exists in ThreadList");
                    IEnumerable<IThreadInfo> threadInfo = runner.GetThreads();
                    Assert.True(threadInfo.Any(thread => thread.Id == breakpointEvent.ThreadId), string.Format(CultureInfo.CurrentCulture, "ThreadId {0} should exist in ThreadList", breakpointEvent.ThreadId));
                }
                
                this.Comment("Run to end.");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }
    }
}
