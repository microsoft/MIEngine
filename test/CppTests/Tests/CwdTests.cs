// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    public class CwdTests : TestBase
    {
        #region Constructor

        public CwdTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region Methods

        private const string CwdName = "cwd";
        private const string CwdSourceName = "cwd.cpp";
        private const int ReturnLine = 14;

        [Theory]
        [RequiresTestSettings]
        public void CompileCwdDebuggee(ITestSettings settings)
        {
            this.TestPurpose("Create and compile the 'cwd' debuggee");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Create(this, settings.CompilerSettings, CwdName, DebuggeeMonikers.Cwd.Default);
            debuggee.AddSourceFiles("cwd.cpp");
            debuggee.Compile();
        }

        [Theory]
        [DependsOnTest(nameof(CompileCwdDebuggee))]
        [RequiresTestSettings]
        public void TestProgramDirectory(ITestSettings settings)
        {
            this.TestPurpose("This test checks if cwd is set correctly in the debugee process for program dir.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, CwdName, DebuggeeMonikers.Natvis.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");

                string path = NormalizePath(Path.GetDirectoryName(debuggee.OutputPath));

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, null, null, false);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(CwdSourceName, ReturnLine);
                runner.SetBreakpoints(writerBreakpoints);

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verifying 'cwd'");
                    string output = NormalizePath(GetPathFromOutput(currentFrame.GetVariable("currentDir").Value));

                    Assert.Equal(path, output);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileCwdDebuggee))]
        [RequiresTestSettings]
        public void TestTestFolderDirectory(ITestSettings settings)
        {
            this.TestPurpose("This test checks if cwd is set correctly in the debugee process for test dir.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, CwdName, DebuggeeMonikers.Natvis.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");

                string path = NormalizePath(Directory.GetCurrentDirectory());

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, path, null, false);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(CwdSourceName, ReturnLine);
                runner.SetBreakpoints(writerBreakpoints);

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verifying 'cwd'");
                    string output = NormalizePath(GetPathFromOutput(currentFrame.GetVariable("currentDir").Value));

                    Assert.Equal(path, output);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileCwdDebuggee))]
        [RequiresTestSettings]
        public void TestTempDirectory(ITestSettings settings)
        {
            this.TestPurpose("This test checks if cwd is set correctly in the debugee process for a temp dir.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, CwdName, DebuggeeMonikers.Natvis.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");

                string path = NormalizePath(Path.GetTempPath());

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, path, null, false);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(CwdSourceName, ReturnLine);
                runner.SetBreakpoints(writerBreakpoints);

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verifying 'cwd'");
                    string output = NormalizePath(GetPathFromOutput(currentFrame.GetVariable("currentDir").Value));

                    // Using 'contains' here since on macOS it prepends the actual tmp path with '/private'
                    Assert.Contains(path, output);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        private string GetPathFromOutput(string output)
        {
            Regex regex = new Regex("0x[0-9a-f]+\\s\"*(.+)\"");

            Match m = regex.Match(output);
            if (m.Success)
            {
                return m.Groups?[1]?.Value;
            }
            else
            {
                return string.Empty;
            }
        }

        private string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        #endregion
    }
}
