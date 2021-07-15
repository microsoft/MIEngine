// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting.OpenDebug.Commands.Responses;

namespace DebuggerTesting.OpenDebug.Commands
{
    public sealed class VariablesCommandArgs : JsonValue
    {
        public int variablesReference;
    }

    public class VariablesCommand : CommandWithResponse<VariablesCommandArgs, VariablesResponseValue>
    {
        public VariablesCommand(int variablesRefernce)
            : base("variables")
        {
            this.Args.variablesReference = variablesRefernce;
        }
    }
}
