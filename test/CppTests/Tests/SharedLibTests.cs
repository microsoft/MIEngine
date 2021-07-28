// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.IO;
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
    public class SharedLibTests : TestBase
    {
        #region Constructor

        public SharedLibTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region Fields

        private const string srcLibName = "mylib.cpp";
        private const string srcAppName = "myapp.cpp";
        private const string outAppName = "myapp";
        private const string outLibName = "mylib";
        private const string debuggeeName = "sharedlib";

        #endregion

        #region Methods

        [Theory]
        [RequiresTestSettings]
        public void CompileSharedLibDebuggee(ITestSettings settings)
        {
            this.TestPurpose("Create and compile the 'sharedlib' debuggee");
            this.WriteSettings(settings);

            //Compile the shared library
            CompileSharedLib(settings, DebuggeeMonikers.SharedLib.Default);

            //Compile the application
            CompileApp(settings, DebuggeeMonikers.SharedLib.Default);
        }

        [Theory]
        [DependsOnTest(nameof(CompileSharedLibDebuggee))]
        // TODO: https://github.com/microsoft/MIEngine/issues/1170
        // - lldb
        [UnsupportedDebugger(SupportedDebugger.VsDbg | SupportedDebugger.Lldb, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        [RequiresTestSettings]
        public void SharedLibBasic(ITestSettings settings)
        {
            this.TestPurpose("This test checks to see if basic debugging scenarios work for application invoked shared library.");
            this.WriteSettings(settings);

            this.Comment("Start running targeted scenarios");
            RunTargetedScenarios(settings, outAppName, DebuggeeMonikers.SharedLib.Default);
        }

        [Theory]
        // TODO: https://github.com/microsoft/MIEngine/issues/1170
        // - lldb
        [UnsupportedDebugger(SupportedDebugger.VsDbg | SupportedDebugger.Lldb, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        [RequiresTestSettings]
        public void SharedLibMismatchSourceAndSymbols(ITestSettings settings)
        {
            this.TestPurpose("This test checks to see if it crashs when debugging the mismatch source and symbols of shared library.");
            this.WriteSettings(settings);

            //Compile the shared library
            CompileSharedLib(settings, DebuggeeMonikers.SharedLib.MismatchedSource);

            //Compile the application
            CompileApp(settings, DebuggeeMonikers.SharedLib.MismatchedSource);

            this.Comment("Apply changes in the source of shared library after build");
            ApplyChangesInSharedLib(settings, DebuggeeMonikers.SharedLib.MismatchedSource);

            this.Comment("Start running targeted scenarios");
            RunTargetedScenarios(settings, outAppName, DebuggeeMonikers.SharedLib.MismatchedSource);
        }

        #endregion

        #region Function Helper

        /// <summary>
        /// Compile the shared library
        /// </summary>
        private void CompileSharedLib(ITestSettings settings, int debuggeeMoniker)
        {
            IDebuggee debuggee = Debuggee.Create(this, settings.CompilerSettings, debuggeeName, debuggeeMoniker, outLibName, CompilerOutputType.SharedLibrary);
            debuggee.AddSourceFiles(srcLibName);
            debuggee.Compile();
        }

        /// <summary>
        /// Compile the application
        /// </summary>
        private void CompileApp(ITestSettings settings, int debuggeeMoniker)
        {
            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, debuggeeName, debuggeeMoniker, outAppName);

            switch (settings.DebuggerSettings.DebuggerType)
            {
                case SupportedDebugger.Gdb_Cygwin:
                case SupportedDebugger.Gdb_Gnu:
                case SupportedDebugger.Lldb:
                    debuggee.AddLibraries("dl");
                    break;
                case SupportedDebugger.Gdb_MinGW:
                    // The sharedlib debuggee contains both POSIX and Windows support on loading dynamic library, we use "_MinGW" to identify the relevant testing code
                    debuggee.AddDefineConstant("_MINGW");
                    break;
            }

            debuggee.AddSourceFiles(srcAppName);

            debuggee.Compile();
        }

        /// <summary>
        /// Apply changes in shared library so that the source and symbols is not matached anymore
        /// </summary>
        private void ApplyChangesInSharedLib(ITestSettings settings, int debuggeeMoniker)
        {
            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, debuggeeName, debuggeeMoniker, outLibName);
            string libPath = string.Format(CultureInfo.InvariantCulture, Path.Combine(debuggee.SourceRoot, srcLibName));
            Assert.True(File.Exists(libPath), string.Format(CultureInfo.InvariantCulture, "ERROR: Didn't find the source file:{0} under {1}", libPath, debuggee.SourceRoot));
            try
            {
                using (StreamWriter writer = File.AppendText(libPath))
                {
                    //TODO: I just simply added a new line to make the symbols mismatch after compile the library, we can add some real code changes here if need it.
                    writer.WriteLine(System.Environment.NewLine);
                }
            }
            catch
            {
                this.Comment("ERROR: Didn't apply the changes in shared library successfully.");
                throw;
            }
        }

        /// <summary>
        /// Testing the common targeted scenarios
        /// </summary>
        private void RunTargetedScenarios(ITestSettings settings, string outputName, int debuggeeMoniker)
        {
            this.Comment("Set initial debuggee");
            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, debuggeeName, debuggeeMoniker, outAppName);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee);

                this.Comment("Set initial function breakpoints");
                FunctionBreakpoints functionBreakpoints = new FunctionBreakpoints("main", "myClass::DisplayName", "myClass::DisplayAge");
                runner.SetFunctionBreakpoints(functionBreakpoints);

                this.Comment("Set line breakpoints to the lines with entry of shared library");
                SourceBreakpoints bps = debuggee.Breakpoints(srcAppName, 71, 77);
                runner.SetBreakpoints(bps);

                this.Comment("Launch and run until first breakpoint in the entry of main");
                runner.ExpectBreakpointAndStepToTarget(srcAppName, startLine: 62, targetLine: 63)
                              .AfterConfigurationDone();

                this.Comment("Continue to go to the line which is the first entry of shared library");
                runner.Expects.HitBreakpointEvent(srcAppName, 71)
                       .AfterContinue();

                this.Comment("Step into the function in shared library");
                runner.Expects.HitStepEvent(srcLibName, 23)
                       .AfterStepIn();

                this.Comment("Step out to go back to the entry in main function");
                runner.Expects.HitStepEvent(srcAppName, 71)
                       .AfterStepOut();

                this.Comment("Step over to go to the line which is the second entry of shared library");
                runner.Expects.HitStepEvent(srcAppName, 73).AfterStepOver();

                this.Comment("Step over a function which have a breakpoint set in shared library");
                runner.ExpectBreakpointAndStepToTarget(srcLibName, startLine: 8, targetLine: 9).AfterStepOver();

                this.Comment("Step over a line in function which is inside shared library");
                runner.Expects.HitStepEvent(srcLibName, 10).AfterStepOver();

                this.Comment("Step out to go back to the entry in main function");
                runner.ExpectStepAndStepToTarget(srcAppName, startLine: 73, targetLine: 75).AfterStepOut();

                this.Comment("Continue to hit breakpoint in function which is inside shared library");
                runner.ExpectBreakpointAndStepToTarget(srcLibName, startLine: 15, targetLine: 16)
                       .AfterContinue();

                this.Comment("Continue to hit breakpoint which set in the last entry of shared library");
                runner.Expects.HitBreakpointEvent(srcAppName, 77).AfterContinue();

                this.Comment("Step over a function which don't have breakpoint set in shared library");

                runner.Expects.HitStepEvent(srcAppName, 79)
                       .AfterStepOver();

                this.Comment("Continue to run till at the end of the application");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        #endregion
    }
}
