// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    public class SourceMappingTests : TestBase
    {
        #region Constructor

        public SourceMappingTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region SourceMappingHelper

        private static class SourceMappingHelper
        {
            public const string Main = "main.cpp";

            public const string Writer = "writer.cpp";
            public const string WriterFolder = "writer";

            public const string Manager = "manager.cpp";
            public const string ManagerFolder = "manager";

            private const string Name = "sourcemap";
            private const string OutputName = "mapping";

            public static IDebuggee OpenAndCompile(ILoggingComponent logger, ICompilerSettings settings, int moniker)
            {
                return DebuggeeHelper.OpenAndCompile(logger, settings, moniker, SourceMappingHelper.Name, SourceMappingHelper.OutputName, SourceMappingHelper.AddSourceFiles);
            }

            public static IDebuggee Open(ILoggingComponent logger, ICompilerSettings settings, int moniker)
            {
                return DebuggeeHelper.Open(logger, settings, moniker, SourceMappingHelper.Name, SourceMappingHelper.OutputName);
            }

            private static void AddSourceFiles(IDebuggee debuggee)
            {
                debuggee.AddSourceFiles(
                    SourceMappingHelper.Main,
                    Path.Combine(SourceMappingHelper.WriterFolder, SourceMappingHelper.Writer),
                    Path.Combine(SourceMappingHelper.ManagerFolder, SourceMappingHelper.Manager));
            }
        }

        #endregion

        private static void ValidateMappingToFrame(string expectedFileName, string expectedFilePath, IFrameInspector frame, StringComparison comparison)
        {
            Assert.True(frame.SourceName.Equals(expectedFileName, comparison), string.Format(CultureInfo.InvariantCulture, "Frame File name. Expected: {0} Actual: {1}", expectedFileName, frame.SourceName));
            Assert.True(frame.SourcePath.Equals(expectedFilePath, comparison), string.Format(CultureInfo.InvariantCulture, "Frame full path. Expected: {0} Actual: {1}", expectedFilePath, frame.SourcePath));
        }

        private string EnsureDriveLetterLowercase(string path)
        {
            if (path.Length > 2 && path[1] == ':')
            {
                path = String.Format(CultureInfo.CurrentUICulture, "{0}{1}", Char.ToLowerInvariant(path[0]), path.Substring(1));
            }

            return path;
        }

        [Theory]
        [RequiresTestSettings]
        public void CompileSourceMapForSourceMapping(ITestSettings settings)
        {
            this.TestPurpose("Compiles source map debuggee for source mapping.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SourceMappingHelper.OpenAndCompile(this, settings.CompilerSettings, DebuggeeMonikers.SourceMapping.Default);
        }

        [Theory]
        [DependsOnTest(nameof(CompileSourceMapForSourceMapping))]
        [RequiresTestSettings]
        public void MapSpecificFile(ITestSettings settings)
        {
            this.TestPurpose("Validate Specific File Mapping.");

            this.WriteSettings(settings);

            IDebuggee debuggee = SourceMappingHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.SourceMapping.Default);

            // VsDbg is case insensitive on Windows so sometimes stackframe file names might all be lowercase
            StringComparison comparison = settings.DebuggerSettings.DebuggerType == SupportedDebugger.VsDbg ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure");
                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, string.Empty, isAttach: false);

                launch.Args.externalConsole = false;

                this.Comment("Setting up Source File Mappings");

                Dictionary<string, string> sourceMappings = new Dictionary<string, string>();
                string pathRoot = Path.GetPathRoot(debuggee.SourceRoot);

                string sourceFileMapping = Path.Combine(pathRoot, Path.GetRandomFileName(), SourceMappingHelper.Writer);
                string compileFileMapping = Path.Combine(debuggee.SourceRoot, SourceMappingHelper.WriterFolder, SourceMappingHelper.Writer);

                if (PlatformUtilities.IsWindows)
                {
                    // Move file to the location
                    Directory.CreateDirectory(Path.GetDirectoryName(sourceFileMapping));
                    File.Copy(compileFileMapping, sourceFileMapping, true);
                }

                // Drive letter should be lowercase
                sourceMappings.Add(compileFileMapping, sourceFileMapping);

                launch.Args.sourceFileMap = sourceMappings;
                try
                {
                    runner.RunCommand(launch);

                    this.Comment("Set Breakpoint");

                    SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(SourceMappingHelper.Writer, 9);
                    runner.SetBreakpoints(writerBreakpoints);
                    runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                    using (IThreadInspector threadInspector = runner.GetThreadInspector())
                    {
                        IEnumerator<IFrameInspector> frameEnumerator = threadInspector.Stack.GetEnumerator();

                        // Move to first stack item
                        Assert.True(frameEnumerator.MoveNext());
                        this.Comment("Verify path is changed for writer.cpp frame");
                        ValidateMappingToFrame(SourceMappingHelper.Writer, EnsureDriveLetterLowercase(sourceFileMapping), frameEnumerator.Current, comparison);

                        // Move to second stack item
                        Assert.True(frameEnumerator.MoveNext());
                        this.Comment("Verify path is not changed for main.cpp frame.");
                        ValidateMappingToFrame(SourceMappingHelper.Main, EnsureDriveLetterLowercase(Path.Combine(debuggee.SourceRoot, SourceMappingHelper.Main)), frameEnumerator.Current, comparison);

                        writerBreakpoints.Remove(9);
                        runner.SetBreakpoints(writerBreakpoints);
                        this.Comment("Continue to end");

                        runner.Expects.TerminatedEvent().AfterContinue();
                        runner.DisconnectAndVerify();
                    }
                }
                finally
                {
                    if (PlatformUtilities.IsWindows)
                    {
                        // Cleanup the directory
                        if (Directory.Exists(Path.GetDirectoryName(sourceFileMapping)))
                        {
                            Directory.Delete(Path.GetDirectoryName(sourceFileMapping), true);
                        }
                    }
                }
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileSourceMapForSourceMapping))]
        [RequiresTestSettings]
        public void MapDirectory(ITestSettings settings)
        {
            this.TestPurpose("Validate Source Mapping.");

            this.WriteSettings(settings);

            IDebuggee debuggee = SourceMappingHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.SourceMapping.Default);

            // VsDbg is case insensitive on Windows so sometimes stackframe file names might all be lowercase
            StringComparison comparison = settings.DebuggerSettings.DebuggerType == SupportedDebugger.VsDbg ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure");
                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, string.Empty, false, "-fCalling");

                launch.Args.externalConsole = false;

                this.Comment("Setting up Source File Mappings");

                Dictionary<string, string> sourceMappings = new Dictionary<string, string>();
                string pathRoot = Path.GetPathRoot(debuggee.SourceRoot);

                string mgrDirectoryMapping = Path.Combine(debuggee.SourceRoot, SourceMappingHelper.Manager, Path.GetRandomFileName());
                string writerDirectoryMapping = Path.Combine(pathRoot, Path.GetRandomFileName(), Path.GetRandomFileName());
                string rootDirectoryMapping = Path.Combine(pathRoot, Path.GetRandomFileName());
                sourceMappings.Add(Path.Combine(debuggee.SourceRoot, SourceMappingHelper.WriterFolder), writerDirectoryMapping);
                sourceMappings.Add(Path.Combine(debuggee.SourceRoot, SourceMappingHelper.ManagerFolder), mgrDirectoryMapping);
                sourceMappings.Add(debuggee.SourceRoot, rootDirectoryMapping);

                launch.Args.sourceFileMap = sourceMappings;

                try
                {
                    if (PlatformUtilities.IsWindows)
                    {
                        // Create all the directories but only some of the files will exist on disk.
                        foreach (var dir in sourceMappings.Values)
                        {
                            Directory.CreateDirectory(dir);
                        }
                        File.Copy(Path.Combine(debuggee.SourceRoot, SourceMappingHelper.WriterFolder, SourceMappingHelper.Writer), Path.Combine(writerDirectoryMapping, SourceMappingHelper.Writer), true);
                        File.Copy(Path.Combine(debuggee.SourceRoot, SourceMappingHelper.Main), Path.Combine(rootDirectoryMapping, SourceMappingHelper.Main), true);
                    }

                    runner.RunCommand(launch);

                    this.Comment("Set Breakpoint");

                    SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(SourceMappingHelper.Writer, 9);
                    SourceBreakpoints managerBreakpoints = debuggee.Breakpoints(SourceMappingHelper.Manager, 8);
                    runner.SetBreakpoints(writerBreakpoints);
                    runner.SetBreakpoints(managerBreakpoints);
                    runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                    using (IThreadInspector threadInspector = runner.GetThreadInspector())
                    {
                        IEnumerator<IFrameInspector> frameEnumerator = threadInspector.Stack.GetEnumerator();

                        // Move to first stack item
                        Assert.True(frameEnumerator.MoveNext());
                        this.Comment(string.Format(CultureInfo.InvariantCulture, "Verify source path for {0}.", SourceMappingHelper.Writer));
                        // Since file is there, lowercase the drive letter
                        ValidateMappingToFrame(SourceMappingHelper.Writer, EnsureDriveLetterLowercase(Path.Combine(writerDirectoryMapping, SourceMappingHelper.Writer)), frameEnumerator.Current, comparison);


                        // Move to second stack item
                        Assert.True(frameEnumerator.MoveNext());
                        this.Comment(string.Format(CultureInfo.InvariantCulture, "Verify source path for {0}.", SourceMappingHelper.Main));
                        // Since file is there, lowercase the drive letter
                        ValidateMappingToFrame(SourceMappingHelper.Main, EnsureDriveLetterLowercase(Path.Combine(rootDirectoryMapping, SourceMappingHelper.Main)), frameEnumerator.Current, comparison);

                    }
                    runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterContinue();

                    using (IThreadInspector threadInspector = runner.GetThreadInspector())
                    {
                        IEnumerator<IFrameInspector> frameEnumerator = threadInspector.Stack.GetEnumerator();

                        // Move to first stack item
                        Assert.True(frameEnumerator.MoveNext());
                        this.Comment(string.Format(CultureInfo.InvariantCulture, "Verify source path for {0}.", SourceMappingHelper.Manager));
                        // Since file is not there, keep what was passed in
                        ValidateMappingToFrame(SourceMappingHelper.Manager, EnsureDriveLetterLowercase(Path.Combine(mgrDirectoryMapping, SourceMappingHelper.Manager)), frameEnumerator.Current, comparison);

                        // Move to second stack item
                        Assert.True(frameEnumerator.MoveNext());
                        this.Comment(string.Format(CultureInfo.InvariantCulture, "Verify source path for {0}.", SourceMappingHelper.Main));
                        // Since file is there, lowercase the drive letter
                        ValidateMappingToFrame(SourceMappingHelper.Main, EnsureDriveLetterLowercase(Path.Combine(rootDirectoryMapping, SourceMappingHelper.Main)), frameEnumerator.Current, comparison);
                    }

                    writerBreakpoints.Remove(9);
                    managerBreakpoints.Remove(8);
                    runner.SetBreakpoints(writerBreakpoints);
                    runner.SetBreakpoints(managerBreakpoints);

                    this.Comment("Continue to end");

                    runner.Expects.TerminatedEvent().AfterContinue();
                    runner.DisconnectAndVerify();
                }
                finally
                {
                    if (PlatformUtilities.IsWindows)
                    {
                        foreach (var dir in sourceMappings.Values)
                        {
                            if (Directory.Exists(dir))
                            {
                                Directory.Delete(dir, recursive: true);
                            }
                        }
                    }
                }
            }
        }
    }
}
