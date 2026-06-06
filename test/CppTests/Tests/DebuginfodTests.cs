// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using DebuggerTesting;
using DebuggerTesting.Compilation;
using DebuggerTesting.OpenDebug;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.OpenDebug.CrossPlatCpp;
using DebuggerTesting.OpenDebug.Events;
using DebuggerTesting.OpenDebug.Extensions;
using DebuggerTesting.Ordering;
using DebuggerTesting.Settings;
using Xunit;
using Xunit.Abstractions;

namespace CppTests.Tests
{
    /// <summary>
    /// Tests that validate debuginfod settings don't cause GDB to hang when
    /// debuginfod servers are unreachable.
    /// </summary>
    [TestCaseOrderer(DependencyTestOrderer.TypeName, DependencyTestOrderer.AssemblyName)]
    public class DebuginfodTests : TestBase
    {
        #region Constructor

        public DebuginfodTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        private const string DebuggeeName = "debuginfod";
        private const string SourceFileName = "debuginfod_test.cpp";

        // RFC 5737 TEST-NET-1: guaranteed non-routable, connections will be dropped
        private const string UnreachableDebuginfodUrl = "http://192.0.2.1:8002";

        #region Tests

        [Theory]
        [RequiresTestSettings]
        public void CompileDebuginfodDebuggee(ITestSettings settings)
        {
            this.TestPurpose("Create and compile the debuginfod test debuggee");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Create(this, settings.CompilerSettings, DebuggeeName, DebuggeeMonikers.Debuginfod.Default);
            debuggee.AddSourceFiles(SourceFileName);
            debuggee.Compile();
        }

