// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using DebuggerTesting;
using DebuggerTesting.Compilation;
using DebuggerTesting.OpenDebug;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.OpenDebug.CrossPlatCpp;
using DebuggerTesting.OpenDebug.Events;
using DebuggerTesting.OpenDebug.Extensions;
using DebuggerTesting.Ordering;
using Xunit;
using Xunit.Abstractions;

namespace CppTests.Tests
{
    [TestCaseOrderer(DependencyTestOrderer.TypeName, DependencyTestOrderer.AssemblyName)]
    public class EnvironmentTests : TestBase
    {
        #region Constructor

        public EnvironmentTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region Methods

        [Theory]
        [RequiresTestSettings]
        public void CompileKitchenSinkForEnvironmentTests(ITestSettings settings)
        {
            this.TestPurpose("Compiles the kitchen sink debuggee.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.OpenAndCompile(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Environment);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForEnvironmentTests))]
        [RequiresTestSettings]
        public void EnvironmentVariablesBasic(ITestSettings settings)
        {
            this.TestPurpose("Tests basic operation of environment variables");
            TestEnvironmentVariable(settings, "VAR_NAME_1", "simpleTest", false);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForEnvironmentTests))]
        [RequiresTestSettings]
        // Need to support 'runInTerminal' request.
        [UnsupportedDebugger(SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        public void EnvironmentVariablesBasicNewTerminal(ITestSettings settings)
        {
            this.TestPurpose("Tests basic operation of environment variables using a new terminal window");
            TestEnvironmentVariable(settings, "VAR_NAME_1", "simpleTest", true);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForEnvironmentTests))]
        [RequiresTestSettings]
        public void EnvironmentVariablesUndefined(ITestSettings settings)
        {
            this.TestPurpose("Tests environment variables that are undefined");
            TestEnvironmentVariable(settings, "VAR_NAME_1", null, false);
        }


        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForEnvironmentTests))]
        [RequiresTestSettings]
        // Need to support 'runInTerminal' request.
        [UnsupportedDebugger(SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        public void EnvironmentVariablesUndefinedNewTerminal(ITestSettings settings)
        {
            this.TestPurpose("Tests environment variables that are undefined using a new terminal window");
            TestEnvironmentVariable(settings, "VAR_NAME_1", null, true);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForEnvironmentTests))]
        [RequiresTestSettings]
        public void EnvironmentVariablesSingleQuote(ITestSettings settings)
        {
            this.TestPurpose("Tests environment variables that include a single quote");
            TestEnvironmentVariable(settings, "VAR_NAME_1", "quot'edstring", false);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForEnvironmentTests))]
        [RequiresTestSettings]
        // TODO There is a bug in LLDB: when executing the new terminal through the AppleScript,
        // it doesn't escape quotes and fails
                // TODO VsDbg: // Need to support 'runInTerminal' request.
        [UnsupportedDebugger(SupportedDebugger.Lldb | SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        public void EnvironmentVariablesSingleQuoteNewTerminal(ITestSettings settings)
        {
            this.TestPurpose("Tests environment variables that include a single quote in a new terminal window");
            TestEnvironmentVariable(settings, "VAR_NAME_1", "quot'edstring", true);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForEnvironmentTests))]
        [RequiresTestSettings]
        public void EnvironmentVariablesDoubleQuote(ITestSettings settings)
        {
            this.TestPurpose("Tests environment variables that include a double quote");
            TestEnvironmentVariable(settings, "VAR_NAME_1", "quot\"edstring", false);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForEnvironmentTests))]
        [RequiresTestSettings]
        // TODO There is a bug in LLDB: when executing the new terminal through the AppleScript,
        // it doesn't escape quotes and fails
        // TODO VsDbg: // Need to support 'runInTerminal' request.
        [UnsupportedDebugger(SupportedDebugger.Lldb | SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        public void EnvironmentVariablesDoubleQuoteNewTerminal(ITestSettings settings)
        {
            this.TestPurpose("Tests environment variables that include a double quote in a new terminal window");
            TestEnvironmentVariable(settings, "VAR_NAME_1", "quot\"edstring", true);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForEnvironmentTests))]
        [RequiresTestSettings]
        public void EnvironmentVariablesSpaces(ITestSettings settings)
        {
            this.TestPurpose("Tests environment variables that include a space");
            TestEnvironmentVariable(settings, "VAR_NAME_1", "string with spaces", false);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForEnvironmentTests))]
        [RequiresTestSettings]
        // Need to support 'runInTerminal' request.
        // darwin-debug does not support spaces in environment variables
        [UnsupportedDebugger(SupportedDebugger.VsDbg | SupportedDebugger.Lldb, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        public void EnvironmentVariablesSpacesNewTerminal(ITestSettings settings)
        {
            this.TestPurpose("Tests environment variables that include a space in a new terminal window");
            TestEnvironmentVariable(settings, "VAR_NAME_1", "string with spaces", true);
        }

        [Theory(Skip = "Need to modify the debuggee so that the name of the environment variable is not hardcoded.")]
        [DependsOnTest(nameof(CompileKitchenSinkForEnvironmentTests))]
        [RequiresTestSettings]
        [SupportedDebugger(SupportedDebugger.VsDbg, SupportedArchitecture.x86 | SupportedArchitecture.x64)]
        public void EnvironmentVariablesSpecialCharactersName(ITestSettings settings)
        {
            this.TestPurpose("Tests environment variables that include a space in a new terminal window");
            TestEnvironmentVariable(settings, "_(){}[]$*+-\\/\"#',;.@!?", "simpleValue", true);
        }

        private void TestEnvironmentVariable(ITestSettings settings, string variableName, string variableValue, bool newTerminal)
        {
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Environment);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, false, "-fEnvironment") { StopAtEntry = false };
                if (variableValue != null)
                {
                    launch.Args.environment = new EnvironmentEntry[] {
                        new EnvironmentEntry { Name = variableName, Value = variableValue }
                    };
                }

                launch.Args.externalConsole = newTerminal;
                runner.RunCommand(launch);

                this.Comment("Set breakpoint");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Environment, 14));

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();
                    this.Comment("Verify locals variables on current frame.");
                    currentFrame.AssertEvaluateAsString("varValue1", EvaluateContext.Watch, variableValue);
                }

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
