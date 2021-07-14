// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using DebuggerTesting;
using DebuggerTesting.Compilation;
using DebuggerTesting.OpenDebug;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.OpenDebug.CrossPlatCpp;
using DebuggerTesting.OpenDebug.Extensions;
using DebuggerTesting.Ordering;
using Xunit;
using Xunit.Abstractions;

namespace CppTests.Tests
{
    [TestCaseOrderer(DependencyTestOrderer.TypeName, DependencyTestOrderer.AssemblyName)]
    public class OptimizationTests : TestBase
    {
        #region Constructor

        public OptimizationTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region Member Variables

        private const string Name = "optimization";
        private const string SourceName = "source.cpp";
        private const string UserDefinedClassName = "foo.cpp";
        private const string SrcLibName = "mylib.cpp";
        private const string OutLibName = "mylib";

        #endregion

        #region Methods
        [Theory]
        [RequiresTestSettings]
        public void CompileSharedLibDebuggeeWithoutSymbol(ITestSettings settings)
        {
            this.TestPurpose("Create and compile the 'sharedlib' debuggee");
            this.WriteSettings(settings);

            //Compile the shared library
            CompileSharedLib(settings, DebuggeeMonikers.Optimization.OptimizationWithoutSymbols, false);

            //Compile the application
            CompileApp(settings, DebuggeeMonikers.Optimization.OptimizationWithoutSymbols);
        }

        [Theory]
        [DependsOnTest(nameof(CompileSharedLibDebuggeeWithoutSymbol))]
        [UnsupportedDebugger(SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        [RequiresTestSettings]
        public void TestSharedLibWithoutSymbol(ITestSettings settings)
        {
            this.TestPurpose("Tests basic bps and source information for shared library without symbols");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, Name, DebuggeeMonikers.Optimization.OptimizationWithoutSymbols);
            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee);

                SourceBreakpoints mainBreakpoints = debuggee.Breakpoints(SourceName, 87, 91);

                this.Comment("Set initial breakpoints");
                runner.SetBreakpoints(mainBreakpoints);

                this.Comment("Launch and run until 1st bp");
                runner.Expects.HitBreakpointEvent(SourceName, 87)
                                .AfterConfigurationDone();

                this.Comment("Step into the library source");
                runner.Expects.HitStepEvent(SourceName, 89)
                                .AfterStepIn();

                this.Comment("Check the stack for debugging shared library without symbols");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = inspector.Stack.First();
                    inspector.AssertStackFrameNames(true, "main");

                    this.Comment("run to continue to 2nd bp");
                    runner.Expects.HitBreakpointEvent(SourceName, 91)
                                  .AfterContinue();

                    this.Comment("Check the stack for debugging shared library without symbols");
                    currentFrame = inspector.Stack.First();
                    inspector.AssertStackFrameNames(true, "main");
                    this.Comment("Check the local variables in main function");
                    IVariableInspector age = currentFrame.Variables["age"];
                    Assert.Matches("31", age.Value);
                }

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [RequiresTestSettings]
        public void CompileSharedLibDebuggeeWithSymbol(ITestSettings settings)
        {
            this.TestPurpose("Create and compile the 'sharedlib' debuggee");
            this.WriteSettings(settings);

            //Compile the shared library
            CompileSharedLib(settings, DebuggeeMonikers.Optimization.OptimizationWithSymbols,true);

            //Compile the application
            CompileApp(settings, DebuggeeMonikers.Optimization.OptimizationWithSymbols);
        }

        [Theory]
        [DependsOnTest(nameof(CompileSharedLibDebuggeeWithSymbol))]
        [UnsupportedDebugger(SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        [RequiresTestSettings]
        public void TestOptimizedBpsAndSource(ITestSettings settings)
        {
            this.TestPurpose("Tests basic operation of bps and source information for optimized app");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, Name, DebuggeeMonikers.Optimization.OptimizationWithSymbols);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee);

                SourceBreakpoints mainBreakpoints = debuggee.Breakpoints(SourceName, 68);
                SourceBreakpoints userDefinedClassBreakpoints = debuggee.Breakpoints(UserDefinedClassName, 8,15,54);

                this.Comment("Set initial breakpoints");
                runner.SetBreakpoints(mainBreakpoints);
                runner.SetBreakpoints(userDefinedClassBreakpoints);

                this.Comment("Launch and run until 1st bp");
                runner.Expects.HitBreakpointEvent(UserDefinedClassName, 8)
                                .AfterConfigurationDone();

                this.Comment("run until 2nd bp");
                runner.Expects.HitBreakpointEvent(UserDefinedClassName, 54)
                                .AfterContinue();

                this.Comment("run until 3rd bp");
                runner.Expects.HitBreakpointEvent(SourceName, 68)
                                .AfterContinue();

                //Todo: this has different behavior on Mac(:16), Other Platforms(15) I have logged bug#247891 to track
                this.Comment("run until 4th bp");
                runner.ExpectBreakpointAndStepToTarget(UserDefinedClassName, 15, 16).AfterContinue();

                this.Comment("continue to next bp");
                runner.Expects.HitBreakpointEvent(UserDefinedClassName, 54)
                                .AfterContinue();

                this.Comment("Check the current callstack frame");
                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    this.Comment("Get current frame object");
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verify current frame");
                    threadInspector.AssertStackFrameNames(true, "Foo::Sum");
                }

                this.Comment("step out to main entry");
                runner.Expects.HitStepEvent(SourceName, 69)
                                .AfterStepOut();

                runner.Expects.ExitedEvent(0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }
  
        [Theory]
        [DependsOnTest(nameof(CompileSharedLibDebuggeeWithSymbol))]
        [RequiresTestSettings]
        public void TestOptimizedLocals(ITestSettings settings)
        {
            this.TestPurpose("Tests basic local expression which is not been optimized");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, Name, DebuggeeMonikers.Optimization.OptimizationWithSymbols);
            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee);

