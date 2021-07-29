// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using DebuggerTesting;
using DebuggerTesting.Compilation;
using DebuggerTesting.OpenDebug;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.OpenDebug.Commands.Responses;
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
    public class CoreDumpTests : TestBase
    {
        #region Constructor

        public CoreDumpTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region Fields

        private const string srcAppName = "main.cpp";
        private const string srcClassName = "exception.cpp";
        private const string outAppName = "myapp";
        private const string outCoreName = "core";
        private const string debuggeeName = "exception";
        private const string evalError = "not available";
        private const string stepError = "Unable to {0}. This operation is not supported when debugging dump files";
        private const string bpError = "Error setting breakpoint. This operation is not supported when debugging dump files";
        private static object syncObject = new object();

        #endregion

        #region Methods

        [Theory]
        [RequiresTestSettings]
        [SupportedPlatform(SupportedPlatform.Linux, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        // TODO: https://github.com/microsoft/MIEngine/issues/1170
        // - gdb_gnu
        [UnsupportedDebugger(SupportedDebugger.Gdb_Gnu, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void CoreDumpBasic(ITestSettings settings)
        {
            this.TestPurpose("This test checks to see if core dump can be launched successfully");
            this.WriteSettings(settings);

            this.Comment("Compile the application");
            CompileApp(this, settings, DebuggeeMonikers.CoreDump.Default);

            this.Comment("Run core dump basic debugging scenarios");
            RunCoreDumpBasic(settings, DebuggeeMonikers.CoreDump.Default);
        }

        [Theory]
        [RequiresTestSettings]
        [SupportedPlatform(SupportedPlatform.Linux, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        // TODO: https://github.com/microsoft/MIEngine/issues/1170
        // - gdb_gnu
        [UnsupportedDebugger(SupportedDebugger.Gdb_Gnu, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void CoreDumpBasicMismatchedSourceAndSymbols(ITestSettings settings)
        {
            this.TestPurpose("This test checks to see if core dump can be launched successfully with mismathed source code.");
            this.WriteSettings(settings);

            this.Comment("Compile the application");
            CompileApp(this, settings, DebuggeeMonikers.CoreDump.MismatchedSource);

            this.TestPurpose("Apply changes in the source code after compile the debuggee");
            ApplyChangesInSource(settings, DebuggeeMonikers.CoreDump.MismatchedSource);

            this.Comment("Run core dump basic debugging scenarios with mismatched source file");
            RunCoreDumpBasic(settings, DebuggeeMonikers.CoreDump.MismatchedSource);
        }

        [Theory]
        [RequiresTestSettings]
        [SupportedPlatform(SupportedPlatform.Linux, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        // TODO: https://github.com/microsoft/MIEngine/issues/1170
        // - gdb_gnu
        [UnsupportedDebugger(SupportedDebugger.Gdb_Gnu, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void CoreDumpVerifyActions(ITestSettings settings)
        {
            this.TestPurpose("This test checks to see the behavior when do actions during core dump debugging.");
            this.WriteSettings(settings);

            this.Comment("Compile the application");
            CompileApp(this, settings, DebuggeeMonikers.CoreDump.Action);

            this.Comment("Set initial debuggee for application");
            IDebuggee debuggee = OpenDebuggee(this, settings, DebuggeeMonikers.CoreDump.Action);

            this.Comment("Launch the application to hit an exception and generate core dump");
            string coreDumpPath = GenerateCoreDump(settings, DebuggeeMonikers.CoreDump.Action, debuggee);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure the core dump before start debugging");
                runner.LaunchCoreDump(settings.DebuggerSettings, debuggee, coreDumpPath);

                this.Comment("Start debugging to hit the exception and verify it should stop at correct source file and line");
                runner.Expects.StoppedEvent(StoppedReason.Exception, srcClassName, 8).AfterConfigurationDone();

                this.Comment("Verify the error message for relevant actions during core dump debugging");
                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    this.Comment("Get current frame object");
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verify current frame when stop at the exception");
                    threadInspector.AssertStackFrameNames(true, "myException::RaisedUnhandledException.*");

                    this.Comment("Try to evaluate a expression and verify the results");

                    string varEvalResult = currentFrame.Evaluate("result + 1");
                    Assert.Equal("11", varEvalResult);

                    this.Comment("Try to evaluate a function and verify the error message");
                    EvaluateResponseValue evalResponse = runner.RunCommand(new EvaluateCommand("EvalFunc(100, 100)", currentFrame.Id));
                    // TODO: From VSCode IDE with the latest cpptools, the error message after evaluate function is "not availabe", but evalResponse.message is null, is it expected ?
                    // Currently just simply verify the result is empty as a workaround, need to revisit this once get more information from logged bug #242418
                    this.Comment(string.Format(CultureInfo.InvariantCulture, "Actual evaluated result: {0}", evalResponse.body.result));
                    Assert.True(evalResponse.body.result.Equals(string.Empty));

                    this.Comment(string.Format(CultureInfo.InvariantCulture, "Actual evaluated respone message: {0}", evalResponse.message));
                    //Assert.True(evalResponse.message.Contains(evalError));
                }

                this.Comment("Try to step in and verify the error message");
                StepInCommand stepInCommand = new StepInCommand(runner.DarRunner.CurrentThreadId);
                runner.RunCommandExpectFailure(stepInCommand);
                this.WriteLine(string.Format(CultureInfo.InvariantCulture, "Actual respone message: {0}", stepInCommand.Message));
                Assert.Contains(stepInCommand.Message, string.Format(CultureInfo.InvariantCulture, stepError, "step in"));

                this.Comment("Try to step over and verify the error message");
                StepOverCommand stepOverCommand = new StepOverCommand(runner.DarRunner.CurrentThreadId);
                runner.RunCommandExpectFailure(stepOverCommand);
                this.WriteLine(string.Format(CultureInfo.InvariantCulture, "Actual respone message: {0}", stepOverCommand.Message));
                Assert.Contains(stepOverCommand.Message, string.Format(CultureInfo.InvariantCulture, stepError, "step next"));

                this.Comment("Try to step out and verify the error message");
                StepOutCommand stepOutCommand = new StepOutCommand(runner.DarRunner.CurrentThreadId);
                runner.RunCommandExpectFailure(stepOutCommand);
                this.WriteLine(string.Format(CultureInfo.InvariantCulture, "Actual respone message: {0}", stepOutCommand.Message));
                Assert.Contains(stepOutCommand.Message, string.Format(CultureInfo.InvariantCulture, stepError, "step out"));

                this.Comment("Try to continue and verify the error message");
                ContinueCommand continueCommand = new ContinueCommand(runner.DarRunner.CurrentThreadId);
                runner.RunCommandExpectFailure(continueCommand);
                this.WriteLine(string.Format(CultureInfo.InvariantCulture, "Actual respone message: {0}", continueCommand.Message));
                Assert.Contains(continueCommand.Message, string.Format(CultureInfo.InvariantCulture, stepError, "continue"));

                this.Comment("Try to set a breakpoint and verify the error message");
                SourceBreakpoints bp = debuggee.Breakpoints(srcAppName, 16);
                SetBreakpointsResponseValue setBpResponse = runner.SetBreakpoints(bp);
                Assert.False(setBpResponse.body.breakpoints[0].verified);
                this.WriteLine(string.Format(CultureInfo.InvariantCulture, "Actual respone message: {0}", setBpResponse.body.breakpoints[0].message));
                Assert.Contains(setBpResponse.body.breakpoints[0].message, bpError);

                this.Comment("Stop core dump debugging");
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

        /// <summary>
        /// Launch the application and generate the core dump
        /// </summary>
        private string GenerateCoreDump(ITestSettings settings, int debuggeeMoniker, IDebuggee debuggee)
        {
            lock (syncObject)
            {
                string dumpPath = debuggee.OutputPath.Replace(outAppName, outCoreName);
                this.Comment("Expecting core dump to be generated at '{0}'.".FormatInvariantWithArgs(dumpPath));

                debuggee.Launch("-CallRaisedUnhandledException").WaitForExit(2000);

                // Sometimes need take more time to generate core dump on virtual machine
                int maxAttempts = 10;
                for (int attempt = 0; attempt < maxAttempts && !File.Exists(dumpPath); attempt++)
                {
                    this.Comment("Waiting for core dump to be generated ({0}/{1}).".FormatInvariantWithArgs(attempt + 1, maxAttempts));
                    Thread.Sleep(3000);
                }

                Assert.True(File.Exists(dumpPath), "Core dump was not generated at '{0}'.".FormatInvariantWithArgs(dumpPath));

                return dumpPath;
            }
        }

        /// <summary>
        /// Apply changes in the source file so that the source and symbols is not matached anymore
        /// </summary>
        private void ApplyChangesInSource(ITestSettings settings, int debuggeeMoniker)
        {
            IDebuggee debuggee = OpenDebuggee(this, settings, debuggeeMoniker);
            string srcAppNamePath = string.Format(CultureInfo.InvariantCulture, Path.Combine(debuggee.SourceRoot, srcAppName));
            Assert.True(File.Exists(srcAppNamePath), string.Format(CultureInfo.InvariantCulture, "ERROR: Didn't find the source file:{0} under {1}", srcAppName, debuggee.SourceRoot));
            try
            {
                using (StreamWriter writer = File.AppendText(srcAppNamePath))
                {
                    //TODO: I just simply added a new line to make the symbols mismatch after compile the application
                    writer.WriteLine(System.Environment.NewLine);
                }
            }
            catch
            {
                this.Comment("ERROR: Didn't apply the changes in source file successfully.");
                throw;
            }
        }

        /// <summary>
        /// Run core dump basic debugging scenarios
        /// </summary>
        void RunCoreDumpBasic(ITestSettings settings, int debuggeeMoniker)
        {
            this.Comment("Set initial debuggee for application");
            IDebuggee debuggee = OpenDebuggee(this, settings, debuggeeMoniker);

            this.Comment("Launch the application to hit an exception and generate core dump");
            string coreDumpPath = GenerateCoreDump(settings, debuggeeMoniker, debuggee);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure the core dump before start debugging");
                runner.LaunchCoreDump(settings.DebuggerSettings, debuggee, coreDumpPath);

                this.Comment("Start debugging to hit the exception and verify it should stop at correct source file and line");
                runner.Expects.StoppedEvent(StoppedReason.Exception, srcClassName, 8).AfterConfigurationDone();

                this.Comment("Verify the callstack and variables");
                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    this.Comment("Get current frame object");
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.WriteLine("current frame: {0}", currentFrame);

                    this.Comment("Verify current frame when stop at the exception");
                    threadInspector.AssertStackFrameNames(true, "myException::RaisedUnhandledException.*");

                    this.Comment("Verify the variables after stop at the exception");
                    Assert.Subset(new HashSet<string>() { "result", "temp", "this", "myvar" }, currentFrame.Variables.ToKeySet());
                    currentFrame.AssertVariables("result", "10", "temp", "0", "myvar", "200");
                }

                this.Comment("Stop core dump debugging");
                runner.DisconnectAndVerify();
            }
        }

        #endregion
    }
}
