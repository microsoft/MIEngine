// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ExpressionTests : TestBase
    {
        #region Constructor

        public ExpressionTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        #endregion

        #region Methods

        [Theory]
        [RequiresTestSettings]
        public void CompileKitchenSinkForExpressionTests(ITestSettings settings)
        {
            this.TestPurpose("Compile kitchen sink debuggee for expression tests.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.OpenAndCompile(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Expression);
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForExpressionTests))]
        [RequiresTestSettings]
        public void LocalsBasic(ITestSettings settings)
        {
            this.TestPurpose("Check primitives displying in locals.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Expression);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch.");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fExpression");

                this.Comment("Set a line breakpoints so that we can stop.");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Expression, 31));

                this.Comment("To start debugging and break");
                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("To verify locals variables on current frame.");
                    Assert.Subset(new HashSet<string>() { "mybool", "mychar", "myint", "mywchar", "myfloat", "mydouble", "this" }, currentFrame.Variables.ToKeySet());
                    currentFrame.AssertVariables(
                        "mybool", "true",
                        "myint", "100");

                    currentFrame.GetVariable("mychar").AssertValueAsChar('A');
                    currentFrame.GetVariable("mywchar").AssertValueAsWChar('z');
                    currentFrame.GetVariable("myfloat").AssertValueAsFloat(299);
                    currentFrame.GetVariable("mydouble").AssertValueAsDouble(321);
                }

                this.Comment("Run to completion");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForExpressionTests))]
        [RequiresTestSettings]
        public void WatchBasic(ITestSettings settings)
        {
            this.TestPurpose("Evaluate some expressions in watch.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Expression);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fExpression");

                this.Comment("Set a line breakpoints so that we can stop.");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Expression, 31));

                this.Comment("To start debugging and break");
                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("To evaluate variables and functions in watch.");
                    string evalMyInt = currentFrame.Evaluate("myint-=100", EvaluateContext.Watch);
                    currentFrame.AssertEvaluateAsChar("mychar", EvaluateContext.Watch, 'A');
                    string evalMyBool = currentFrame.Evaluate("mybool", EvaluateContext.Watch);
                    currentFrame.AssertEvaluateAsWChar("mywchar", EvaluateContext.Watch, 'z');
                    currentFrame.AssertEvaluateAsDouble("mydouble", EvaluateContext.Watch, 321);
                    currentFrame.AssertEvaluateAsFloat("myfloat", EvaluateContext.Watch, 299);
                    string evalFuncMaxInt = currentFrame.Evaluate("Test::max(myint,1)", EvaluateContext.Watch);
                    currentFrame.AssertEvaluateAsDouble("Test::max(mydouble,0.0)-321.0", EvaluateContext.Watch, 0);

                    Assert.Equal("0", evalMyInt);
                    Assert.Equal("true", evalMyBool);
                    Assert.Equal("1", evalFuncMaxInt);
                }

                this.Comment("Run to completion");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForExpressionTests))]
        [RequiresTestSettings]
        public void DataTipBasic(ITestSettings settings)
        {
            this.TestPurpose("To test evaluation in datatip.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Expression);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch.");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fExpression");

                this.Comment("To set a breakpoint so that we can stop at somewhere for evaluation.");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Expression, 19));

                this.Comment("To start debugging and break");
                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame;

                    this.Comment("To evaluate in datatip on the first frame.");
                    currentFrame = threadInspector.Stack.First();
                    Assert.Equal("true", currentFrame.Evaluate("isCalled", EvaluateContext.DataTip));
                    currentFrame.AssertEvaluateAsDouble("d", EvaluateContext.DataTip, 10.1);

                    // We only verify the major contents in datatip
                    Assert.Contains(@"accumulate(int)", currentFrame.Evaluate("accumulate", EvaluateContext.DataTip), StringComparison.Ordinal);

                    this.Comment("To evaluate in datatip on the fourth frame.");
                    currentFrame = threadInspector.Stack.ElementAt(3);
                    currentFrame.AssertEvaluateAsDouble("mydouble", EvaluateContext.DataTip, double.PositiveInfinity);
                    currentFrame.AssertEvaluateAsChar("mynull", EvaluateContext.DataTip, '\0');

                    this.Comment("To evaluate in datatip on the fifth frame.");
                    currentFrame = threadInspector.Stack.ElementAt(4);
                    currentFrame.AssertEvaluateAsObject("student", EvaluateContext.DataTip, "name", @"""John""", "age", "10");
                    Assert.Matches(@"0x[0-9A-Fa-f]+", currentFrame.Evaluate("pStu", EvaluateContext.DataTip));

                    this.Comment("To evaluate in datatip on the sixth frame.");
                    currentFrame = threadInspector.Stack.ElementAt(5);
                    currentFrame.AssertEvaluateAsIntArray("arr", EvaluateContext.DataTip, 0, 1, 2, 3, 4);
                    Assert.Matches(@"0x[0-9A-Fa-f]+", currentFrame.Evaluate("pArr", EvaluateContext.DataTip));

                    this.Comment("To evaluate in datatip on the seventh frame.");
                    currentFrame = threadInspector.Stack.ElementAt(6);
                    Assert.Equal("true", currentFrame.Evaluate("mybool", EvaluateContext.DataTip));
                    Assert.Equal("100", currentFrame.Evaluate("myint", EvaluateContext.DataTip));
                    currentFrame.AssertEvaluateAsFloat("myfloat", EvaluateContext.DataTip, 299);
                    currentFrame.AssertEvaluateAsDouble("mydouble", EvaluateContext.DataTip, 321);
                    currentFrame.AssertEvaluateAsChar("mychar", EvaluateContext.DataTip, 'A');
                    currentFrame.AssertEvaluateAsWChar("mywchar", EvaluateContext.DataTip, 'z');
                }

                this.Comment("Continue to run to exist.");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForExpressionTests))]
        // TODO: https://github.com/microsoft/MIEngine/issues/1170
        // - lldb
        [UnsupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        [RequiresTestSettings]
        public void CallStackBasic(ITestSettings settings)
        {
            this.TestPurpose("To check all frames of callstack on a thead and evaluation on each frame.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Expression);

            this.Comment("Here are stack frames in a list we expect the actual to match with this.");
            StackFrame[] expectedstackFrames = ExpressionTests.GenerateFramesList(settings.DebuggerSettings);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch.");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fExpression");

                this.Comment("Set a breakpoint so that we can stop after starting debugging.");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Expression, 19));

                this.Comment("To start debugging and break");
                runner.Expects.StoppedEvent(StoppedReason.Breakpoint, SinkHelper.Expression, 19).AfterConfigurationDone();

                this.Comment("To step in several times into the innermost layer of a recursive call.");
                runner.ExpectStepAndStepToTarget(SinkHelper.Expression, 9, 10).AfterStepIn();
                runner.Expects.HitStepEvent(SinkHelper.Expression, 12).AfterStepIn();
                runner.ExpectStepAndStepToTarget(SinkHelper.Expression, 9, 10).AfterStepIn();
                runner.Expects.HitStepEvent(SinkHelper.Expression, 12).AfterStepIn();
                runner.ExpectStepAndStepToTarget(SinkHelper.Expression, 9, 10).AfterStepIn();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IEnumerable<IFrameInspector> stackOfCurrentThread = threadInspector.Stack;
                    this.Comment("To verify the count of stack frames count.");
                    Assert.True(stackOfCurrentThread.Count() >= 13, "Expected the stack frame count to be at least 13 deep");

                    this.Comment("To verify each frame, include frame name, line number and source name.");
                    int index = 0;
                    foreach (IFrameInspector frame in stackOfCurrentThread)
                    {
                        if (index >= 13)
                            break;
                        StackFrame expectedstackFrame = expectedstackFrames[index];
                        this.Comment("Comparing Names. Expecected: {0}, Actual: {1}", expectedstackFrame.Name, frame.Name);
                        Assert.Contains(expectedstackFrame.Name, frame.Name, StringComparison.Ordinal);
                        this.Comment("Comparing line number. Expecected: {0}, Actual: {1}", expectedstackFrame.Line, frame.Line);
                        Assert.Equal(expectedstackFrame.Line, frame.Line);
                        this.Comment("Comparing Source Name. Expecected: {0}, Actual: {1}", expectedstackFrame.SourceName, frame.SourceName);
                        Assert.Equal(expectedstackFrame.SourceName, frame.SourceName);
                        index++;
                    }
                }

                this.Comment("Continue to run to exist.");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForExpressionTests))]
        [RequiresTestSettings]
        public void EvaluateOnFrames(ITestSettings settings)
        {
            this.TestPurpose("To check evalution on different frame with different variable types.");
            this.WriteSettings(settings);

            this.Comment("Open the debugge with a initialization.");
            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Expression);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch.");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fExpression");

                this.Comment("Set a breakpoint so that we can stop at a line.");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Expression, 19));

                this.Comment("To start debugging and break");
                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame;

                    this.Comment("To evaluate on the first frame.");
                    currentFrame = threadInspector.Stack.First();
                    currentFrame.AssertVariables("isCalled", "true");
                    Assert.Equal("true", currentFrame.Evaluate("isCalled||false", EvaluateContext.Watch));
                    currentFrame.AssertEvaluateAsDouble("d", EvaluateContext.Watch, 10.1);

                    this.Comment("Switch to next frame then evaluate on that frame.");
                    currentFrame = threadInspector.Stack.ElementAt(1);
                    currentFrame.GetVariable("_f").AssertValueAsFloat(1);
                    currentFrame.AssertEvaluateAsFloat("_f", EvaluateContext.Watch, 1);

                    this.Comment("To evaluate string and vector type on it, the try to enable pretty printing to evaluate again.");
                    currentFrame = threadInspector.Stack.ElementAt(2);
                    currentFrame.GetVariable("vec").AssertValueAsVector(5);
                    currentFrame.AssertEvaluateAsVector("vec", EvaluateContext.Watch, 5);

                    this.Comment("To evaluate some special values on the fourth frame.");
                    currentFrame = threadInspector.Stack.ElementAt(3);
                    currentFrame.AssertEvaluateAsDouble("mydouble=1.0", EvaluateContext.Watch, 1);
                    currentFrame.GetVariable("mynull").AssertValueAsChar('\0');

                    this.Comment("To evaluate class on stack on the fifth frame.");
                    currentFrame = threadInspector.Stack.ElementAt(4);
                    IVariableInspector varInspector;
                    varInspector = currentFrame.GetVariable("student");
                    Assert.Equal("10", varInspector.GetVariable("age").Value);
                    Assert.Equal("19", currentFrame.Evaluate("student.age=19", EvaluateContext.Watch));

                    this.Comment("To evaluate array on the sixth frame.");
                    currentFrame = threadInspector.Stack.ElementAt(5);
                    Assert.Equal("10", currentFrame.Evaluate("*(pArr+1)=10", EvaluateContext.Watch));
                    Assert.Equal("100", currentFrame.Evaluate("arr[0]=100", EvaluateContext.Watch));
                }


                this.Comment("Continue to run to exist.");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForExpressionTests))]
        // TODO: https://github.com/microsoft/MIEngine/issues/1170
        // - lldb
        [UnsupportedDebugger(SupportedDebugger.Lldb, SupportedArchitecture.x64 | SupportedArchitecture.x86)]
        [RequiresTestSettings]
        public void EvaluateInvalidExpression(ITestSettings settings)
        {
            this.TestPurpose("To test invalid expression evaluation return apropriate errors.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Expression);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch.");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fExpression");

                this.Comment("Set a breakpoint so that we can stop at a line.");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Expression, 31));

                this.Comment("To start debugging and hit breakpoint.");
                runner.Expects.HitBreakpointEvent().AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("To evaluate some invalid expression on curren stack frame.");
                    currentFrame.AssertEvaluateAsError("notExistVar", EvaluateContext.Watch);
                }

                this.Comment("Continue to run to exist.");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        #region Assign expression

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForExpressionTests))]
        [RequiresTestSettings]
        public void AssignIntExpressionToVariable(ITestSettings settings)
        {
            this.TestPurpose("Assign an expression to a variable.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Expression);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch.");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fExpression");

                this.Comment("Set a breakpoint so that we can stop at a line.");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Expression, 31));

                this.Comment("Start debugging and hit breakpoint.");
                runner.Expects.HitBreakpointEvent().AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Assign an expression to a variable.");
                    currentFrame.GetVariable("myint").Value = "39+3";
                }

                // Start another inspector to refresh values
                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Check the value of the variable has been updated.");
                    Assert.Equal("42", currentFrame.GetVariable("myint").Value);
                }

                this.Comment("Continue to run to exist.");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        [Theory]
        [DependsOnTest(nameof(CompileKitchenSinkForExpressionTests))]
        [RequiresTestSettings]
        public void AssignInvalidExpressionToVariable(ITestSettings settings)
        {
            this.TestPurpose("Assign an invalid expression to a variable.");
            this.WriteSettings(settings);

            IDebuggee debuggee = SinkHelper.Open(this, settings.CompilerSettings, DebuggeeMonikers.KitchenSink.Expression);

            using (IDebuggerRunner runner = CreateDebugAdapterRunner(settings))
            {
                this.Comment("Configure launch.");
                runner.Launch(settings.DebuggerSettings, debuggee, "-fExpression");

                this.Comment("Set a breakpoint so that we can stop at a line.");
                runner.SetBreakpoints(debuggee.Breakpoints(SinkHelper.Expression, 31));

                this.Comment("Start debugging and hit breakpoint.");
                runner.Expects.StoppedEvent(StoppedReason.Breakpoint).AfterConfigurationDone();

                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Assign an invalid expression to a variable.");
                    currentFrame.GetVariable("myint").SetVariableValueExpectFailure("39+nonexistingint");
                }

                // Start another inspector to refresh values
                using (IThreadInspector threadInspector = runner.GetThreadInspector())
                {
                    IFrameInspector currentFrame = threadInspector.Stack.First();

                    this.Comment("Check the value of the variable hasn't been updated.");
                    Assert.Equal("100", currentFrame.GetVariable("myint").Value);
                }

                this.Comment("Continue to run to exist.");
                runner.Expects.TerminatedEvent().AfterContinue();

                runner.DisconnectAndVerify();
            }
        }

        #endregion

        #endregion

        #region Private methods

        private static StackFrame[] GenerateFramesList(IDebuggerSettings debugger)
        {
            // VsDbg moves the stack pointer to the return address which is not necessarily the calling address
            if (debugger.DebuggerType == SupportedDebugger.VsDbg)
            {
                // Visual C++ compiler for x64 and x86 generate symbols differently which means that the line numbers will be different.
                if (debugger.DebuggeeArchitecture == SupportedArchitecture.x64)
                {
                    return new[] {
                        new StackFrame(10, "accumulate(", "expression.cpp", null),
                        new StackFrame(12, "accumulate(", "expression.cpp", null),
                        new StackFrame(12, "accumulate(", "expression.cpp", null),
                        new StackFrame(20, "func(", "expression.cpp", null),
                        new StackFrame(81, "Expression::checkCallStack(", "expression.cpp", null),
                        new StackFrame(74, "Expression::checkPrettyPrinting(", "expression.cpp", null),
                        new StackFrame(63, "Expression::checkSpecialValues(", "expression.cpp", null),
                        new StackFrame(53, "Expression::checkClassOnStackAndHeap(", "expression.cpp", null),
                        new StackFrame(40, "Expression::checkArrayAndPointers(", "expression.cpp", null),
                        new StackFrame(32, "Expression::checkPrimitiveTypes(", "expression.cpp", null),
                        new StackFrame(86, "Expression::CoreRun(", "expression.cpp", null),
                        new StackFrame(23, "Feature::Run(", "feature.cpp", null),
                        new StackFrame(45, "main", "main.cpp", null)
                    };
                }
                else
                {
                    return new[] {
                        new StackFrame(10, "accumulate(", "expression.cpp", null),
                        new StackFrame(12, "accumulate(", "expression.cpp", null),
                        new StackFrame(12, "accumulate(", "expression.cpp", null),
                        new StackFrame(19, "func(", "expression.cpp", null),
                        new StackFrame(81, "Expression::checkCallStack(", "expression.cpp", null),
                        new StackFrame(75, "Expression::checkPrettyPrinting(", "expression.cpp", null),
                        new StackFrame(63, "Expression::checkSpecialValues(", "expression.cpp", null),
                        new StackFrame(54, "Expression::checkClassOnStackAndHeap(", "expression.cpp", null),
                        new StackFrame(40, "Expression::checkArrayAndPointers(", "expression.cpp", null),
                        new StackFrame(32, "Expression::checkPrimitiveTypes(", "expression.cpp", null),
                        new StackFrame(86, "Expression::CoreRun(", "expression.cpp", null),
                        new StackFrame(23, "Feature::Run(", "feature.cpp", null),
                        new StackFrame(46, "main", "main.cpp", null)
                    };
                }
            }
            else
            {
                return new[] {
                        new StackFrame(10, "accumulate(", "expression.cpp", null),
                        new StackFrame(12, "accumulate(", "expression.cpp", null),
                        new StackFrame(12, "accumulate(", "expression.cpp", null),
                        new StackFrame(19, "func(", "expression.cpp", null),
                        new StackFrame(80, "Expression::checkCallStack(", "expression.cpp", null),
                        new StackFrame(74, "Expression::checkPrettyPrinting(", "expression.cpp", null),
                        new StackFrame(62, "Expression::checkSpecialValues(", "expression.cpp", null),
                        new StackFrame(53, "Expression::checkClassOnStackAndHeap(", "expression.cpp", null),
                        new StackFrame(39, "Expression::checkArrayAndPointers(", "expression.cpp", null),
                        new StackFrame(31, "Expression::checkPrimitiveTypes(", "expression.cpp", null),
                        new StackFrame(85, "Expression::CoreRun(", "expression.cpp", null),
                        new StackFrame(22, "Feature::Run(", "feature.cpp", null),
                        new StackFrame(45, "main", "main.cpp", null)
                    };
            }
        }

        #endregion
    }

    #region Class of Stack Frame

    /// <summary>
    /// Represent a stack frame
    /// </summary>
    internal class StackFrame
    {
        public int Line { get; private set; }
        public string Name { get; private set; }
        public string SourceName { get; private set; }
        public string SourcePath { get; private set; }

        public StackFrame(int line, string name, string sourceName, string sourcePath)
        {
            this.Line = line;
            this.Name = name;
            this.SourceName = sourceName;
            this.SourcePath = sourcePath;
        }
    }

    #endregion

}