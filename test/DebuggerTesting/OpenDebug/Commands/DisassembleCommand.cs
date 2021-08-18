// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting.OpenDebug.Commands.Responses;
using Newtonsoft.Json;
using System;

namespace DebuggerTesting.OpenDebug.Commands
{
    public sealed class DisassembleArgs : JsonValue
    {
        public string memoryReference;

        public int? offset;

        public int? instructionOffset;

        public int instructionCount;

        public bool? resolveSymbols;
    }

    public class DisassembleCommand : CommandWithResponse<DisassembleArgs, DisassembleResponseValue>
    {
        public DisassembleCommand(string memoryReference, int? offset, int? instructionOffset, int instructionCount, bool? resolveSymbols) : base("disassemble")
        {
            this.Args.memoryReference = memoryReference;
            this.Args.offset = offset;
            this.Args.instructionOffset = instructionOffset;
            this.Args.instructionCount = instructionCount;
            this.Args.resolveSymbols = resolveSymbols;
        }
    }
}
