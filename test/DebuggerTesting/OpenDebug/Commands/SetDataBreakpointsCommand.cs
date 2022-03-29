// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DebuggerTesting.Compilation;
using DebuggerTesting.OpenDebug.Commands.Responses;
using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands
{

    #region SetBreakpointsCommandArgs

    public sealed class SetDataBreakpointsCommandArgs : JsonValue
    {
        public sealed class DataBreakpoint
        {
            public DataBreakpoint(string dataId, string accessTypes, string condition, string hitCondition)
            {
                this.dataId = dataId;
                this.accessTypes = accessTypes;
                this.condition = condition;
                this.hitCondition = hitCondition;
            }

            public string dataId;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string accessTypes;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string condition;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string hitCondition;
        }
        public DataBreakpoint[] breakpoints;
    }

    #endregion

    #region SourceBreakpoints

    /// <summary>
    /// Contains information on all the breakpoints for a source file
    /// </summary>
    public sealed class DataBreakpoints
    {
        private List<SetDataBreakpointsCommandArgs.DataBreakpoint> breakpoints;

        public DataBreakpoints()
        {
            this.breakpoints = new List<SetDataBreakpointsCommandArgs.DataBreakpoint>();
        }

        public DataBreakpoints(params string[] dataIds)
    : this()
        {
            foreach (string dataId in dataIds)
            {
                this.Add(dataId);
            }
        }

        public DataBreakpoints Add(string dataId, string accessType = "write", string condition = null, string hitCondition = null)
        {
            this.breakpoints.Add(new SetDataBreakpointsCommandArgs.DataBreakpoint(dataId, accessType, condition, null));
            return this;
        }

        public DataBreakpoints Remove(string dataId)
        {
            this.breakpoints.RemoveAll(bp => String.Equals(bp.dataId, dataId, StringComparison.Ordinal));
            return this;
        }

        internal IList<SetDataBreakpointsCommandArgs.DataBreakpoint> Breakpoints
        {
            get { return this.breakpoints; }
        }
    }

    #endregion

    public class SetDataBreakpointsCommand : CommandWithResponse<SetDataBreakpointsCommandArgs, SetDataBreakpointsResponseValue>
    {
        public SetDataBreakpointsCommand() : base("setDataBreakpoints")
        {
        }

        public SetDataBreakpointsCommand(DataBreakpoints dataBreakpoints) :
            this()
        {
            this.Args.breakpoints = dataBreakpoints.Breakpoints.ToArray();
        }

        public override string ToString()
        {
            return "{0} ({1})".FormatInvariantWithArgs(base.ToString(), String.Join(", ", this.Args.breakpoints.Select(bp => bp.dataId)));
        }
    }
}
