// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting.OpenDebug.Commands.Responses;

namespace DebuggerTesting.OpenDebug.Commands
{
    public sealed class SetVariableCommandArgs
    {
        public string name;
        public string value;
        public int variablesReference;
    }

    public class SetVariableCommand : CommandWithResponse<SetVariableCommandArgs, SetVariableResponseValue>
    {
        public SetVariableCommand(int variablesReference, string name, string value) : base("setVariable")
        {
            Parameter.ThrowIfNullOrWhiteSpace(name, nameof(name));
            Parameter.ThrowIfNullOrWhiteSpace(value, nameof(value));
            this.Args.variablesReference = variablesReference;
            this.Args.name = name;
            this.Args.value = value;
        }

        public override string ToString()
        {
            return "{0} ({1}={2})".FormatInvariantWithArgs(base.ToString(), this.Args.name, this.Args.value);
        }
    }
}
