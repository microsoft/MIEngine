// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
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
    public class MacOSAppTests : TestBase
    {
        #region Constructor

        public MacOSAppTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region Methods

        [Theory]
        [RequiresTestSettings]
        [SupportedCompiler(SupportedCompiler.XCodeBuild, SupportedArchitecture.x64)]
        public void CompileMacOSAppForTests(ITestSettings settings)
        {
            this.TestPurpose("Compile MacOSApp debuggee for tests.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Create(this, settings.CompilerSettings, "MacOSApp", 1, "TestApp.app", CompilerOutputType.MacOSApp);
            
            debuggee.AddSourceFiles("TestApp.xcodeproj");

            debuggee.Compile();
        }

        [Theory]
        [DependsOnTest(nameof(CompileMacOSAppForTests))]
        [RequiresTestSettings]
        [SupportedCompiler(SupportedCompiler.XCodeBuild, SupportedArchitecture.x64)]
        public void MacOSAppBasic(ITestSettings settings)
        {
            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, "MacOSApp", 1, "TestApp.app", CompilerOutputType.MacOSApp);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch.");
                runner.Launch(settings.DebuggerSettings, debuggee, null);

                SourceBreakpoints mainBreakpoints = debuggee.Breakpoints(SinkHelper.Main, 14);
                runner.SetBreakpoints(mainBreakpoints);

                this.Comment("Launch and run until first breakpoint");
                runner.Expects.HitBreakpointEvent(SinkHelper.Main, 14)
                              .AfterConfigurationDone();

                this.Comment("Continue until end");
                    runner.Expects.ExitedEvent()
                                .TerminatedEvent()
                                .AfterContinue();

                    runner.DisconnectAndVerify();
            }
        }

        #endregion
    }
}
