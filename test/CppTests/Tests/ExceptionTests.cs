// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using DebuggerTesting;
using DebuggerTesting.Compilation;
using DebuggerTesting.OpenDebug;
using DebuggerTesting.OpenDebug.Commands;
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
    public class ExceptionTests : TestBase
    {
        #region Constructor

        public ExceptionTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region Fields

        private const string srcAppName = "main.cpp";
        private const string srcClassName = "exception.cpp";
        private const string outAppName = "myapp";
        private const string debuggeeName = "exception";
        private static object syncObject = new object();

        #endregion

        #region Methods

        [Theory]
        [RequiresTestSettings]
        public void CompileExceptionDebuggee(ITestSettings settings)
        {
            this.TestPurpose("Create and compile the 'exception' debuggee");
            this.WriteSettings(settings);

            this.Comment("Compile the application");
            CompileApp(this, settings, DebuggeeMonikers.Exception.Default);
        }

        [Theory]
        [DependsOnTest(nameof(CompileExceptionDebuggee))]
        [RequiresTestSettings]
        public void RaisedUnhandledException(ITestSettings settings)
        {
            this.TestPurpose("This test checks to see if unhandled exception can work during debugging");
            this.WriteSettings(settings);
            this.Comment("Set initial debuggee for application");
            IDebuggee debuggee = OpenDebuggee(this, settings, DebuggeeMonikers.Exception.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Launch the application");
                runner.Launch(settings.DebuggerSettings, debuggee, "-CallRaisedUnhandledException");

                this.Comment("Start debugging to hit the exception and verify it should stop at correct source file and line");
                runner.Expects.StoppedEvent(StoppedReason.Exception, srcClassName, 8).AfterConfigurationDone();

                this.Comment("Verify the callstack, variables and evaluation ");
                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    this.Comment("Get current frame object");
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verify current frame when stop at the exception");
                    threadInspector.AssertStackFrameNames(true, "myException::RaisedUnhandledException.*");

                    this.Comment("Verify the variables after stop at the exception");
                    Assert.Subset(new HashSet<string>() { "result", "temp", "this", "myvar" }, currentFrame.Variables.ToKeySet());
                    currentFrame.AssertVariables("result", "10", "temp", "0", "myvar", "200");

                    // TODO: LLDB was affected by bug #240441, I wil update this once this bug get fixed
                    if (settings.DebuggerSettings.DebuggerType != SupportedDebugger.Lldb)
                    {
                        this.Comment("Evaluate an expression and verify the results after stop at the exception");
                        string varEvalResult = currentFrame.Evaluate("result = result + 1");
                        this.WriteLine("Expected: 11, Actual: {0}", varEvalResult);
                        Assert.Equal("11", varEvalResult);
                    }

                    // TODO: Mingw32 was affected by bug #242924, I wil update this once this bug get fixed
                    if (!(settings.DebuggerSettings.DebuggerType == SupportedDebugger.Gdb_MinGW && settings.DebuggerSettings.DebuggeeArchitecture == SupportedArchitecture.x86))
                    {
                        this.Comment("Evaluate a function and verify the the results after stop at exception");
                        string funEvalResult = currentFrame.Evaluate("EvalFunc(100,100)");
                        this.WriteLine("Expected: 200, Actual: {0}", funEvalResult);
                        Assert.Equal("200", funEvalResult);
                    }
                }

                this.Comment("Stop debugging");
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileExceptionDebuggee))]
        [RequiresTestSettings]
        public void RaisedHandledException(ITestSettings settings)
        {
            this.TestPurpose("This test checks to see if user handled exception can work during debugging");
            this.WriteSettings(settings);

            this.Comment("Set initial debuggee for application");
            IDebuggee debuggee = OpenDebuggee(this, settings, DebuggeeMonikers.Exception.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Launch the application");
                runner.Launch(settings.DebuggerSettings, debuggee, "-CallRaisedHandledException");

                this.Comment("Set line breakpoints to the lines with entry of try block and catch block");
                SourceBreakpoints bps = debuggee.Breakpoints(srcClassName, 20, 33);
                runner.SetBreakpoints(bps);

                this.Comment("Start debugging and hit the breakpoint in the try block");
                runner.Expects.HitBreakpointEvent(srcClassName, 20).AfterConfigurationDone();

                this.Comment("Step over in the try block");
                runner.Expects.HitStepEvent(srcClassName, 21).AfterStepOver();

                this.Comment("Continue to raise the exception and hit the breakpoint set in the catch block");
                runner.Expects.HitBreakpointEvent(srcClassName, 33).AfterContinue();

                this.Comment("Verify can step over in the catch block");
                runner.Expects.HitStepEvent(srcClassName, 34).AfterStepOver();

                this.Comment("Verify the callstack, variables and evaluation ");
                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    this.Comment("Get current frame object");
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verify current frame when stop at the catch block");
                    threadInspector.AssertStackFrameNames(true, "myException::RaisedHandledException.*");

                    this.Comment("Verify the variables in the catch block");
                    Assert.Subset(new HashSet<string>() { "result", "global", "this", "a" ,"errorCode","ex"}, currentFrame.Variables.ToKeySet());
                    currentFrame.AssertVariables("result", "201", "global", "101", "a", "100");

                    this.Comment("Verify the exception information in the catch block");
                    IVariableInspector exVar = currentFrame.Variables["ex"];
                    Assert.Contains("code", exVar.Variables.Keys);
                    this.WriteLine("Expected: 101, Actual: {0}", exVar.Variables["code"].Value);
                    Assert.Equal("101", exVar.Variables["code"].Value);

                    // TODO: LLDB was affected by bug #240441, I wil update this once this bug get fixed
                    if (settings.DebuggerSettings.DebuggerType != SupportedDebugger.Lldb)
                    {
                        this.Comment("Evaluate an expression and verify the results");
                        string varEvalResult = currentFrame.Evaluate("result=result + 1");
                        this.WriteLine("Expected: 202, Actual: {0}", varEvalResult);
                        Assert.Equal("202", varEvalResult);
                    }

                    this.Comment("Evaluate a function and verify the the results");
                    // TODO: Mingw32 was affected by bug #242924, I wil update this once this bug get fixed
                    bool evalNotSupportedInCatch =
                        (settings.DebuggerSettings.DebuggerType == SupportedDebugger.Gdb_MinGW && settings.DebuggerSettings.DebuggeeArchitecture == SupportedArchitecture.x86) ||
                        (settings.DebuggerSettings.DebuggerType == SupportedDebugger.VsDbg && settings.DebuggerSettings.DebuggeeArchitecture == SupportedArchitecture.x64);
                    if (!evalNotSupportedInCatch)
                    {
                        string funEvalResult = currentFrame.Evaluate("RecursiveFunc(50)");
                        this.WriteLine("Expected: 1, Actual: {0}", funEvalResult);
                        Assert.Equal("1", funEvalResult);
                    }
                }

                this.Comment("Verify can step over after evaluation in the catch block");
                runner.Expects.HitStepEvent(srcClassName, 35).AfterStepOver();

                this.Comment("Continue to run at the end of the application");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileExceptionDebuggee))]
        [RequiresTestSettings]
        public void RaisedReThrowException(ITestSettings settings)
        {
            this.TestPurpose("This test checks to see if re-throw exception can work during debugging.");
            this.WriteSettings(settings);

            this.Comment("Set initial debuggee for application");
            IDebuggee debuggee = OpenDebuggee(this, settings, DebuggeeMonikers.Exception.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Launch the application");
                runner.Launch(settings.DebuggerSettings, debuggee, "-CallRaisedReThrowException");

                this.Comment("Set line breakpoints to the lines with entry of try block and catch block");
                SourceBreakpoints bps = debuggee.Breakpoints(srcClassName, 73, 79, 86);
                runner.SetBreakpoints(bps);

                this.Comment("Start debugging and hit the breakpoint in the try block");
                runner.Expects.HitBreakpointEvent(srcClassName, 73).AfterConfigurationDone();

                this.Comment("Continue executing and hit the breakpoint in the frist catch block");
                runner.Expects.HitBreakpointEvent(srcClassName, 79).AfterContinue();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    this.Comment("Get current frame object");
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verify current frame when stop at the first catch block");
                    threadInspector.AssertStackFrameNames(true, "myException::RaisedReThrowException.*");

                    this.Comment("Verify the variables of 'errorCode' in the first catch block");
                    currentFrame.AssertVariables("errorCode", "200");

                    this.Comment("Verify step in can work in the first catch block");
                    runner.ExpectStepAndStepToTarget(srcClassName, startLine: 41, targetLine: 42).AfterStepIn();

                    this.Comment("Verify current frame after step in another function");
                    threadInspector.AssertStackFrameNames(true, "myException::EvalFunc.*");

                    this.Comment("Verify step out can work in the first catch block");
                    runner.Expects.HitStepEvent(srcClassName, 79).AfterStepOut();
                }

                this.Comment("Continue to hit the re-throw exception in the first catch block");
                runner.Expects.HitBreakpointEvent(srcClassName, 86).AfterContinue();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    this.Comment("Get current frame object");
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verify current frame when stop at the second catch block");
                    threadInspector.AssertStackFrameNames(true, "myException::RaisedReThrowException.*");

                    this.Comment("Verify the variables in the second catch block");
                    Assert.Subset(new HashSet<string>() { "var", "this", "errorCode", "ex2" }, currentFrame.Variables.ToKeySet());
                    currentFrame.AssertVariables("var", "400", "errorCode", "400");

                    this.Comment("Verify the exception information in the second catch block");
                    IVariableInspector ex2Var = currentFrame.Variables["ex2"];
                    Assert.Contains("code", ex2Var.Variables.Keys);
                    this.WriteLine("Expected: 400, Actual: {0}", ex2Var.Variables["code"].Value);
                    Assert.Equal("400", ex2Var.Variables["code"].Value);

                    this.Comment("Evaluate a function and verify the the results at the second catch block ");
                    // TODO: Mingw32 was affected by bug #242924, I wil update this once this bug get fixed
                    bool evalNotSupportedInCatch =
                        (settings.DebuggerSettings.DebuggerType == SupportedDebugger.Gdb_MinGW && settings.DebuggerSettings.DebuggeeArchitecture == SupportedArchitecture.x86) ||
                        (settings.DebuggerSettings.DebuggerType == SupportedDebugger.VsDbg && settings.DebuggerSettings.DebuggeeArchitecture == SupportedArchitecture.x64);
                    if (!evalNotSupportedInCatch)
                    {
                        string funEvalResult = currentFrame.Evaluate("EvalFunc(20,20)");
                        this.WriteLine("Expected: 40, Actual: {0}", funEvalResult);
                        Assert.Equal("40", funEvalResult);
                    }
                }

                this.Comment("Verify can step out from a catch block");
                runner.ExpectStopAndStepToTarget(StoppedReason.Step, srcAppName, startLine: 61, targetLine: 64).AfterStepOut();

                this.Comment("Continue to run at the end of the application");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileExceptionDebuggee))]
        [RequiresTestSettings]
        [UnsupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        public void SetAllExceptionBreakpointTest(ITestSettings settings)
        {
            this.TestPurpose("This test checks to see if we can hit an exception breakpoint.");
            this.WriteSettings(settings);

            this.Comment("Set initial debuggee for application");
            IDebuggee debuggee = OpenDebuggee(this, settings, DebuggeeMonikers.Exception.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Launch the application");
                runner.Launch(settings.DebuggerSettings, debuggee, "-CallRaisedHandledException");

                this.Comment("Set line breakpoints to the lines with entry of try block and catch block");
                SourceBreakpoints bps = debuggee.Breakpoints(srcClassName, 20);
                runner.SetBreakpoints(bps);

                List<ExceptionFilterOptions> filterOptions = new List<ExceptionFilterOptions>()
                {
                    new ExceptionFilterOptions()
                    {
                        condition = string.Empty,
                        filterId = "all"
                    }
                };
                this.Comment("Set all exception breakpoint");
                runner.SetExceptionBreakpoints(null, filterOptions.ToArray());

                this.Comment("Start debugging and hit the breakpoint in the try block");
                runner.Expects.HitBreakpointEvent(srcClassName, 20).AfterConfigurationDone();

                this.Comment("Hit the exception");
                runner.Expects.StoppedEvent(StoppedReason.Exception).AfterContinue();

                this.Comment("Continue to run at the end of the application");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileExceptionDebuggee))]
        // TODO: Re-enable SupportedDebugger.Gdb_MinGW when updated past 10.x in CI.
        // Current issue is that it reports "did not find exception probe (does libstdcxx have SDT probes)?" and does not evaluate
        // the condition.
        [UnsupportedDebugger(SupportedDebugger.Lldb | SupportedDebugger.Gdb_MinGW, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        [RequiresTestSettings]
        public void SetConditionExceptionBreakpointTest(ITestSettings settings)
        {
            this.TestPurpose("This test checks to see if we can hit a conditional exception breakpoint.");
            this.WriteSettings(settings);

            this.Comment("Set initial debuggee for application");
            IDebuggee debuggee = OpenDebuggee(this, settings, DebuggeeMonikers.Exception.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Launch the application");
                runner.Launch(settings.DebuggerSettings, debuggee, "-CallRaisedHandledException");

                this.Comment("Set line breakpoints to the lines with entry of try block and catch block");
                SourceBreakpoints bps = debuggee.Breakpoints(srcClassName, 20);
                runner.SetBreakpoints(bps);

                List<ExceptionFilterOptions> filterOptions = new List<ExceptionFilterOptions>()
                {
                    new ExceptionFilterOptions()
                    {
                        condition = "std::out_of_range",
                        filterId = "all"
                    }
                };
                this.Comment("Set conditional exception breakpoint");
                runner.SetExceptionBreakpoints(null, filterOptions.ToArray());

                this.Comment("Start debugging and hit the breakpoint in the try block");
                runner.Expects.HitBreakpointEvent(srcClassName, 20).AfterConfigurationDone();

                // We expect the program to terminate because the exception is caught.
                // and we only want to stop on std::exception.

                this.Comment("Continue to run at the end of the application");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        #endregion

        #region Function Helper

        /// <summary>
        /// Create the debuggee and compile the application
        /// </summary>
        private static IDebuggee CompileApp(ILoggingComponent logger, ITestSettings settings, int debuggeeMoniker)
        {
            lock (syncObject)
            {
                IDebuggee debuggee = Debuggee.Create(logger, settings.CompilerSettings, debuggeeName, debuggeeMoniker, outAppName);
                debuggee.AddSourceFiles(srcClassName, srcAppName);
                debuggee.Compile();
                return debuggee;
            }
        }

        /// <summary>
        /// Open existing debuggee
        /// </summary>
        private static IDebuggee OpenDebuggee(ILoggingComponent logger, ITestSettings settings, int debuggeeMoniker)
        {
            lock (syncObject)
            {
                IDebuggee debuggee = Debuggee.Open(logger, settings.CompilerSettings, debuggeeName, debuggeeMoniker, outAppName);
                Assert.True(File.Exists(debuggee.OutputPath), "The debuggee was not compiled. Missing " + debuggee.OutputPath);
                return debuggee;
            }
        }

        #endregion
    }
}
