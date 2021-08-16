// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.OpenDebug.Commands.Responses;
using DebuggerTesting.OpenDebug.Events;
using Newtonsoft.Json;
using Xunit;
using static DebuggerTesting.OpenDebug.Commands.Responses.DisassembleResponseValue.Body;

namespace DebuggerTesting.OpenDebug.Extensions
{
    public interface IThreadInfo
    {
        int Id { get; }
        string Name { get; }
        IThreadInspector GetThreadInspector();
    }

    public interface IDisassemblyInstruction
    {
        public string Address { get; }
        public string InstructionBytes { get; }
        public string Instruction { get; }
        public string Symbol { get; }
        public Source Location { get; }
        public int? Line { get; }
        public int? Column { get; }
        public int? EndLine { get; }
        public int? EndColumn { get; }
    }

    public static class DebuggerRunnerExtensions
    {
        /// <summary>
        /// Provides an abstraction over the commands that
        /// retrieve debugger information on inspecting debuggee
        /// data when the debugger is in a break state.
        /// </summary>
        public static IThreadInspector GetThreadInspector(this IDebuggerRunner runner, int? threadId = null)
        {
            return new ThreadInspector(runner, threadId);
        }

        /// <summary>
        /// Verifies the names of the frames in the stack
        /// </summary>
        /// <param name="threadInspector">The thread to verify</param>
        /// <param name="useRegEx">Set to true if the frame names are regular expressions, otherwise expects exact match.</param>
        /// <param name="frameNames">The list of the frame names to verify.</param>
        public static void AssertStackFrameNames(this IThreadInspector threadInspector, bool useRegEx, params string[] frameNames)
        {
            Parameter.ThrowIfOutOfRange(frameNames.Length, 1, int.MaxValue, nameof(frameNames));

            int frameCount = 0;
            foreach (IFrameInspector frameInspector in threadInspector.Stack)
            {
                AssertFrameName(frameInspector, useRegEx, frameNames[frameCount]);

                frameCount++;
                if (frameCount >= frameNames.Length)
                    break;
            }
        }

        private static void AssertFrameName(this IFrameInspector frameInspector, bool useRegEx, string frameName)
        {
            if (useRegEx)
                Assert.Matches(frameName, frameInspector.Name);
            else
                Assert.Equal(frameName, frameInspector.Name);
        }

        /// <summary>
        /// Verify the child variables match the expected names and values.
        /// </summary>
        /// <param name="variableExpander">The parent to the variables</param>
        /// <param name="variables">An alternating list of variable name and variabl value.</param>
        public static void AssertVariables(this IVariableExpander variableExpander, params string[] variables)
        {
            if (variables.Length % 2 != 0)
            {
                throw new ArgumentException("Missing a property!");
            }

            for (int i = 0; i < variables.Length; i += 2)
            {
                string name = variables[i];
                string value = variables[i + 1];
                Assert.True(variableExpander.Variables.ContainsKey(name), "Expected variable name '{0}'.".FormatInvariantWithArgs(name));
                Assert.True(string.Equals(variableExpander.Variables[name].Value, value, StringComparison.Ordinal),
                            "Expected variable value '{0}' for '{1}'.  Actual value is '{2}'.".FormatInvariantWithArgs(value, name, variableExpander.Variables[name].Value));
            }
        }

        /// <summary>
        /// Get's a variable, but fails with more detailed message if it doesn't exist. Can be called with child variable names
        /// to follow variable expansion tree.
        /// </summary>
        public static IVariableInspector GetVariable(this IVariableExpander variableExpander, params string[] names)
        {
            Parameter.ThrowIfIsInvalid(names.Length, 0, nameof(names));

            IVariableInspector variableInspector = null;
            foreach (var name in names)
            {
                variableInspector = GetVariablePart(variableExpander, name);
                variableExpander = variableInspector;
            }
            return variableInspector;
        }

        private static IVariableInspector GetVariablePart(IVariableExpander variableExpander, string name)
        {
            IVariableInspector inspector;
            if (variableExpander.Variables.TryGetValue(name, out inspector))
                return inspector;

            string existingVariables = variableExpander.Variables.ToReadableString();
            throw new KeyNotFoundException("Variable " + name + " not found. Existing variables: " + existingVariables);
        }

        public static IEnumerable<IThreadInfo> GetThreads(this IDebuggerRunner runner)
        {
            ThreadsResponseValue threads = runner.RunCommand(new ThreadsCommand());
            return threads?.body?.threads?.Select(t => new ThreadInfo(runner, t.name, t.id ?? -1));
        }

        #region ThreadInfo

        internal class ThreadInfo : IThreadInfo
        {
            #region Constructor

            public ThreadInfo(IDebuggerRunner runner, string name, int id)
            {
                this.Runner = runner;
                this.Name = name;
                this.Id = id;
            }

            #endregion

            private IDebuggerRunner Runner { get; set; }
            public string Name { get; private set; }
            public int Id { get; private set; }

            public IThreadInspector GetThreadInspector()
            {
                return new ThreadInspector(this.Runner, this.Id);
            }
        }

        #endregion

        #region DisassembleInstruction

