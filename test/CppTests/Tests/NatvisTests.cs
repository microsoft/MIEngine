// Copyright (c) Microsoft. All rights reserved.
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
        private const int ReturnSourceLine = 55;

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

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, visFile, false, true);
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
        public void TestIndexListItems(ITestSettings settings)
        {
            this.TestPurpose("This test checks if IndexListItems are visualized.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, NatvisName, DebuggeeMonikers.Natvis.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                string visFile = Path.Join(debuggee.SourceRoot, "visualizer_files", "Simple.natvis");

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, visFile, false, true);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(NatvisSourceName, ReturnSourceLine);
                runner.SetBreakpoints(writerBreakpoints);

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verifying IndexListItems natvis");
                    var arr = currentFrame.GetVariable("arr");
                    Assert.Equal("{ size=152 }", arr.Value);

                    // Index element for IndexListItems
                    // Natvis retrieves items in reverse order.
                    Assert.Equal("22801", arr.GetVariable("[0]").Value);
                    Assert.Equal("19881", arr.GetVariable("[10]").Value);

                    this.Comment("Verifying [More...]");
                    Assert.Equal("10000", arr.GetVariable("[More...]").GetVariable("[51]").Value);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileNatvisDebuggee))]
        [RequiresTestSettings]
        // Disable on macOS
        // Error:
        //   C-style cast from 'int' to 'int [10]' is not allowed
        //   (int[10])*(((vec)._start))
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

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, visFile, false, true);
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
                    Assert.Equal("{ size=52 }", ll.Value);

                    // Custom Item in natvis
                    Assert.Equal("52", ll.GetVariable("Size").Value);

                    // Index element for ArrayItems
                    Assert.Equal("20", ll.GetVariable("[5]").Value);

                    this.Comment("Verifying [More...]");
                    Assert.Equal("0", ll.GetVariable("[More...]").GetVariable("[51]").Value);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileNatvisDebuggee))]
        [RequiresTestSettings]
        public void TestLinkedListItems(ITestSettings settings)
        {
            this.TestPurpose("This test checks if LinkedListItems are visualized.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, NatvisName, DebuggeeMonikers.Natvis.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                string visFile = Path.Join(debuggee.SourceRoot, "visualizer_files", "Simple.natvis");

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, visFile, false, true);
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

                    this.Comment("Verifying [More...]");
                    Assert.Equal("75", ll.GetVariable("[More...]").GetVariable("[75]").Value);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileNatvisDebuggee))]
        [RequiresTestSettings]
        public void TestTreeItems(ITestSettings settings)
        {
            this.TestPurpose("This test checks if TreeItems are visualized.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, NatvisName, DebuggeeMonikers.Natvis.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                string visFile = Path.Join(debuggee.SourceRoot, "visualizer_files", "Simple.natvis");

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, visFile, false, true);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(NatvisSourceName, ReturnSourceLine);
                runner.SetBreakpoints(writerBreakpoints);

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    // This array is the sorted version of 'data' in natvis main.cpp
                    var sortedMapValues = new int[100] {
                        -489, -481, -478, -471, -437, -418, -399, -390, -388, -387,
                        -381, -368, -362, -350, -349, -346, -334, -314, -309, -305,
                        -282, -279, -278, -250, -205, -198, -197, -187, -172, -166,
                        -153, -148, -139, -120, -119, -105, -50, -43, -35, -25,
                        -23, -21, -19, -6, 6, 16, 17, 27, 30, 32,
                        35, 37, 44, 62, 76, 78, 86, 92, 96, 113,
                        121, 123, 141, 153, 187, 200, 215, 230, 234, 243,
                        254, 294, 299, 315, 323, 331, 336, 348, 364, 368,
                        369, 370, 375, 390, 392, 412, 427, 429, 433, 435,
                        446, 449, 461, 467, 472, 475, 485, 490, 497, 498
                    };

                    this.Comment("Verifying TreeItems natvis");
                    var map = currentFrame.GetVariable("map");

                    // Custom Item in natvis
                    Assert.Equal("100", map.GetVariable("Count").Value);

                    // Index element for TreeItems
                    // Visualized map will show the BST in a flat ordered list.
                    for (int i = 0; i < 50; i++)
                    {
                        Assert.Equal(sortedMapValues[i].ToString(), map.GetVariable("[" + i + "]").Value);
                    }

                    var more = map.GetVariable("[More...]");
                    for (int i = 50; i < 100; i++)
                    {
                        Assert.Equal(sortedMapValues[i].ToString(), more.GetVariable("[" + i + "]").Value);
                    }
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileNatvisDebuggee))]
        [RequiresTestSettings]
        public void TestThisConditional(ITestSettings settings)
        {
            this.TestPurpose("This test checks if 'this' in conditional expressions are evaluated.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, NatvisName, DebuggeeMonikers.Natvis.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                string visFile = Path.Join(debuggee.SourceRoot, "visualizer_files", "Simple.natvis");

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, visFile, false, true);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint before assigning 'simpleClass' and end of method.");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(NatvisSourceName, new int[] { 53, ReturnSourceLine });
                runner.SetBreakpoints(writerBreakpoints);

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verifying SimpleClass natvis with condition before initialization");
                    var simpleClass = currentFrame.GetVariable("simpleClass");

                    // Custom Item in natvis
                    Assert.Equal("Null Class", simpleClass.Value);
                }

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterContinue();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verifying SimpleClass natvis with condition after initialization");
                    var simpleClass = currentFrame.GetVariable("simpleClass");

                    // Custom Item in natvis
                    Assert.Equal("Non-null Class", simpleClass.Value);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileNatvisDebuggee))]
        [RequiresTestSettings]
        public void TestNonDisplayString(ITestSettings settings)
        {
            this.TestPurpose("This test checks if we can expand [More...] multiple times and its items are visualized.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, NatvisName, DebuggeeMonikers.Natvis.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                string visFile = Path.Join(debuggee.SourceRoot, "visualizer_files", "Simple.natvis");

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, visFile, false, false);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(NatvisSourceName, ReturnSourceLine);
                runner.SetBreakpoints(writerBreakpoints);

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verifying IndexListItems natvis");
                    var arr = currentFrame.GetVariable("arr");
                    Assert.Equal("{...}", arr.Value);

                    Assert.Equal("152", arr.GetVariable("_size").Value);


                    var visView = arr.GetVariable("[Visualized View]");

                    // Index element for IndexListItems
                    // Natvis retrieves items in reverse order.
                    Assert.Equal("22801", visView.GetVariable("[0]").Value);
                    Assert.Equal("19881", visView.GetVariable("[10]").Value);

                    var more1 = visView.GetVariable("[More...]");
                    Assert.Equal("10000", more1.GetVariable("[51]").Value);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileNatvisDebuggee))]
        [RequiresTestSettings]
        public void TestMultipleMoreExpands(ITestSettings settings)
        {
            this.TestPurpose("This test checks if we can expand [More...] multiple times and its items are visualized.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, NatvisName, DebuggeeMonikers.Natvis.Default);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                string visFile = Path.Join(debuggee.SourceRoot, "visualizer_files", "Simple.natvis");

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, visFile, false, true);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(NatvisSourceName, ReturnSourceLine);
                runner.SetBreakpoints(writerBreakpoints);

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verifying IndexListItems natvis");
                    var arr = currentFrame.GetVariable("arr");
                    Assert.Equal("{ size=152 }", arr.Value);

                    // Index element for IndexListItems
                    // Natvis retrieves items in reverse order.
                    Assert.Equal("22801", arr.GetVariable("[0]").Value);
                    Assert.Equal("19881", arr.GetVariable("[10]").Value);

                    var more1 = arr.GetVariable("[More...]");
                    Assert.Equal("10000", more1.GetVariable("[51]").Value);

                    var more2 = more1.GetVariable("[More...]");
                    Assert.Equal("4", more2.GetVariable("[149]").Value);

                    var more3 = more2.GetVariable("[More...]");
                    Assert.Equal("0", more3.GetVariable("[151]").Value);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileNatvisDebuggee))]
        [RequiresTestSettings]
        // Disable on macOS
        [UnsupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        public void TestArrayPointer(ITestSettings settings)
        {
            this.TestPurpose("This test checks if the comma format specifier is visualized.");
            this.WriteSettings(settings);

            IDebuggee debuggee = Debuggee.Open(this, settings.CompilerSettings, NatvisName, DebuggeeMonikers.Natvis.Default);

            this.Comment("Run the debuggee, check argument count");
            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(NatvisSourceName, ReturnSourceLine);
                runner.SetBreakpoints(writerBreakpoints);

                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Verifying comma format specifier");
                    int[] expected = { 0, 1, 4, 9 };
                    currentFrame.AssertEvaluateAsIntArray("arr._array,4", EvaluateContext.Watch, expected);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }


        #endregion
    }
}
