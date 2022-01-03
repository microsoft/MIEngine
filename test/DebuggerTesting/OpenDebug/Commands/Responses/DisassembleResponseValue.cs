// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands.Responses
{
    public sealed class DisassembleResponseValue : CommandResponseValue
    {
        public sealed class Body
        {
            public sealed class DisassembledInstruction
            {
                public string address;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public string instructionBytes;

                public string instruction;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public string symbol;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public Source location;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? line;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? column;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? endLine;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? endColumn;
            }

            public DisassembledInstruction[] instructions;
        }

        public Body body = new Body();
    }
}
