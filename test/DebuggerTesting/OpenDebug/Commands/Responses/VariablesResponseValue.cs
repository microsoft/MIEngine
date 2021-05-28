// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands.Responses
{
    public sealed class VariablesResponseValue : CommandResponseValue
    {
        public sealed class Body
        {
            public sealed class Variable
            {
                [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
                public string name;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public string value;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? variablesReference;
            }

            public Variable[] variables;
        }

        public Body body = new Body();
    }
}
