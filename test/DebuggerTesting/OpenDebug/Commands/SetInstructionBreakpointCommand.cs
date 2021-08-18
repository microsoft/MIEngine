// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting.OpenDebug.Commands.Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebuggerTesting.OpenDebug.Commands
{

    #region SetInstructionBreakpointCommandArgs

    public sealed class SetInstructionBreakpointCommandArgs : JsonValue
    {
        public sealed class InstructionBreakpoint
        {
            public InstructionBreakpoint(string instructionReference, string condition = null)
            {
                this.instructionReference = instructionReference;
                this.condition = condition;
            }

            public string instructionReference;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int? offset;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string condition;
        }

        public InstructionBreakpoint[] breakpoints;
    }

    #endregion

    #region InstructionBreakpoints

    public sealed class InstructionBreakpoints
    {
        private List<SetInstructionBreakpointCommandArgs.InstructionBreakpoint> breakpoints;

        public InstructionBreakpoints()
        {
            this.breakpoints = new List<SetInstructionBreakpointCommandArgs.InstructionBreakpoint>();
        }

        public InstructionBreakpoints(params string[] addresses)
            : this()
        {
            foreach (string address in addresses)
            {
                this.Add(address);
            }
        }

        public InstructionBreakpoints Add(string address, string condition = null)
        {
            this.breakpoints.Add(new SetInstructionBreakpointCommandArgs.InstructionBreakpoint(address, condition));
            return this;
        }

        public InstructionBreakpoints Remove(string address)
        {
            this.breakpoints.RemoveAll(bp => String.Equals(bp.instructionReference, address, StringComparison.Ordinal));
            return this;
        }

        internal IList<SetInstructionBreakpointCommandArgs.InstructionBreakpoint> Breakpoints
        {
            get { return this.breakpoints; }
        }
    }

    #endregion

    public class SetInstructionBreakpointsCommand : CommandWithResponse<SetInstructionBreakpointCommandArgs, SetBreakpointsResponseValue>
    {
        public SetInstructionBreakpointsCommand() : base("setInstructionBreakpoints")
        {
        }

        public SetInstructionBreakpointsCommand(InstructionBreakpoints breakpoints) :
            this()
        {
            this.Args.breakpoints = breakpoints.Breakpoints.ToArray();
        }

        public override string ToString()
        {
            return "{0} ({1})".FormatInvariantWithArgs(base.ToString(), String.Join(", ", this.Args.breakpoints.Select(bp => bp.instructionReference)));
        }
    }
}