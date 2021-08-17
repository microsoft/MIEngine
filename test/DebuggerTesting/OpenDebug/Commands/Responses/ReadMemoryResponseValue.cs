// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands.Responses
{
    public sealed class ReadMemoryResponseValue : CommandResponseValue
    {
        public sealed class Body
        {
            public string address;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public int unreadableBytes;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string data;
        }

        public Body body = new Body();
    }
}
