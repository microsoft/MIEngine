// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting.OpenDebug.Commands.Responses;

namespace DebuggerTesting.OpenDebug.Commands
{
    public sealed class CompletionsArgs : JsonValue
    {
        public int? frameId;

        public string text;

        public int column;

        public int? line;
    }

    public class CompletionsCommand : CommandWithResponse<CompletionsArgs, CompletionsResponseValue>
    {
        public CompletionsCommand(int? frameId, string text, int column, int? line) : base("completions")
        {
            this.Args.frameId = frameId;
            this.Args.text = text;
            this.Args.column = column;
            this.Args.line = line;
        }
    }
}
