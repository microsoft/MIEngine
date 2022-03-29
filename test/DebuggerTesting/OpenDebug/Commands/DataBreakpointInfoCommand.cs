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

    #region DataBreakpointsInfoCommandArgs

    public sealed class DataBreakpointsInfoCommandArgs : JsonValue
    {
        public int? variableReference;
        public string name;
    }

    #endregion

    public class DataBreakpointsInfoCommand : CommandWithResponse<DataBreakpointsInfoCommandArgs, DataBreakpointsInfoResponseValue>
    {
        public DataBreakpointsInfoCommand() : base("dataBreakpointInfo")
        {
        }

        public DataBreakpointsInfoCommand(string name) :
            this()
        {
            this.Args.name = name;
        }

        public override string ToString()
        {
            return "{0} ({1})".FormatInvariantWithArgs(base.ToString(), this.Args.name);
        }
    }
}
