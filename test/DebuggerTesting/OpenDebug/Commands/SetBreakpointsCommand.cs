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

    public sealed class SetBreakpointsCommandArgs : JsonValue
    {
        public sealed class SourceBreakpoint
        {
            public SourceBreakpoint(int line, int? column, string condition)
            {
                this.line = line;
                this.condition = condition;
            }

            public int line;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int? column;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string condition;
        }

        public Source source = new Source();
        public int[] lines;
        public SourceBreakpoint[] breakpoints;
    }

    #endregion

    #region SourceBreakpoints

    /// <summary>
    /// Contains information on all the breakpoints for a source file
    /// </summary>
    public sealed class SourceBreakpoints
    {
        #region Constructor/Create

        public SourceBreakpoints(IDebuggee debuggee, string relativePath)
            : this(debuggee?.SourceRoot, relativePath)
        {
        }

        public SourceBreakpoints(string sourceRoot, string relativePath)
        {
            Parameter.ThrowIfNull(sourceRoot, nameof(sourceRoot));
            Parameter.ThrowIfNull(relativePath, nameof(relativePath));
            this.Breakpoints = new Dictionary<int, string>();
            this.RelativePath = relativePath;
            this.FullPath = Path.Combine(sourceRoot, relativePath);
        }

        #endregion

        #region Add/Remove

        public SourceBreakpoints Add(int lineNumber, string condition = null)
        {
            if (this.Breakpoints.ContainsKey(lineNumber))
                throw new RunnerException("Breakpoint line {0} already added to file {1}.", lineNumber, this.RelativePath);
            this.Breakpoints.Add(lineNumber, condition);
            return this;
        }

        public SourceBreakpoints Remove(int lineNumber)
        {
            if (!this.Breakpoints.ContainsKey(lineNumber))
                throw new RunnerException("Breakpoint line {0} does not exist in file {1}.", lineNumber, this.RelativePath);
            this.Breakpoints.Remove(lineNumber);
            return this;
        }

        #endregion

        #region Source File Info

        public string FullPath { get; private set; }

        public string RelativePath { get; private set; }

        #endregion

        /// <summary>
        /// Keep the breakpoint info in a dictionary indexed by line number.
        /// Store the condition as the value.
        /// </summary>
        public IDictionary<int, string> Breakpoints { get; private set; }
    }

    #endregion

    public class SetBreakpointsCommand : CommandWithResponse<SetBreakpointsCommandArgs, SetBreakpointsResponseValue>
    {
        public SetBreakpointsCommand() : base("setBreakpoints")
        {
        }

        public SetBreakpointsCommand(SourceBreakpoints sourceBreakpoints) :
            this()
        {
            this.Args.source.path = sourceBreakpoints.FullPath;
            IDictionary<int, string> breakpoints = sourceBreakpoints.Breakpoints;
            this.Args.breakpoints = breakpoints.Select(x =>
                    new SetBreakpointsCommandArgs.SourceBreakpoint(x.Key, null, x.Value)
                ).ToArray();
            this.Args.lines = this.Args.breakpoints.Select(x => x.line).ToArray();
        }

        public override string ToString()
        {
            return "{0} ({1}:{2})".FormatInvariantWithArgs(base.ToString(), this.Args.source.path,
                this.Args.lines.Count() == 0 ?
                    "(none)" :
                    "[{0}]".FormatInvariantWithArgs(String.Join(", ", this.Args.lines)));
        }
    }
}
