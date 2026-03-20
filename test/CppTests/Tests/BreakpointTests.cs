// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
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
    public class BreakpointTests : TestBase
    {
        #region Constructor

        public BreakpointTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region Methods

        [Theory]
        [RequiresTestSettings]
        public void CompileKitchenSinkForBreakpointTests(ITestSettings settings)
        {
            this.TestPurpose("Compiles the kitchen sink debuggee.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.OpenAndCompile(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void LineBreakpointsBasic(ITestSettings settings)
        {
            this.TestPurpose("Tests basic operation of line breakpoints");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                // These keep track of all the breakpoints in a source file
                SourceBreakpoints argumentsBreakpoints = debuggee.Breakpoints(SinkHelper.Arguments, 23);
                SourceBreakpoints mainBreakpoints = debuggee.Breakpoints(SinkHelper.Main, 33);

                SourceBreakpoints callingBreakpoints = debuggee.Breakpoints(SinkHelper.Calling, 48);

                // A bug in clang causes several breakpoint hits in a constructor
                // See: https://llvm.org/bugs/show_bug.cgi?id=30620
                if (settings.CompilerSettings.CompilerType != SupportedCompiler.ClangPlusPlus)
                {
                    callingBreakpoints.Add(6);
                }

                this.Comment("Set initial breakpoints");
                runner.SetBreakpoints(argumentsBreakpoints);
                runner.SetBreakpoints(mainBreakpoints);
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Launch and run until first breakpoint");
                runner.Expects.HitBreakpointEvent(SinkHelper.Arguments, 23)
                              .AfterConfigurationDone();

                // A bug in clang causes several breakpoint hits in a constructor
                // See: https://llvm.org/bugs/show_bug.cgi?id=30620
                if (settings.CompilerSettings.CompilerType != SupportedCompiler.ClangPlusPlus)
                {
                    this.Comment("Continue until second initial breakpoint");
                    runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 6).AfterContinue();
                }

                this.Comment("Disable third initial breakpoint");
                mainBreakpoints.Remove(33);
                runner.SetBreakpoints(mainBreakpoints);

                this.Comment("Continue, hit fourth initial breakpoint");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 48).AfterContinue();

                this.Comment("Set a breakpoint while in break mode");
                callingBreakpoints.Add(52);
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Continue until newly-added breakpoint");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 52)
                              .AfterContinue();

                this.Comment("Continue until end");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void FunctionBreakpointsBasic(ITestSettings settings)
        {
            this.TestPurpose("Tests basic operation of function breakpoints");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set initial function breakpoints");
                FunctionBreakpoints functionBreakpoints = new FunctionBreakpoints("Arguments::CoreRun", "Calling::CoreRun", "a");
                runner.SetFunctionBreakpoints(functionBreakpoints);

                this.Comment("Launch and run until first initial breakpoint");
                runner.ExpectBreakpointAndStepToTarget(SinkHelper.Arguments, startLine: 9, targetLine: 10)
                              .AfterConfigurationDone();

                this.Comment("Continue until second initial breakpoint");
                runner.ExpectBreakpointAndStepToTarget(SinkHelper.Calling, startLine: 47, targetLine: 48)
                              .AfterContinue();

                this.Comment("Remove and replace third initial function breakpoint while in break mode");
                functionBreakpoints.Remove("a");
                functionBreakpoints.Add("b()");
                runner.SetFunctionBreakpoints(functionBreakpoints);

                this.Comment("Continue until newly-added function breakpoint");
                runner.ExpectBreakpointAndStepToTarget(SinkHelper.Calling, startLine: 37, targetLine: 38)
                              .AfterContinue();

                this.Comment("Continue until end");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        [UnsupportedDebugger(SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        public void LineLogBreakpointsBasic(ITestSettings settings)
        {
            this.TestPurpose("Tests basic operation of line breakpoints with a LogPoint");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                // These keep track of all the breakpoints in a source file
                SourceBreakpoints callingBreakpoints = debuggee.Breakpoints(SinkHelper.Calling, 48);

                this.Comment("Set initial breakpoints");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Launch and run until first breakpoint");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 48)
                              .AfterConfigurationDone();

                string logMessage = "Log Message";

                this.Comment("Set a logpoint while in break mode");
                callingBreakpoints.Add(52, null, logMessage);
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Continue til end with newly-added logpoint");
                // ignoringResponseOrder: true here since sometimes the ContinuedResponse occurs after the OutputEvent and
                // DAR does not look at previous messages unless marked ignoreResponseOrder. 
                runner.Expects.OutputEvent("^" + logMessage + "\\b", CategoryValue.Console, ignoreResponseOrder: true)
                              .ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        // TODO: https://github.com/microsoft/MIEngine/issues/1170
        // - gdb_gnu
        // - lldb
        [UnsupportedDebugger(SupportedDebugger.Lldb | SupportedDebugger.Gdb_Gnu | SupportedDebugger.Gdb_Cygwin | SupportedDebugger.Gdb_MinGW, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void RunModeBreakpoints(ITestSettings settings)
        {
            this.TestPurpose("Tests setting breakpoints while in run mode");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fNonTerminating");
                runner.ConfigurationDone();

                // Wait a second to ensure the debuggee has entered run mode, then try to set a breakpoint
                Thread.Sleep(TimeSpan.FromSeconds(1));
                this.Comment("Set a function breakpoint while in run mode");
                FunctionBreakpoints functionBreakpoints = new FunctionBreakpoints("NonTerminating::DoSleep");
                runner.ExpectBreakpointAndStepToTarget(SinkHelper.NonTerminating, startLine: 37, targetLine: 38)
                              .AfterSetFunctionBreakpoints(functionBreakpoints);

                this.Comment("Remove function breakpoint");
                functionBreakpoints.Remove("NonTerminating::DoSleep");
                runner.SetFunctionBreakpoints(functionBreakpoints);

                this.Comment("Continue, set a line breakpoint while in run mode");
                runner.Continue();

                // Wait a second to ensure the debuggee has entered run mode, then try to set a breakpoint
                Thread.Sleep(TimeSpan.FromSeconds(1));
                runner.Expects.HitBreakpointEvent(SinkHelper.NonTerminating, 28)
                              .AfterSetBreakpoints(debuggee.Breakpoints(SinkHelper.NonTerminating, 28));

                this.Comment("Escape loop");
                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector firstFrame = threadInspector.Stack.First();
                    this.WriteLine(firstFrame.ToString());
                    firstFrame.GetVariable("this", "shouldExit").Value = "1";
                }

                this.Comment("Continue until end");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void BreakpointBinding(ITestSettings settings)
        {
            this.TestPurpose("Tests that breakpoints are bound to the correct locations");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee);

                SourceBreakpoints callingBreakpoints = debuggee.Breakpoints(SinkHelper.Calling, 11);
                FunctionBreakpoints functionBreakpoints = new FunctionBreakpoints("Calling::CoreRun");

                // VsDbg does not fire Breakpoint Change events when breakpoints are set.
                // Instead it sends a new breakpoint event when it is bound (after configuration done).
                bool bindsLate = (settings.DebuggerSettings.DebuggerType == SupportedDebugger.VsDbg);

                this.Comment("Set a breakpoint at a location that has no executable code, expect it to be moved to the next line");
                runner.Expects.ConditionalEvent(!bindsLate, x => x.BreakpointChangedEvent(BreakpointReason.Changed, 12))
                              .AfterSetBreakpoints(callingBreakpoints);

                this.Comment("Set a function breakpoint in a class member, expect it to be placed at the opening bracket");
                runner.Expects.ConditionalEvent(!bindsLate, x => x.FunctionBreakpointChangedEvent(BreakpointReason.Changed, startLine: 47, endLine: 48))
                              .AfterSetFunctionBreakpoints(functionBreakpoints);

                this.Comment("Set a function breakpoint in a non-member, expect it to be placed on the first line of code");
                functionBreakpoints.Add("a()");
                runner.Expects.ConditionalEvent(!bindsLate, x => x.FunctionBreakpointChangedEvent(BreakpointReason.Changed, startLine: 42, endLine: 43))
                              .AfterSetFunctionBreakpoints(functionBreakpoints);


                runner.Expects.ConditionalEvent(bindsLate, x => x.BreakpointChangedEvent(BreakpointReason.Changed, 12)
                                  .FunctionBreakpointChangedEvent(BreakpointReason.Changed, startLine: 47, endLine: 48)
                                  .FunctionBreakpointChangedEvent(BreakpointReason.Changed, startLine: 42, endLine: 43))
                              .ExitedEvent()
                              .TerminatedEvent()
                              .AfterConfigurationDone();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void DuplicateBreakpoints(ITestSettings settings)
        {
            this.TestPurpose("Tests that duplicate breakpoints are only hit once");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                // These two breakpoints should resolve to the same line - setting two breakpoints on the same
                //  line directly will throw an exception.
                SourceBreakpoints callingBreakpoints = debuggee.Breakpoints(SinkHelper.Calling, 11, 12);

                this.Comment("Set two line breakpoints that resolve to the same source location");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Set duplicate function breakpoints");
                FunctionBreakpoints functionBreakpoints = new FunctionBreakpoints("Arguments::CoreRun", "Arguments::CoreRun");
                runner.SetFunctionBreakpoints(functionBreakpoints);

                this.Comment("Run to first breakpoint");
                runner.ExpectBreakpointAndStepToTarget(SinkHelper.Arguments, startLine: 9, targetLine: 10)
                              .AfterConfigurationDone();

                this.Comment("Run to second breakpoint");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 12)
                              .AfterContinue();

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void ConditionalBreakpoints(ITestSettings settings)
        {
            this.TestPurpose("Tests that conditional breakpoints work");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a conditional line breakpoint");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, "i == 5");
                runner.SetBreakpoints(callingBreakpoints);

                // The schema also supports conditions on function breakpoints, but MIEngine or GDB doesn't seem
                //  to support that.  Plus, there's no way to do it through the VSCode UI anyway.

                this.Comment("Run to breakpoint");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                this.Comment("Verify breakpoint condition is met");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "5");
                }

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        // lldb-mi returns the condition without escaping the quotes.
        // >=breakpoint-modified,bkpt={..., cond="str == "hello, world"", ...}
        [UnsupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void ConditionalStringBreakpoints(ITestSettings settings)
        {
            this.TestPurpose("Tests that conditional breakpoints on strings work");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fExpression");

                this.Comment("Set a conditional line with string comparison breakpoint");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Expression);
                callingBreakpoints.Add(69, "str == \"hello, world\"");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to conditional breakpoint");
                runner.Expects.HitBreakpointEvent(null, 69)
                              .AfterConfigurationDone();

                // Skip verifying variable since strings result in "{ ... }"

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        // lldb-mi does not support -break-watch
        [UnsupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        [UnsupportedDebugger(SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        public void DataBreakpointTest(ITestSettings settings)
        {
            this.TestPurpose("Tests that data breakpoints work");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                SourceBreakpoints callingBreakpoints = debuggee.Breakpoints(SinkHelper.Calling, 15);
                runner.SetBreakpoints(callingBreakpoints);

                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 15)
                            .AfterConfigurationDone();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.GetVariable("total");

                    this.Comment("Get DataBreakpointInfo on 'total'");
                    var response = runner.DataBreakpointInfo("total");
                    Assert.NotNull(response?.body);
                    Assert.Contains("write", response.body.accessTypes); // Validate access type is "write"
                    Assert.Equal("When 'total' changes (8 bytes)", response.body.description, true, true, true); // Validate description matches DataBreakpointDisplayString
                    Assert.False(string.IsNullOrEmpty(response.body.dataId));
                    Assert.EndsWith("total,8", response.body.dataId); // Validate dataId matches format <Address>,<Id>,<Size>

                    this.Comment("SetDataBreakpoint on 'total' Info");
                    DataBreakpoints dataBreakpoints = new DataBreakpoints();
                    dataBreakpoints.Add(response.body.dataId);
                    runner.SetDataBreakpoints(dataBreakpoints);
                }

                this.Comment("Run to statement after data breakpoint");
                // Note this is going to be source line 15 for `i++`.
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 15)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    string value = mainFrame.GetVariable("total").Value;
                    Assert.True(double.TryParse(value, out double result));
                    Assert.Equal(1.0, result);
                }

                // Delete data breakpoint
                this.Comment("Clear data breakpoint");
                runner.SetDataBreakpoints(new DataBreakpoints());

                // Ensures that disabling the data bp works if it does not hit another
                // stopping bp event but ends the program.
                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void BreakpointSettingsVerification(ITestSettings settings)
        {
            this.TestPurpose("Tests supported breakpoint settings");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee);

                Assert.True(runner.InitializeResponse.body.supportsConditionalBreakpoints.HasValue
                    && runner.InitializeResponse.body.supportsConditionalBreakpoints.Value == true, "Conditional breakpoints should be supported");

                Assert.True(runner.InitializeResponse.body.supportsFunctionBreakpoints.HasValue
                    && runner.InitializeResponse.body.supportsFunctionBreakpoints.Value == true, "Function breakpoints should be supported");

                Assert.True(runner.InitializeResponse.body.supportsHitConditionalBreakpoints.HasValue
                    && runner.InitializeResponse.body.supportsHitConditionalBreakpoints.Value == true, "Hit conditional breakpoints should be supported");

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterConfigurationDone();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointEqual(ITestSettings settings)
        {
            this.TestPurpose("Tests that a breakpoint with a hit count condition (equal) only breaks on the Nth hit");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition = 5 inside a loop that iterates 10 times");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: "5");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should only stop on 5th hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                this.Comment("Verify that the loop variable equals 4 (0-indexed, 5th iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "4");
                }

                this.Comment("Run to completion - hit count 5 already reached, should not stop again");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointGreaterOrEqual(ITestSettings settings)
        {
            this.TestPurpose("Tests that a breakpoint with a hit count condition (>=) breaks on the Nth hit and every subsequent hit");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition >= 8 inside a loop that iterates 10 times");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: ">=8");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should stop on 8th hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                this.Comment("Verify that the loop variable equals 7 (0-indexed, 8th iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "7");
                }

                this.Comment("Continue - should stop on 9th hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                this.Comment("Verify that the loop variable equals 8 (0-indexed, 9th iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "8");
                }

                this.Comment("Continue - should stop on 10th hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                this.Comment("Verify that the loop variable equals 9 (0-indexed, 10th iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "9");
                }

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointModulo(ITestSettings settings)
        {
            this.TestPurpose("Tests that a breakpoint with a hit count condition (modulo) breaks on every Nth hit");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition %3 inside a loop that iterates 10 times");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: "%3");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should stop on 3rd hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                this.Comment("Verify that the loop variable equals 2 (0-indexed, 3rd iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "2");
                }

                this.Comment("Continue - should stop on 6th hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                this.Comment("Verify that the loop variable equals 5 (0-indexed, 6th iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "5");
                }

                this.Comment("Continue - should stop on 9th hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                this.Comment("Verify that the loop variable equals 8 (0-indexed, 9th iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "8");
                }

                this.Comment("Run to completion - no more multiples of 3 within 10 iterations");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointEqualFirst(ITestSettings settings)
        {
            this.TestPurpose("Tests that hitCondition '1' breaks on the very first hit");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition = 1 (first hit)");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: "1");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should stop on the very first hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                this.Comment("Verify i == 0 (first iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "0");
                }

                this.Comment("Run to completion - equal condition already satisfied, should not stop again");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointEqualLast(ITestSettings settings)
        {
            this.TestPurpose("Tests that hitCondition equal to the loop bound stops on the last iteration");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition = 10 (last hit in a 10-iteration loop)");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: "10");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should stop on the 10th and final hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                this.Comment("Verify i == 9 (last iteration, 0-indexed)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "9");
                }

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointEqualExceedsIterations(ITestSettings settings)
        {
            this.TestPurpose("Tests that hitCondition exceeding total iterations never stops");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition = 99 inside a loop that only iterates 10 times");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: "99");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to completion - breakpoint should never fire");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterConfigurationDone();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointGreaterOrEqualOne(ITestSettings settings)
        {
            this.TestPurpose("Tests that hitCondition '>=1' stops on every single hit (same as no condition)");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition >= 1 (should stop on every hit)");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: ">=1");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should stop on first hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "0");
                }

                this.Comment("Continue - should stop on second hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "1");
                }

                this.Comment("Continue - should stop on third hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "2");
                }

                // Remove the breakpoint so we can run to completion without stopping 7 more times
                this.Comment("Remove the breakpoint and run to completion");
                callingBreakpoints.Remove(17);
                runner.SetBreakpoints(callingBreakpoints);

                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointModuloOne(ITestSettings settings)
        {
            this.TestPurpose("Tests that hitCondition '%%1' stops on every hit (degenerate modulo)");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition %1 (every hit is a multiple of 1)");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: "%1");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should stop on first hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "0");
                }

                this.Comment("Continue - should stop on second hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "1");
                }

                // Remove the breakpoint so we can run to completion
                this.Comment("Remove the breakpoint and run to completion");
                callingBreakpoints.Remove(17);
                runner.SetBreakpoints(callingBreakpoints);

                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointModuloNoMultiple(ITestSettings settings)
        {
            this.TestPurpose("Tests that a modulo hit condition whose value exceeds total iterations only fires once at the Nth hit");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition %7 inside a loop that iterates 10 times");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: "%7");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should stop on 7th hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                this.Comment("Verify i == 6 (0-indexed, 7th iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "6");
                }

                this.Comment("Run to completion - 14th hit would be next multiple but loop only has 10 iterations");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointModuloRearms(ITestSettings settings)
        {
            this.TestPurpose("Tests that a modulo breakpoint fires on every Nth hit across many cycles, verifying that GDB's ignore count is re-armed after each stop");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition %2 inside a loop that iterates 10 times");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: "%2");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should stop on 2nd hit (i == 1)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "1");
                }

                this.Comment("Continue - should stop on 4th hit (i == 3)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "3");
                }

                this.Comment("Continue - should stop on 6th hit (i == 5)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "5");
                }

                this.Comment("Continue - should stop on 8th hit (i == 7)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "7");
                }

                this.Comment("Continue - should stop on 10th hit (i == 9)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "9");
                }

                this.Comment("Run to completion - loop is exhausted");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointModifyMidRun(ITestSettings settings)
        {
            this.TestPurpose("Tests changing a hitCondition while stopped at the breakpoint");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition = 3");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: "3");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should stop on 3rd hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                this.Comment("Verify i == 2 (3rd iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "2");
                }

                // Change from EQUAL to GREATER_OR_EQUAL. The engine carries over the
                // 3 hits already counted, so >=8 should fire on the 8th overall hit.
                this.Comment("Change hit condition to >=8 while stopped");
                callingBreakpoints.Remove(17);
                callingBreakpoints.Add(17, hitCondition: ">=8");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Continue - should stop on 8th overall hit (i == 7)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                this.Comment("Verify i == 7 (8th iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "7");
                }

                this.Comment("Continue - should stop on 9th hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "8");
                }

                this.Comment("Continue - should stop on 10th hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "9");
                }

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointClearMidRun(ITestSettings settings)
        {
            this.TestPurpose("Tests that clearing a hit condition mid-run resets GDB's ignore count so the breakpoint fires on the next hit");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition %5 inside a loop that iterates 10 times");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: "%5");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should stop on 5th hit (i == 4)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "4");
                }

                this.Comment("Clear the hit condition while stopped — remove and re-add without hitCondition");
                callingBreakpoints.Remove(17);
                callingBreakpoints.Add(17);
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Continue - with no hit condition, should stop on the very next hit (i == 5)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                this.Comment("Verify the breakpoint fired immediately, not at the next modulo multiple");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "5");
                }

                this.Comment("Remove the breakpoint and run to completion");
                callingBreakpoints.Remove(17);
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointModifyGteToEqual(ITestSettings settings)
        {
            this.TestPurpose("Tests changing hitCondition from >=N to exact N (GTE -> EQUAL) mid-run");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition >=2");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: ">=2");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should stop on 2nd hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                this.Comment("Verify i == 1 (2nd iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "1");
                }

                // Change from GTE to EQUAL. Hit count is 2. We want to stop at exactly hit 5.
                this.Comment("Change hit condition to 5 (exact) while stopped at hit 2");
                callingBreakpoints.Remove(17);
                callingBreakpoints.Add(17, hitCondition: "5");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Continue - should stop on 5th overall hit (i == 4)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                this.Comment("Verify i == 4 (5th iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "4");
                }

                // Now change from EQUAL to a target that the hit count has already passed.
                // Changing to "3" while at hit 5 means the target is already passed — program should run to completion.
                this.Comment("Change hit condition to 3 (already passed) while stopped at hit 5");
                callingBreakpoints.Remove(17);
                callingBreakpoints.Add(17, hitCondition: "3");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to completion - hit 3 already passed, EQUAL never fires again");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointRemoveAndReAdd(ITestSettings settings)
        {
            this.TestPurpose("Tests removing a hit condition breakpoint and re-adding it with a different condition");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with hit condition = 2");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: "2");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should stop on 2nd hit");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                this.Comment("Verify i == 1 (2nd iteration)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "1");
                }

                this.Comment("Remove the breakpoint entirely");
                callingBreakpoints.Remove(17);
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Re-add with modulo condition %4 - hit count resets for new breakpoint");
                callingBreakpoints.Add(17, hitCondition: "%4");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Continue - breakpoint was removed and re-added, hit count restarted from 0");
                // After removal and re-add, the next hits are 3rd through 10th loop iterations.
                // With a fresh %4 condition, it should stop when the new hit count reaches 4.
                // The 3rd loop iteration is the 1st new hit, so 4th new hit = 6th loop iteration (i==5).
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                this.Comment("Verify the breakpoint stopped at the expected iteration");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "5");
                }

                this.Comment("Continue - next %4 would be the 8th new hit = 10th loop iteration (i==9)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "9");
                }

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointWithCondition(ITestSettings settings)
        {
            this.TestPurpose("Tests combining a boolean condition with a hitCondition on the same breakpoint");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with condition 'i >= 4' and hitCondition '3'");
                // The loop runs i = 0..9. The condition 'i >= 4' is true for i = 4,5,6,7,8,9.
                // Among those qualifying hits, the hitCondition '3' means break on the 3rd qualifying hit.
                // 1st qualifying hit: i=4, 2nd: i=5, 3rd: i=6 -> should stop at i=6.
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, condition: "i >= 4", hitCondition: "3");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                this.Comment("Verify the breakpoint stopped at the 3rd hit where i >= 4 (i == 6)");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "6");
                }

                this.Comment("Run to completion - equal condition already satisfied");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointGreaterOrEqualWithCondition(ITestSettings settings)
        {
            this.TestPurpose("Tests combining a boolean condition with a >= hitCondition");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with condition 'i % 2 == 0' and hitCondition '>=3'");
                // The loop runs i = 0..9. The condition 'i % 2 == 0' is true for i = 0,2,4,6,8.
                // Among those qualifying hits: 1st: i=0, 2nd: i=2, 3rd: i=4, 4th: i=6, 5th: i=8
                // The hitCondition '>=3' means break from the 3rd qualifying hit onward.
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, condition: "i % 2 == 0", hitCondition: ">=3");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to breakpoint - should stop on 3rd qualifying hit (i == 4)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "4");
                }

                this.Comment("Continue - should stop on 4th qualifying hit (i == 6)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "6");
                }

                this.Comment("Continue - should stop on 5th qualifying hit (i == 8)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "8");
                }

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        [UnsupportedDebugger(SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        public void InvalidHitConditionBreakpoint(ITestSettings settings)
        {
            this.TestPurpose("Tests that an invalid hit condition returns a non-verified breakpoint with an error message");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with an invalid hit condition");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: "invalid");
                var response = runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Verify breakpoint is not verified and has an error message about hit condition");
                Assert.NotNull(response.body.breakpoints);
                Assert.Single(response.body.breakpoints);
                Assert.False(response.body.breakpoints[0].verified, "Breakpoint with invalid hit condition should not be verified");
                Assert.Contains("hitCondition", response.body.breakpoints[0].message);

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterConfigurationDone();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        [UnsupportedDebugger(SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        public void InvalidLogMessageBreakpoint(ITestSettings settings)
        {
            this.TestPurpose("Tests that an invalid log message returns a non-verified breakpoint with an error message");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with an invalid log message (unmatched brace)");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, logMessage: "{unmatched");
                var response = runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Verify breakpoint is not verified and has an error message about log message");
                Assert.NotNull(response.body.breakpoints);
                Assert.Single(response.body.breakpoints);
                Assert.False(response.body.breakpoints[0].verified, "Breakpoint with invalid log message should not be verified");
                Assert.Contains("logMessage", response.body.breakpoints[0].message);

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterConfigurationDone();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        [UnsupportedDebugger(SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        public void InvalidLogMessageAndHitConditionBreakpoint(ITestSettings settings)
        {
            this.TestPurpose("Tests that both invalid log message and invalid hit condition errors are returned together");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set a breakpoint with both an invalid log message and an invalid hit condition");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, logMessage: "{unmatched", hitCondition: "invalid");
                var response = runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Verify breakpoint is not verified and error message mentions both logMessage and hitCondition");
                Assert.NotNull(response.body.breakpoints);
                Assert.Single(response.body.breakpoints);
                Assert.False(response.body.breakpoints[0].verified, "Breakpoint with invalid log message and hit condition should not be verified");
                Assert.Contains("logMessage", response.body.breakpoints[0].message);
                Assert.Contains("hitCondition", response.body.breakpoints[0].message);

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterConfigurationDone();

                runner.DisconnectAndVerify();
            }
        }

        #endregion

        #region HitCondition Edge Case Tests

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointEqualFiresOnce(ITestSettings settings)
        {
            this.TestPurpose("Tests that an EQUAL hitCondition fires exactly once and never again, even with re-arm");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set EQUAL breakpoint at hit 3 on loop body, plus unconditional breakpoint after loop");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: "3");
                callingBreakpoints.Add(21);
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run - should stop at hit 3 (i == 2)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "2");
                }

                this.Comment("Continue - EQUAL should not fire on hits 4-10; next stop is the post-loop breakpoint");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 21)
                              .AfterContinue();

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void HitConditionBreakpointEqualAfterGte(ITestSettings settings)
        {
            this.TestPurpose("Tests changing from >=N to EQUAL mid-run, verifying re-arm clears stale ignore count");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                this.Comment("Set breakpoint with >=3 — fires on hits 3, 4, 5, ...");
                SourceBreakpoints callingBreakpoints = new SourceBreakpoints(debuggee, SinkHelper.Calling);
                callingBreakpoints.Add(17, hitCondition: ">=3");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Run to first fire at hit 3 (i == 2)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterConfigurationDone();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "2");
                }

                this.Comment("Continue - >=3 fires on hit 4 (i == 3)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                this.Comment("Switch to EQUAL 7 while stopped at hit 4");
                callingBreakpoints.Remove(17);
                callingBreakpoints.Add(17, hitCondition: "7");
                runner.SetBreakpoints(callingBreakpoints);

                this.Comment("Continue - should skip hits 5 and 6, fire at hit 7 (i == 6)");
                runner.Expects.HitBreakpointEvent(SinkHelper.Calling, 17)
                              .AfterContinue();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector mainFrame = inspector.Stack.First();
                    mainFrame.AssertVariables("i", "6");
                }

                this.Comment("Run to completion - EQUAL 7 already satisfied, should not fire again");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        #endregion
    }
}
