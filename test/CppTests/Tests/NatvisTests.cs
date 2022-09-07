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
        private const int ReturnSourceLine = 48;

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
        public void TestIndexListItems(ITestSettings settings)
        {
            this.TestPurpose("This test checks if IndexListItems are visualized.");
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

                    this.Comment("Verifying IndexListItems natvis");
                    var arr = currentFrame.GetVariable("arr");
                    Assert.Equal("{ size=52 }", arr.Value);

                    // Index element for IndexListItems
                    // Natvis retrieves items in reverse order.
                    Assert.Equal("2601", arr.GetVariable("[0]").Value);
                    Assert.Equal("1681", arr.GetVariable("[10]").Value);
                    Assert.Equal("0", arr.GetVariable("[More...]").GetVariable("[51]").Value);
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
                    Assert.Equal("{ size=2000 }", ll.Value);

                    // Custom Item in natvis
                    Assert.Equal("2000", ll.GetVariable("Size").Value);

                    // Index element for ArrayItems
                    Assert.Equal("20", ll.GetVariable("[5]").Value);
                    Assert.Equal("51", ll.GetVariable("[More...]").GetVariable("[51]").Value);

                    // Multi-dimensional array
                    var c = currentFrame.GetVariable("c");
                    Assert.Equal("3", c.GetVariable("[1,1]").Value);
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
        public void Test2000ArrayItems(ITestSettings settings)
        {
            this.TestPurpose("This test checks if ArrayItems can be visualized past 1000 elements.");
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
                    Assert.Equal("{ size=2000 }", ll.Value);

                    // Custom Item in natvis
                    Assert.Equal("2000", ll.GetVariable("Size").Value);

                    var more = ll.GetVariable("[More...]");
                    for (int i = 1; i < 40; i++)
                    {
                        string idx = (i * 50).ToString();
                        this.Comment($"Verifying ArrayItems [More...] for {idx}");
                        Assert.Equal(idx, more.GetVariable("[" + idx + "]").Value);
                        if (i == 39)
                        {
                            // For 1950 to 1999 there should not be a [More...] node.
                            try
                            {
                                more = more.GetVariable("[More...]");
                                Assert.True(false, "Unexpected [More...] node for the last page.");
                            }
                            catch (KeyNotFoundException)
                            {
                                // Success
                            }
                        }
                        else
                        {
                            more = more.GetVariable("[More...]");
                        }
                    }
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

                LaunchCommand launch = new LaunchCommand(settings.DebuggerSettings, debuggee.OutputPath, visFile, false);
                runner.RunCommand(launch);

                this.Comment("Set Breakpoint before assigning 'simpleClass' and end of method.");
                SourceBreakpoints writerBreakpoints = debuggee.Breakpoints(NatvisSourceName, new int[] { 46, ReturnSourceLine });
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
        public void TestCommaFormatSpecifier(ITestSettings settings)
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

        [Theory]
        [DependsOnTest(nameof(CompileNatvisDebuggee))]
        [RequiresTestSettings]
        public void TestCommaFormatWithSquareBrackets(ITestSettings settings)
        {
            this.TestPurpose("This test checks if the comma format specifier with square brackets is visualized.");
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
                    currentFrame.AssertEvaluateAsIntArray("arr._array,[4]", EvaluateContext.Watch, expected);
                }

                runner.Expects.ExitedEvent(exitCode: 0).TerminatedEvent().AfterContinue();
                runner.DisconnectAndVerify();
            }
        }

        #endregion
    }
}