        internal class DisassemblyInstruction: IDisassemblyInstruction
        {
            public string Address { get; private set; }
            public string InstructionBytes { get; private set; }
            public string Instruction { get; private set; }
            public string Symbol { get; private set; }
            public Source Location { get; private set; }
            public int? Line { get; private set; }
            public int? Column { get; private set; }
            public int? EndLine { get; private set; }
            public int? EndColumn { get; private set; }

            public DisassemblyInstruction(DisassembledInstruction disassembledInstruction)
            {
                this.Address = disassembledInstruction.address;
                this.InstructionBytes = disassembledInstruction.instructionBytes;
                this.Instruction = disassembledInstruction.instruction;
                this.Symbol = disassembledInstruction.symbol;
                this.Location = disassembledInstruction.location;
                this.Line = disassembledInstruction.line;
                this.Column = disassembledInstruction.column;
                this.EndLine = disassembledInstruction.endLine;
                this.EndColumn = disassembledInstruction.endColumn;
            }
        }

        #endregion

        public static void RunCommandExpectFailure(this IDebuggerRunner runner, ICommand command)
        {
            command.ExpectsSuccess = false;
            runner.RunCommand(command);
        }

        public static SetBreakpointsResponseValue SetBreakpoints(this IDebuggerRunner runner, SourceBreakpoints sourceBreakpoints)
        {
            return runner.RunCommand(new SetBreakpointsCommand(sourceBreakpoints));
        }

        public static void SetExceptionBreakpoints(this IDebuggerRunner runner, params string[] filters)
        {
                runner.RunCommand(new SetExceptionBreakpointsCommand(filters));
        }

        public static SetBreakpointsResponseValue SetFunctionBreakpoints(this IDebuggerRunner runner, FunctionBreakpoints breakpoints)
        {
            return runner.RunCommand(new SetFunctionBreakpointsCommand(breakpoints));
        }

        public static SetBreakpointsResponseValue SetInstructionBreakpoints(this IDebuggerRunner runner, InstructionBreakpoints breakpoints)
        {
            return runner.RunCommand(new SetInstructionBreakpointsCommand(breakpoints));
        }

        public static string[] CompletionsRequest(this IDebuggerRunner runner, string text)
        {
            CompletionItem[] completionItems = runner.RunCommand(new CompletionsCommand(null, text, 0, null))?.body?.targets;
            return completionItems?.Select(x => x.label).ToArray();
        }

        public static void Continue(this IDebuggerRunner runner)
        {
            runner.RunCommand(new ContinueCommand(runner.StoppedThreadId));
        }

        public static void ConfigurationDone(this IDebuggerRunner runner)
        {
            runner.RunCommand(new ConfigurationDoneCommand());
        }

        public static ReadMemoryResponseValue ReadMemory(this IDebuggerRunner runner, string reference, int? offset, int count)
        {
            ReadMemoryResponseValue response = runner.RunCommand(new ReadMemoryCommand(reference, offset, count));
            return response;
        }

        public static IEnumerable<IDisassemblyInstruction> Disassemble(this IDebuggerRunner runner, string memoryReference, int instructionCount)
        {
            DisassembleResponseValue response = runner.RunCommand(new DisassembleCommand(memoryReference, 0, 0, instructionCount, false));
            return response?.body?.instructions.Select(i => new DisassemblyInstruction(i));
        }

        /// <summary>
        /// Adds an expected stop event within a range of line numbers. If the stop does not occur on <paramref name="targetLine"/>, then perform a step over
        /// and verify that the debuggee breaks at the <paramref name="targetLine"/>.
        /// </summary>
        /// <param name="startLine">The first line number onto which it is acceptable that the stop occurs.</param>
        /// <param name="targetLine">The line number which is the desired stopping line.</param>
        public static IRunBuilder ExpectStopAndStepToTarget(this IDebuggerRunner runner, StoppedReason reason, string fileName, int startLine, int targetLine)
        {
            return runner.Expects.HitStopEventWithinRange(reason, fileName, startLine, targetLine, (e) =>
            {
                if (e.ActualEvent.body.line != targetLine)
                {
                    runner.Expects.HitStepEvent(fileName, targetLine).AfterStepOver();
                }
            });
        }

        /// <summary>
        /// Adds an expected step event within a range of line numbers. If the step does not occur on <paramref name="targetLine"/>, then perform a step over
        /// and verify that the debuggee breaks at the <paramref name="targetLine"/>.
        /// </summary>
        public static IRunBuilder ExpectStepAndStepToTarget(this IDebuggerRunner runner, string fileName, int startLine, int targetLine)
        {
            return runner.ExpectStopAndStepToTarget(StoppedReason.Step, fileName, startLine, targetLine);
        }

        /// <summary>
        /// Adds an expected breakpoint event within a range of line numbers. If the breakpoint does not occur on <paramref name="targetLine"/>, then perform a step over
        /// and verify that the debuggee breaks at the <paramref name="targetLine"/>.
        /// </summary>
        public static IRunBuilder ExpectBreakpointAndStepToTarget(this IDebuggerRunner runner, string fileName, int startLine, int targetLine)
        {
            return runner.ExpectStopAndStepToTarget(StoppedReason.Breakpoint, fileName, startLine, targetLine);
        }
    }
}
