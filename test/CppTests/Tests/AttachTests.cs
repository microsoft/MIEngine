// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Linq;
using DebuggerTesting;
using DebuggerTesting.Compilation;
using DebuggerTesting.OpenDebug;
using DebuggerTesting.OpenDebug.CrossPlatCpp;
using DebuggerTesting.OpenDebug.Events;
using DebuggerTesting.OpenDebug.Extensions;
using DebuggerTesting.Ordering;
using DebuggerTesting.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace CppTests.Tests
{
    [TestCaseOrderer(DependencyTestOrderer.TypeName, DependencyTestOrderer.AssemblyName)]
    public class AttachTests : TestBase
    {
        #region Constructor

        public AttachTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        [Theory]
        [RequiresTestSettings]
        public void CompileKitchenSinkForAttach(ITestSettings settings)
        {
            this.TestPurpose("Compiles the kitchen sink debuggee for attach.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.OpenAndCompile(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Attach);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForAttach))]
        [RequiresTestSettings]
        // TODO: Re-enable for vsdbg
        [UnsupportedDebugger(SupportedDebugger.Gdb_Cygwin | SupportedDebugger.Gdb_MinGW | SupportedDebugger.Lldb | SupportedDebugger.VsDbg, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void AttachAsyncBreak(ITestSettings settings)
        {
            this.TestPurpose("Verifies attach and that breakpoints can be set from break mode.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Attach);
            Process debuggeeProcess = debuggee.Launch("-fNonTerminating", "-fCalling");

            using (ProcessHelper.ProcessCleanup(this, debuggeeProcess))
            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Attach to debuggee");
                runner.Attach(settings.DebuggerSettings, debuggeeProcess);
                runner.ConfigurationDone();

                this.Comment("Attempt to break all");
                StoppedEvent breakAllEvent = new StoppedEvent(StoppedReason.Pause);
                runner.Expects.Event(breakAllEvent)
                              .AfterAsyncBreak();

                this.WriteLine("Break all stopped on:");
                this.WriteLine(breakAllEvent.ActualEvent.ToString());

                this.Comment("Set breakpoint while breaking code.");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.NonTerminating, 28));

                this.Comment("Start running after the async break (since we have no idea where we are) and then hit the breakpoint");
                runner.Expects.HitBreakpointEvent(SinkHelper.NonTerminating, 28)
                              .AfterContinue();

                this.Comment("Evaluate the shouldExit member to true to stop the infinite loop.");
                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector firstFrame = threadInspector.Stack.First();
                    this.WriteLine(firstFrame.ToString());
                    firstFrame.GetVariable("shouldExitLocal").Value = "true";
                }

                this.Comment("Continue until debuggee exists");
                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();

                this.Comment("Verify debugger and debuggee closed");
                runner.DisconnectAndVerify();
                Assert.True(debuggeeProcess.HasExited, "Debuggee still running.");
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForAttach))]
        [RequiresTestSettings]
        // TODO: Re-enable for vsdbg
        [UnsupportedDebugger(SupportedDebugger.Gdb_Cygwin | SupportedDebugger.Gdb_MinGW | SupportedDebugger.Lldb | SupportedDebugger.VsDbg, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void Detach(ITestSettings settings)
        {
            this.TestPurpose("Verify debugger can detach and reattach to a debuggee.");
            this.WriteSettings(settings);

            this.Comment("Starting debuggee");
            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Attach);
            Process debuggeeProcess = debuggee.Launch("-fNonTerminating", "-fCalling");

            using (ProcessHelper.ProcessCleanup(this, debuggeeProcess))
            {
                using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
                {
                    this.Comment("Attaching first time");
                    runner.Attach(settings.DebuggerSettings, debuggeeProcess);
                    runner.ConfigurationDone();

                    this.Comment("Attempt to break all");
                    StoppedEvent breakAllEvent = new StoppedEvent(StoppedReason.Pause);
                    runner.Expects.Event(breakAllEvent)
                                  .AfterAsyncBreak();

                    this.WriteLine("Break all stopped on:");
                    this.WriteLine(breakAllEvent.ActualEvent.ToString());

                    this.Comment("Detach then verify debugger closed");
                    runner.DisconnectAndVerify();
                }

                Assert.False(debuggeeProcess.HasExited, "Debuggee should still be running.");

                using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
                {
                    this.Comment("Attaching second time");
                    runner.Attach(settings.DebuggerSettings, debuggeeProcess);
                    runner.ConfigurationDone();

                    this.Comment("Detach then verify debugger closed");
                    runner.DisconnectAndVerify();
                }
            }
        }
    }
}
