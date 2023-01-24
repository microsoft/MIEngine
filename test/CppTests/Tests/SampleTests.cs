// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DebuggerTesting;
using DebuggerTesting.Compilation;
using DebuggerTesting.OpenDebug;
using DebuggerTesting.OpenDebug.CrossPlatCpp;
using DebuggerTesting.OpenDebug.Events;
using DebuggerTesting.OpenDebug.Extensions;
using DebuggerTesting.Ordering;
using DebuggerTesting.Settings;
using DebuggerTesting.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace CppTests.Tests
{
    [TestCaseOrderer(DependencyTestOrderer.TypeName, DependencyTestOrderer.AssemblyName)]
    public class SampleTests : TestBase
    {
        #region Constructor

        public SampleTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region Methods

        [Fact]
        public void ValidateSettings()
        {
            this.WriteLine(PathSettings.GetDebugPathString());
            AssertDirectoryExists(PathSettings.TempPath);
            AssertDirectoryExists(PathSettings.DebugAdaptersPath);
            AssertDirectoryExists(PathSettings.TestsPath);
            AssertDirectoryExists(PathSettings.DebuggeesPath);
            AssertFileExists(PathSettings.TestConfigurationFilePath);
        }

        private static void AssertDirectoryExists(string path)
        {
            Assert.True(Directory.Exists(path), "Directory '{0}' does not exist.".FormatInvariantWithArgs(path));
        }

        private static void AssertFileExists(string path)
        {
            Assert.True(File.Exists(path), "File '{0}' does not exist.".FormatInvariantWithArgs(path));
        }

        [Theory]
        [RequiresTestSettings]
        public void LaunchNonExistentDebuggee(ITestSettings settings)
        {
            this.TestPurpose("This test checks to see the debugger handles trying to start when there is no debuggee.");
            this.WriteSettings(settings);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, "foofoo", string.Empty);
                launch.ExpectsSuccess = false;
                runner.RunCommand(launch);

                Assert.Matches(".*does not exist.*", launch.Message);

                runner.DisconnectAndVerify();
            }
        }

        private const string HelloName = "hello";
        private const string HelloSourceName = "hello.cpp";
        private const string HexNumberPattern = @"0x[0-9A-Fa-f]+";

        [Theory]
        [RequiresTestSettings]
        public void CompileHelloDebuggee(ITestSettings settings)
        {
            this.TestPurpose("Create and compile the 'hello' debuggee");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Create(this, settings.CompilerSettings, HelloName, DebuggeeMonikers.HelloWorld.Sample);
            debuggee.AddSourceFiles(HelloSourceName);
            debuggee.Compile();
        }

        [Theory]
        [DependsOnTest(nameof(CompileHelloDebuggee))]
        [RequiresTestSettings]
        [UnsupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void TestArguments(ITestSettings settings)
        {
            this.TestPurpose("This test checks to see if arguments are passed to the debugee.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, HelloName, DebuggeeMonikers.HelloWorld.Sample);

            this.Comment("Run the debuggee, check argument count");
            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                runner.Launch(settings.DebuggerSettings, debuggee, "Param 1", "Param 2");
                int args = 2;
                runner.Expects.ExitedEvent(exitCode: args).TerminatedEvent().AfterConfigurationDone();
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileHelloDebuggee))]
        [RequiresTestSettings]
        public void TestFolderol(ITestSettings settings)
        {
            this.TestPurpose("This test checks a bunch of commands and events.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, HelloName, DebuggeeMonikers.HelloWorld.Sample);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Launch the debuggee");
                runner.Launch(settings.DebuggerSettings, debuggee);

                StoppedEvent stopAtBreak = new StoppedEvent(StoppedReason.Breakpoint);

                // VsDbg does not fire Breakpoint Change events when breakpoints are set.
                // Instead it sends a new breakpoint event when it is bound (after configuration done).
                bool bindsLate = (settings.DebuggerSettings.DebuggerType == SupportedDebugger.VsDbg);

                this.Comment("Set a breakpoint on line 8, but expect it to resolve to line 9.");
                runner.Expects.ConditionalEvent(!bindsLate, x => x.BreakpointChangedEvent(BreakpointReason.Changed, 9))
                              .AfterSetBreakpoints(debuggee.Breakpoints(HelloSourceName, 8));

                this.Comment("Start debuggging until breakpoint is hit.");
                runner.Expects.ConditionalEvent(bindsLate, x => x.BreakpointChangedEvent(BreakpointReason.Changed, 9))
                              .Event(stopAtBreak)
                              .AfterConfigurationDone();

                Assert.Equal(HelloSourceName, stopAtBreak.ActualEventInfo.Filename);
                Assert.Equal(9, stopAtBreak.ActualEventInfo.Line);
                Assert.Equal(StoppedReason.Breakpoint, stopAtBreak.ActualEventInfo.Reason);

                this.Comment("Step forward twice until we have initialized variables");
                runner.Expects.StoppedEvent(StoppedReason.Step, HelloSourceName, 10)
                              .AfterStepOver();
                runner.Expects.StoppedEvent(StoppedReason.Step, HelloSourceName, 11)
                              .AfterStepIn();

                this.Comment("Inspect the stack and try evaluation.");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    this.Comment("Get the stack trace");
                    IFrameInspector mainFrame = inspector.Stack.First();
                    inspector.AssertStackFrameNames(true, "main.*");

                    this.WriteLine("Main frame: {0}", mainFrame);

                    this.Comment("Get variables");
                    Assert.Subset(new HashSet<string>() { "x", "y", "argc", "argv" }, mainFrame.Variables.ToKeySet());
                    mainFrame.AssertVariables(
                        "x", "6",
                        "argc", "1");

                    IVariableInspector argv = mainFrame.Variables["argv"];
                    Assert.Matches(HexNumberPattern, argv.Value);

                    this.Comment("Expand a variable (argv has *argv under it)");
                    string variableName = "*argv";
                    if (settings.DebuggerSettings.DebuggerType == SupportedDebugger.VsDbg)
                    {
                        variableName = String.Empty;
                    }
                    Assert.Contains(variableName, argv.Variables.Keys);
                    Assert.Matches(HexNumberPattern, argv.Variables[variableName].Value);

                    this.Comment("Evaluate with side effect");
                    string result = mainFrame.Evaluate("x = x + 1");
                    Assert.Equal("7", result);
                }

                this.Comment("Step to force stack info to refresh");
                runner.Expects.StoppedEvent(StoppedReason.Step).AfterStepOver();

                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    this.Comment("Evaluation has side effect, make sure it propagates");
                    Assert.Equal("7", inspector.Stack.First().Variables["x"].Value);
                }

                runner.Expects.ExitedEvent(0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        #endregion
    }
}