                SourceBreakpoints userDefinedClassBreakpoints = debuggee.Breakpoints(UserDefinedClassName, 54);

                this.Comment("Set initial breakpoints");
                runner.SetBreakpoints(userDefinedClassBreakpoints);

                this.Comment("Launch and run until 1st bp");
                runner.Expects.HitBreakpointEvent(UserDefinedClassName, 54)
                              .AfterConfigurationDone();

                this.Comment("Check the un-optimized values");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = inspector.Stack.First();
                    IVariableInspector sum = currentFrame.Variables["sum"];
                    IVariableInspector first = currentFrame.Variables["first"];

                    this.Comment("Check the local variables in sub function");
                    Assert.Matches("^0",sum.Value);
                    Assert.Matches("^1",first.Value);

                    this.Comment("Step out");
                    runner.Expects.HitStepEvent(SourceName, 66)
                                  .AfterStepOut();

                    this.Comment("Evaluate the expression:");
                    currentFrame = inspector.Stack.First();
                    inspector.AssertStackFrameNames(true, "main"); 
                }

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileSharedLibDebuggeeWithSymbol))]
        // TODO: Re-enable for Gdb_Gnu
        [UnsupportedDebugger(SupportedDebugger.Gdb_Gnu | SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        [RequiresTestSettings]
        public void TestOptimizedSharedLib(ITestSettings settings)
        {
            this.TestPurpose("Tests basic bps and source information for shared library");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, Name, DebuggeeMonikers.Optimization.OptimizationWithSymbols);
            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee);

                SourceBreakpoints mainBreakpoints = debuggee.Breakpoints(SourceName, 87, 89);
                SourceBreakpoints libBreakpoints = debuggee.Breakpoints(SrcLibName, 9, 12);

                this.Comment("Set initial breakpoints");
                runner.SetBreakpoints(mainBreakpoints);
                runner.SetBreakpoints(libBreakpoints);

                this.Comment("Launch and run until 1st bp");
                runner.Expects.HitBreakpointEvent(SourceName, 87)
                                .AfterConfigurationDone();

                //Todo: this has different behavior on Mac, I have logged bug#247895 to track
                //Del said that Different compilers generate symbols differently. 
                //Our tests have to be resilient to this fact. The location of the step is reasonable, 
                //so this is by design.
                this.Comment("enter into the library source");
                runner.ExpectBreakpointAndStepToTarget(SrcLibName, 8, 9).AfterContinue();

                this.Comment("Step over");
                runner.Expects.HitStepEvent(SrcLibName, 10).AfterStepOver();

                this.Comment("Check the un-optimized values in shared library");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = inspector.Stack.First();
                    IVariableInspector age = currentFrame.Variables["age"];

                    this.Comment("Check the local variable in sub function");
                    Assert.Matches("31", age.Value);

                    this.Comment("run to continue");
                    if(settings.DebuggerSettings.DebuggerType == SupportedDebugger.Lldb)
                    {
                        runner.Expects.HitBreakpointEvent(SourceName, 89)
                                  .AfterContinue();
                        this.Comment("Verify current frame for main func");
                        inspector.AssertStackFrameNames(true, "main");
                    }
                    else
                    {
                        runner.Expects.HitBreakpointEvent(SrcLibName, 12)
                                      .AfterContinue();
                        this.Comment("Verify current frame for library func");
                        inspector.AssertStackFrameNames(true, "myClass::DisplayAge");

                        this.Comment("Step out to main entry");
                        runner.Expects.HitBreakpointEvent(SourceName, 89).AfterContinue();

                        this.Comment("Verify current frame for main entry");
                        inspector.AssertStackFrameNames(true, "main");
                    }

                    this.Comment("Evaluate the expression:");
                    //skip the Mac's verification as bug#247893
                    currentFrame = inspector.Stack.First();
                    string strAge = currentFrame.Evaluate("myclass->DisplayAge(30)");

                    if (settings.DebuggerSettings.DebuggerType == SupportedDebugger.Gdb_MinGW || 
                        settings.DebuggerSettings.DebuggerType == SupportedDebugger.Gdb_Cygwin)
                    {
                        Assert.Equal("Cannot evaluate function -- may be inlined", strAge);
                    }
                }

                runner.DisconnectAndVerify();
            }
        }
        #endregion

        #region Function Helper

        private void CompileSharedLib(ITestSettings settings, int debuggeeMoniker, bool symbol)
        {
            IDebuggee debuggee = Debuggee.Create(this, settings.CompilerSettings, Name, debuggeeMoniker, OutLibName, CompilerOutputType.SharedLibrary);
            debuggee.AddSourceFiles(SrcLibName);
            debuggee.CompilerOptions = CompilerOption.OptimizeLevel2;
            if (symbol)
            {
                debuggee.CompilerOptions = CompilerOption.GenerateSymbols;
            }
            debuggee.Compile();
        }

        private void CompileApp(ITestSettings settings, int debuggeeMoniker)
        {
            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, Name, debuggeeMoniker, null, CompilerOutputType.Executable);

            switch (settings.DebuggerSettings.DebuggerType)
            {
                case SupportedDebugger.Gdb_Cygwin:
                case SupportedDebugger.Gdb_Gnu:
                case SupportedDebugger.Lldb:
                    debuggee.AddLibraries("dl");
                    break;
            }

            debuggee.AddSourceFiles(SourceName,UserDefinedClassName);
            debuggee.CompilerOptions = CompilerOption.OptimizeLevel2;
            debuggee.CompilerOptions = CompilerOption.GenerateSymbols;
            debuggee.Compile();
        }

        #endregion
    }
}