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
                FunctionBreakpoints functionBreakpoints = new FunctionBreakpoints("Arguments::CoreRun", "Calling::CoreRun", "a()");
                runner.SetFunctionBreakpoints(functionBreakpoints);

                this.Comment("Launch and run until first initial breakpoint");
                runner.ExpectBreakpointAndStepToTarget(SinkHelper.Arguments, startLine: 9, targetLine: 10)
                              .AfterConfigurationDone();

                this.Comment("Continue until second initial breakpoint");
                runner.ExpectBreakpointAndStepToTarget(SinkHelper.Calling, startLine: 47, targetLine: 48)
                              .AfterContinue();

                this.Comment("Remove and replace third initial function breakpoint while in break mode");
                functionBreakpoints.Remove("a()");
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

                this.Comment("Run to completion");
                runner.Expects.ExitedEvent()
                              .TerminatedEvent()
                              .AfterConfigurationDone();

                runner.DisconnectAndVerify();
            }
        }

        #endregion
    }
}
