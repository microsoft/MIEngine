// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands.Responses
{
    public sealed class StackTraceResponseValue : CommandResponseValue
    {
        public sealed class Body
        {
            public sealed class StackFrame
            {
                public string name;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? id;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? line;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? column;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public Source source;
            }
            public StackFrame[] stackFrames;
        }

        public Body body = new Body();
    }
}
