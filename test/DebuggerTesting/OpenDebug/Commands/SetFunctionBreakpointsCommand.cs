// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using DebuggerTesting.OpenDebug.Commands.Responses;
using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands
{
    #region SetBreakpointsCommandArgs

    public sealed class SetFunctionBreakpointsCommandArgs : JsonValue
    {
        public sealed class FunctionBreakpoint
        {
            public FunctionBreakpoint(string name, string condition = null)
            {
                this.name = name;
                this.condition = condition;
            }

            public string name;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string condition;
        }

        public FunctionBreakpoint[] breakpoints;
    }

    #endregion

    #region FunctionBreakpoints

    public sealed class FunctionBreakpoints
    {
        private List<SetFunctionBreakpointsCommandArgs.FunctionBreakpoint> breakpoints;

        public FunctionBreakpoints()
        {
            this.breakpoints = new List<SetFunctionBreakpointsCommandArgs.FunctionBreakpoint>();
        }

        public FunctionBreakpoints(params string[] functions)
            : this()
        {
            foreach (string function in functions)
            {
                this.Add(function);
            }
        }

        public FunctionBreakpoints Add(string function, string condition = null)
        {
            this.breakpoints.Add(new SetFunctionBreakpointsCommandArgs.FunctionBreakpoint(function, condition));
            return this;
        }

        public FunctionBreakpoints Remove(string function)
        {
            this.breakpoints.RemoveAll(bp => String.Equals(bp.name, function, StringComparison.Ordinal));
            return this;
        }

        internal IList<SetFunctionBreakpointsCommandArgs.FunctionBreakpoint> Breakpoints
        {
            get { return this.breakpoints; }
        }
    }

    #endregion

    public class SetFunctionBreakpointsCommand : CommandWithResponse<SetFunctionBreakpointsCommandArgs, SetBreakpointsResponseValue>
    {
        public SetFunctionBreakpointsCommand() : base("setFunctionBreakpoints")
        {
        }

        public SetFunctionBreakpointsCommand(FunctionBreakpoints breakpoints) :
            this()
        {
            this.Args.breakpoints = breakpoints.Breakpoints.ToArray();
        }

        public override string ToString()
        {
            return "{0} ({1})".FormatInvariantWithArgs(base.ToString(), String.Join(", ", this.Args.breakpoints.Select(bp => bp.name)));
        }
    }
}
