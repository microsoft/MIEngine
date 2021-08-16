// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
using DebuggerTesting.Settings;
using DebuggerTesting.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace CppTests.Tests
{
    [TestCaseOrderer(DependencyTestOrderer.TypeName, DependencyTestOrderer.AssemblyName)]
    public class AutoCompleteTests : TestBase
    {
        #region Constructor

        public AutoCompleteTests(ITestOutputHelper outputHelper) : base(outputHelper)
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

        private const string HelloName = "hello";
        private const string HelloSourceName = "hello.cpp";

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
        [UnsupportedDebugger(SupportedDebugger.Lldb | SupportedDebugger.VsDbg, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void TestAutoComplete(ITestSettings settings)
        {
            this.TestPurpose("This test checks a bunch of commands and events.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, HelloName, DebuggeeMonikers.HelloWorld.Sample);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Launch the debuggee");
                runner.Launch(settings.DebuggerSettings, debuggee);

                StoppedEvent stopAtBreak = new StoppedEvent(StoppedReason.Breakpoint);

                runner.Expects.BreakpointChangedEvent(BreakpointReason.Changed, 9)
                              .Event(stopAtBreak)
                              .AfterConfigurationDone();

                string[] completions = runner.CompletionsRequest("break");
                Assert.Collection(completions,
                    elem1 => Assert.Equal("break", elem1),
                    elem2 => Assert.Equal("break-range", elem2)
                );

                runner.Expects.ExitedEvent(0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        #endregion
    }
}
