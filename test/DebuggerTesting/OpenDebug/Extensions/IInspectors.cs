// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using DebuggerTesting.OpenDebug.Commands;

namespace DebuggerTesting.OpenDebug.Extensions
{
    public interface IInspector
    {
        IDebuggerRunner DebuggerRunner { get; }
    }

    /// <summary>
    /// Provides an abstraction over the commands that
    /// retrieve debugger information on inspecting debuggee
    /// data when the debugger is in a break state.
    /// </summary>
    public interface IThreadInspector : IInspector, IDisposable
    {
        IEnumerable<IFrameInspector> Stack { get; }

        int ThreadId { get; }

        void Refresh();
    }

    public interface IVariableExpander
    {
        /// <summary>
        /// Gets the variables and their values on this frame
        /// </summary>
        IDictionary<string, IVariableInspector> Variables { get; }
    }

    /// <summary>
    /// Provides an abstraction over the commands that
    /// retrieve debugger information on a frame and
    /// variables in the frame's scope.
    /// </summary>
    public interface IFrameInspector : IVariableExpander, IInspector
    {
        /// <summary>
        /// The name of the frame. i.e. main(int argc, char ** argv)
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The OpenDebug id for the frame. Usually aroung 1000
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The source code file name
        /// </summary>
        string SourceName { get; }

        /// <summary>
        /// The full path to the source code file name
        /// </summary>
        string SourcePath { get; }

        /// <summary>
        /// The reference used to get more info on the source
        /// </summary>
        int? SourceReference { get; }

        int? Line { get; }

        int? Column { get; }

        string InstructionPointerReference {  get; }

        /// <summary>
        /// Evaluates an expression on this frame
        /// </summary>
        string Evaluate(string expression, EvaluateContext context = EvaluateContext.None);

        /// <summary>
        /// Gets the source associated with this frame
        /// </summary>
        string GetSourceContent();
    }

    /// <summary>
    /// Provides an abstraction over the commands that
    /// retrieve debugger information on a variable and
    /// can expand on child variable info.
    /// </summary>
    public interface IVariableInspector : IVariableExpander, IInspector
    {
        string Name { get; }
        string Value { get; set; }
        int? VariablesReference { get; }
        void SetVariableValueExpectFailure(string expression);
    }
}
