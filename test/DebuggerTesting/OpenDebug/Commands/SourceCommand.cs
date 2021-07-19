// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting.OpenDebug.Commands.Responses;

namespace DebuggerTesting.OpenDebug.Commands
{
    public sealed class SourceCommandArgs : JsonValue
    {
        public int sourceReference;
    }

    public class SourceCommand : CommandWithResponse<SourceCommandArgs, SourceResponseValue>
    {
        public SourceCommand(int sourceReference) : base("source")
        {
            this.Args.sourceReference = sourceReference;
        }
    }
}
