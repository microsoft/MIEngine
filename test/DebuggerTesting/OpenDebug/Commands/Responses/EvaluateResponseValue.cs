// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands.Responses
{
    public sealed class EvaluateResponseValue : CommandResponseValue
    {
        public sealed class Body
        {
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string result;
        }

        public Body body = new Body();
    }
}