        /// <summary>
        /// Tests that disabling debuginfod prevents hangs when stepping into library code
        /// with DEBUGINFOD_URLS pointing to an unreachable server.
        /// </summary>
        [Theory]
        [DependsOnTest(nameof(CompileDebuginfodDebuggee))]
        [RequiresTestSettings]
        [UnsupportedDebugger(SupportedDebugger.VsDbg | SupportedDebugger.Lldb, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void DebuginfodDisabledDoesNotHang(ITestSettings settings)
        {
            this.TestPurpose("Verify that disabling debuginfod prevents GDB from hanging when stepping into library code.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, DebuggeeName, DebuggeeMonikers.Debuginfod.Default);

            string originalUrl = Environment.GetEnvironmentVariable("DEBUGINFOD_URLS");
            string originalVerbose = Environment.GetEnvironmentVariable("DEBUGINFOD_VERBOSE");
            try
            {
                Environment.SetEnvironmentVariable("DEBUGINFOD_URLS", UnreachableDebuginfodUrl);
                Environment.SetEnvironmentVariable("DEBUGINFOD_VERBOSE", "1");

                Stopwatch sw = Stopwatch.StartNew();

                using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
                {
                    this.Comment("Launch with debuginfod disabled");
                    LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath);
                    launch.Args.debuginfod = new DebuginfodArgs { Enabled = false };
                    runner.RunCommand(launch);

                    this.Comment("Set breakpoint at regex_search call and run to it");
                    runner.SetBreakpoints(debuggee.Breakpoints(SourceFileName, 13));
                    runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                    this.Comment("Step into std::regex_search to trigger debuginfod lookup");
                    runner.Expects.StoppedEvent(StoppedReason.Step).AfterStepIn();

                    sw.Stop();
                    this.Comment($"Step into library completed in {sw.ElapsedMilliseconds}ms");

                    Assert.True(sw.ElapsedMilliseconds < 10000,
                        $"Step into library took {sw.ElapsedMilliseconds}ms with debuginfod disabled. " +
                        "Expected < 10s. Debuginfod may not be properly disabled.");

                    runner.Expects.ExitedEvent().TerminatedEvent().AfterContinue();
                    runner.DisconnectAndVerify();
                }

                string engineLogPath = Path.Combine(PathSettings.TempPath,
                    $"EngineLog-{nameof(DebuginfodDisabledDoesNotHang)}-{settings.DebuggerSettings.DebuggeeArchitecture}-{settings.DebuggerSettings.DebuggerType}.log");
                if (File.Exists(engineLogPath))
                {
                    string logContent = File.ReadAllText(engineLogPath);
                    this.Comment("Verifying debuginfod was NOT enabled in GDB");
                    Assert.DoesNotContain("set debuginfod enabled on", logContent);
                    Assert.Contains("set debuginfod enabled off", logContent);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("DEBUGINFOD_URLS", originalUrl);
                Environment.SetEnvironmentVariable("DEBUGINFOD_VERBOSE", originalVerbose);
            }
        }

        /// <summary>
        /// Tests that a short debuginfod timeout prevents hangs when stepping into library code
        /// with DEBUGINFOD_URLS pointing to an unreachable server.
        /// </summary>
        [Theory]
        [DependsOnTest(nameof(CompileDebuginfodDebuggee))]
        [RequiresTestSettings]
        [UnsupportedDebugger(SupportedDebugger.VsDbg | SupportedDebugger.Lldb | SupportedDebugger.Gdb_MinGW | SupportedDebugger.Gdb_Cygwin, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void DebuginfodTimeoutPreventsHang(ITestSettings settings)
        {
            this.TestPurpose("Verify that debuginfod timeout prevents GDB from hanging when stepping into library code.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, DebuggeeName, DebuggeeMonikers.Debuginfod.Default);

            string originalUrl = Environment.GetEnvironmentVariable("DEBUGINFOD_URLS");
            string originalVerbose = Environment.GetEnvironmentVariable("DEBUGINFOD_VERBOSE");
            try
            {
                Environment.SetEnvironmentVariable("DEBUGINFOD_URLS", UnreachableDebuginfodUrl);
                Environment.SetEnvironmentVariable("DEBUGINFOD_VERBOSE", "1");

                Stopwatch sw = Stopwatch.StartNew();

                using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
                {
                    this.Comment("Launch with debuginfod enabled but short timeout (5s)");
                    LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath);
                    launch.Args.debuginfod = new DebuginfodArgs { Enabled = true, Timeout = 5 };
                    launch.Args.setupCommands = new SetupCommandArg[]
                    {
                        new SetupCommandArg { Text = "set debuginfod verbose 1", IgnoreFailures = true }
                    };
                    runner.RunCommand(launch);

                    this.Comment("Set breakpoint at regex_search call and run to it");
                    runner.SetBreakpoints(debuggee.Breakpoints(SourceFileName, 13));
                    runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                    this.Comment("Step into std::regex_search to trigger debuginfod lookup");
                    runner.Expects.StoppedEvent(StoppedReason.Step).AfterStepIn();

                    sw.Stop();
                    this.Comment($"Step into library completed in {sw.ElapsedMilliseconds}ms");

                    Assert.True(sw.ElapsedMilliseconds < 30000,
                        $"Step into library took {sw.ElapsedMilliseconds}ms with debuginfod timeout=5s. " +
                        "Expected < 30s. The timeout may not be applied correctly.");

                    runner.Expects.ExitedEvent().TerminatedEvent().AfterContinue();
                    runner.DisconnectAndVerify();
                }

                string engineLogPath = Path.Combine(PathSettings.TempPath,
                    $"EngineLog-{nameof(DebuginfodTimeoutPreventsHang)}-{settings.DebuggerSettings.DebuggeeArchitecture}-{settings.DebuggerSettings.DebuggerType}.log");
                if (File.Exists(engineLogPath))
                {
                    string logContent = File.ReadAllText(engineLogPath);
                    this.Comment("Verifying debuginfod was enabled in GDB");
                    Assert.Contains("debuginfod enabled", logContent);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("DEBUGINFOD_URLS", originalUrl);
                Environment.SetEnvironmentVariable("DEBUGINFOD_VERBOSE", originalVerbose);
            }
        }

        #endregion
    }
}
