﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public class NatvisTests : TestBase
    {
        #region Constructor

        public NatvisTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region Methods


        private const string NatvisName = "natvis";
        private const string NatvisSourceName = "main.cpp";
        private const int ReturnSourceLine = 33;

        [Theory]
        [RequiresTestSettings]
        public void CompileNatvisDebuggee(ITestSettings settings)
        {
            this.TestPurpose("Create and compile the 'natvis' debuggee");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Create(this, settings.CompilerSettings, NatvisName, DebuggeeMonikers.Natvis.Default);
            debuggee.AddSourceFiles(NatvisSourceName);
            debuggee.Compile();
        }

        [Theory]
        [DependsOnTest(nameof(CompileNatvisDebuggee))]
        [RequiresTestSettings]
        [UnsupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void TestDisplayString(ITestSettings settings)
        {
            this.TestPurpose("This test checks if DisplayString are visualized.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, NatvisName, DebuggeeMonikers.Natvis.Default);

            this.Comment("Run the debuggee, check argument count");
            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                string visFile = Path.Join(debuggee.SourceRoot, "visualizer_files", "Simple.natvis");

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, visFile, false);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(NatvisSourceName, ReturnSourceLine);
                runner.SetBreakpoints(writerBreakpoints);

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verifying 'DisplayString' natvis");
                    Assert.Equal("Hello DisplayString", currentFrame.GetVariable("obj_1").Value);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileNatvisDebuggee))]
        [RequiresTestSettings]
        [UnsupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void TestArrayItems(ITestSettings settings)
        {
            this.TestPurpose("This test checks if ArrayItems are visualized.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, NatvisName, DebuggeeMonikers.Natvis.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                string visFile = Path.Join(debuggee.SourceRoot, "visualizer_files", "Simple.natvis");

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, visFile, false);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(NatvisSourceName, ReturnSourceLine);
                runner.SetBreakpoints(writerBreakpoints);

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verifying ArrayItems natvis");
                    var ll = currentFrame.GetVariable("vec");
                    Assert.Equal("{ size=10 }", ll.Value);

                    // Custom Item in natvis
                    Assert.Equal("10", ll.GetVariable("Size").Value);

                    // Index element for ArrayItems
                    Assert.Equal("20", ll.GetVariable("[5]").Value);
                    // TODO: Add test below when we can support the [More..] expansion to handle >50 elements
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileNatvisDebuggee))]
        [RequiresTestSettings]
        [UnsupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void TestLinkedListItems(ITestSettings settings)
        {
            this.TestPurpose("This test checks if LinkedListItems are visualized.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, NatvisName, DebuggeeMonikers.Natvis.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                string visFile = Path.Join(debuggee.SourceRoot, "visualizer_files", "Simple.natvis");

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, visFile, false);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(NatvisSourceName, ReturnSourceLine);
                runner.SetBreakpoints(writerBreakpoints);

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verifying LinkedListItems natvis");
                    var ll = currentFrame.GetVariable("ll");
                    Assert.Equal("{ size=100 }", ll.Value);

                    // Custom Item in natvis
                    Assert.Equal("100", ll.GetVariable("Count").Value);

                    // Index element for LinkedListItems
                    Assert.Equal("5", ll.GetVariable("[5]").Value);
                    // TODO: Uncomment line below when we can support the [More..] expansion to handle >50 elements
                    // Assert.Equal("75", ll.GetVariable("[More...]").GetVariable("[75]").Value);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileNatvisDebuggee))]
        [RequiresTestSettings]
        [UnsupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void TestTreeItems(ITestSettings settings)
        {
            this.TestPurpose("This test checks if TreeItems are visualized.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, NatvisName, DebuggeeMonikers.Natvis.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                string visFile = Path.Join(debuggee.SourceRoot, "visualizer_files", "Simple.natvis");

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, visFile, false);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(NatvisSourceName, ReturnSourceLine);
                runner.SetBreakpoints(writerBreakpoints);

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verifying TreeItems natvis");
                    var map = currentFrame.GetVariable("map");

                    // Custom Item in natvis
                    Assert.Equal("6", map.GetVariable("Count").Value);

                    // Index element for TreeItems
                    // Visualized map will show the BST in a flat ordered list.
                    // Values are inserted as [0, -100, 15, -35, 4, -72]
                    // Expected visualize list to be [-100, -72, -35, 0, 4, 15]
                    Assert.Equal("-100", map.GetVariable("[0]").Value);
                    Assert.Equal("0", map.GetVariable("[3]").Value);
                    Assert.Equal("15", map.GetVariable("[5]").Value);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        #endregion
    }
}