// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using DebuggerTesting;
using DebuggerTesting.Compilation;
using DebuggerTesting.OpenDebug;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.OpenDebug.CrossPlatCpp;
using DebuggerTesting.OpenDebug.Events;
using DebuggerTesting.OpenDebug.Extensions;
using DebuggerTesting.Ordering;
using Xunit;
using Xunit.Abstractions;

namespace CppTests.Tests
{
    [TestCaseOrderer(DependencyTestOrderer.TypeName, DependencyTestOrderer.AssemblyName)]
    public class ExecutionTests : TestBase
    {
        #region Constructor

        public ExecutionTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        [Theory]
        [RequiresTestSettings]
        public void CompileKitchenSinkForExecution(ITestSettings settings)
        {
            this.TestPurpose("Compiles the kitchen sink debuggee for execution tests.");
            this.WriteSettings(settings);
            IDebuggee debuggee = SinkHelper.OpenAndCompile(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Execution);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForExecution))]
        [RequiresTestSettings]
        public void ExecutionStepBasic(ITestSettings settings)
        {
            this.TestPurpose("Verify basic step in/over/out should work during debugging");
            this.WriteSettings(settings);

            this.Comment("Open the kitchen sink debuggee for execution tests.");
            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Execution);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set initial source breakpoints");
                SourceBreakpoints bps = debuggee.Breakpoints(SinkHelper.Main, 21, 33);
                runner.SetBreakpoints(bps);

                this.Comment("Launch and run until hit the first entry");
                runner.Expects.HitBreakpointEvent(SinkHelper.Main, 21).AfterConfigurationDone();

                this.Comment("Step over the function");
                runner.Expects.HitStepEvent(SinkHelper.Main, 23).AfterStepOver();

                this.Comment("Continue to hit the second entry");
                runner.Expects.HitBreakpointEvent(SinkHelper.Main, 33).AfterContinue();

                this.Comment("Step in the function");
                runner.ExpectStepAndStepToTarget(SinkHelper.Feature, startLine: 19, targetLine: 20).AfterStepIn();

                this.Comment("Step over the function");
                runner.Expects.HitStepEvent(SinkHelper.Feature, 21).AfterStepOver();
                runner.Expects.HitStepEvent(SinkHelper.Feature, 22).AfterStepOver();

                this.Comment("Step in the function");
                runner.ExpectStepAndStepToTarget(SinkHelper.Calling, startLine: 47, targetLine: 48).AfterStepIn();

                this.Comment("Step out the function");
                runner.ExpectStepAndStepToTarget(SinkHelper.Feature, startLine: 22, targetLine: 23).AfterStepOut();

                this.Comment("Continue running at the end of application");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                this.Comment("Verify debugger and debuggee closed");
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForExecution))]
        [RequiresTestSettings]
        public void ExecutionStepRecursiveCall(ITestSettings settings)
        {
            this.TestPurpose("Verify steps should work when debugging recursive call");
            this.WriteSettings(settings);

            this.Comment("Open the kitchen sink debuggee for execution tests.");
            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Execution);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set initial function breakpoints");
                FunctionBreakpoints funcBp = new FunctionBreakpoints("Calling::CoreRun()");
                runner.SetFunctionBreakpoints(funcBp);

                this.Comment("Launch and run until hit function breakpoint in the entry of calling");
                runner.ExpectBreakpointAndStepToTarget(SinkHelper.Calling, startLine: 47, targetLine: 48).AfterConfigurationDone();

                this.Comment("Step over to go to the entry of recursive call");
                runner.Expects.HitStepEvent(SinkHelper.Calling, 49).AfterStepOver();

                this.Comment("Step in the recursive call");
                runner.ExpectStepAndStepToTarget(SinkHelper.Calling, startLine: 25, targetLine: 26).AfterStepIn();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    this.Comment("Set count = 2");
                    IFrameInspector currentFrame = threadInspector.Stack.First();
                    currentFrame.GetVariable("count").Value = "2";

                    this.Comment("Verify there is only one 'recursiveCall' frames");
                    threadInspector.AssertStackFrameNames(true, "recursiveCall.*", "Calling::CoreRun.*", "Feature::Run.*", "main.*");
                }

                this.Comment("Step over and then step in the recursive call once again");
                runner.Expects.HitStepEvent(SinkHelper.Calling, 29).AfterStepOver();
                runner.ExpectStepAndStepToTarget(SinkHelper.Calling, startLine: 25, targetLine: 26).AfterStepIn();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    this.Comment("Verify there are two 'recursiveCall' frames");
                    threadInspector.AssertStackFrameNames(true, "recursiveCall.*", "recursiveCall.*", "Calling::CoreRun.*", "Feature::Run.*", "main.*");

                    this.Comment("Set a source breakpoint in recursive call");
                    SourceBreakpoints srcBp = debuggee.Breakpoints(SinkHelper.Calling, 26);
                    runner.SetBreakpoints(srcBp);
                }

                this.Comment("Step over the recursive call and hit the source breakpoint");
                runner.Expects.HitStepEvent(SinkHelper.Calling, 29).AfterStepOver();
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 26).AfterStepOver();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    this.Comment("Verify there are three 'recursiveCall' frames");
                    threadInspector.AssertStackFrameNames(true, "recursiveCall.*", "recursiveCall.*", "recursiveCall.*", "Calling::CoreRun.*", "Feature::Run.*", "main.*");
                }

                this.Comment("Try to step out twice from recursive call");
                runner.ExpectStepAndStepToTarget(SinkHelper.Calling, 29, 30).AfterStepOut();
                runner.ExpectStepAndStepToTarget(SinkHelper.Calling, 29, 30).AfterStepOut();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    this.Comment("Verify 'recursiveCall' return back only one frame");
                    threadInspector.AssertStackFrameNames(true, "recursiveCall.*", "Calling::CoreRun.*", "Feature::Run.*", "main.*");
                }

                this.Comment("Step over from recursive call");
                runner.ExpectStepAndStepToTarget(SinkHelper.Calling, 49, 50).AfterStepOver();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    this.Comment("Verify there is not 'recursiveCall' frame");
                    threadInspector.AssertStackFrameNames(true, "Calling::CoreRun.*", "Feature::Run.*", "main.*");
                }

                this.Comment("Verify stop debugging");
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForExecution))]
        [RequiresTestSettings]
        // TODO: Re-enable for VsDbg and Gdb_Gnu
        [UnsupportedDebugger(SupportedDebugger.Gdb_Cygwin | SupportedDebugger.Gdb_MinGW | SupportedDebugger.Gdb_Gnu | SupportedDebugger.Lldb | SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        public void ExecutionAsyncBreak(ITestSettings settings)
        {
            this.TestPurpose("Verify break all should work run function");
            this.WriteSettings(settings);

            this.Comment("Open the kitchen sink debuggee for execution tests.");
            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Execution);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fNonTerminating", "-fCalling");

                this.Comment("Try to break all");
                StoppedEvent breakAllEvent = new StoppedEvent(StoppedReason.Pause);
                runner.Expects.Event(breakAllEvent).AfterAsyncBreak();

                this.WriteLine("Break all stopped on:");
                this.WriteLine(breakAllEvent.ActualEvent.ToString());

                this.Comment("Set a breakpoint while breaking code");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.NonTerminating, 28));

                this.Comment("Start running after the async break and then hit the breakpoint");
                runner.Expects.HitBreakpointEvent(SinkHelper.NonTerminating, 28).AfterContinue();

                this.Comment("Evaluate the shouldExit member to true to stop the infinite loop.");
                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector firstFrame = threadInspector.Stack.First();
                    this.WriteLine(firstFrame.ToString());
                    firstFrame.GetVariable("shouldExit").Value = "true";
                }

                this.Comment("Continue running at the end of application");
                runner.Expects.ExitedEvent().TerminatedEvent().AfterContinue();

                this.Comment("Verify debugger and debuggee closed");
                runner.DisconnectAndVerify();
            }
        }
    }
}
