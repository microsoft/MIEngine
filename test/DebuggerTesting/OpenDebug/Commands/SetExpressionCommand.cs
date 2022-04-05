// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting.OpenDebug.Commands.Responses;

namespace DebuggerTesting.OpenDebug.Commands
{
    public sealed class ValueFormat
    {
        public bool? hex;
    }
    public sealed class SetExpressionCommandArgs
    {
        public string expression;
        public string value;
        public int? frameId;
        public ValueFormat format;
    }

    public class SetExpressionCommand : CommandWithResponse<SetExpressionCommandArgs, SetExpressionResponseValue>
    {
        public SetExpressionCommand(string expression, string value, int frameId, ValueFormat format = null) : base("setExpression")
        {
            Parameter.ThrowIfNullOrWhiteSpace(expression, nameof(expression));
            Parameter.ThrowIfNullOrWhiteSpace(value, nameof(value));
            this.Args.expression = expression;
            this.Args.value = value;
            this.Args.frameId = frameId;
            this.Args.format = format;
        }

        public override string ToString()
        {
            return "{0} ({1}={2})".FormatInvariantWithArgs(base.ToString(), this.Args.expression, this.Args.value);
        }
    }
}
