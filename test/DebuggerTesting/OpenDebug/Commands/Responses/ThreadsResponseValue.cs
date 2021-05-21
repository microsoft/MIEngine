// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands.Responses
{
    public sealed class ThreadsResponseValue : CommandResponseValue
    {
        public sealed class Body
        {
            public sealed class Thread
            {
                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? id;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public string name;
            }

            public Thread[] threads;
        }

        public Body body = new Body();
    }
}
