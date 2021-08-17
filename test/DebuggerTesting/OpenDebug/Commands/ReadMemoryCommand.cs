// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting.OpenDebug.Commands.Responses;
using Newtonsoft.Json;
using System;

namespace DebuggerTesting.OpenDebug.Commands
{
    public sealed class ReadMemoryArgs : JsonValue
    {
        public string memoryReference;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? offset;

        public int count;
    }

    public class ReadMemoryCommand : CommandWithResponse<ReadMemoryArgs, ReadMemoryResponseValue>
    {
        public ReadMemoryCommand(string reference, int? offset, int count) : base("readMemory")
        {
            this.Args.memoryReference = reference;
            this.Args.offset = offset;
            this.Args.count = count;
        }
    }
}
