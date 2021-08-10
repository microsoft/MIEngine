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
    public class MemoryTests : TestBase
    {
        #region Constructor

        public MemoryTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region Methods

        [Theory]
        [RequiresTestSettings]
        public void CompileKitchenSinkForBreakpointTests(ITestSettings settings)
        {
            this.TestPurpose("Compiles the kitchen sink debuggee.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.OpenAndCompile(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForBreakpointTests))]
        [RequiresTestSettings]
        public void InstructionBreakpointsBasic(ITestSettings settings)
        {
            this.TestPurpose("Tests basic operation of instruction breakpoints");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Breakpoint);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fCalling");

                SourceBreakpoints mainBreakpoints = debuggee.Breakpoints(SinkHelper.Main, 33);

                this.Comment("Set initial breakpoints");
                runner.SetBreakpoints(mainBreakpoints);

                this.Comment("Launch and run until first breakpoint");
                runner.Expects.HitBreakpointEvent(SinkHelper.Main, 33)
                              .AfterConfigurationDone();

                this.Comment("Inspect the stack and try evaluation.");
                using (IThreadInspector inspector = runner.GetThreadInspector())
                {
                    this.Comment("Get the stack trace");
                    IFrameInspector mainFrame = inspector.Stack.First();
                    inspector.AssertStackFrameNames(true, "main.*");

                    this.WriteLine("Main frame: {0}", mainFrame);
                    string ip = mainFrame.InstructionPointerReference;

                    // Send Disassemble Request to get the current instruction and next one.
                    IEnumerable<IDisassemblyInstruction> instructions = runner.Disassemble(ip, 2);

                    // Validate that we got two instructions.
                    Assert.Equal(2, instructions.Count());

                    // Get the next instruction's address
                    string nextIPAddress = instructions.Last().Address;

                    // Set an instruction breakpoint
                    InstructionBreakpoints instruction = new InstructionBreakpoints(new string[] { nextIPAddress });
                    runner.SetInstructionBreakpoints(instruction);

                    // Expect it to be hit.
                    runner.Expects.HitInstructionBreakpointEvent(nextIPAddress).AfterContinue();
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
