// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands.Responses
{
    #region ScopesResponseValue

    public sealed class ScopesResponseValue : CommandResponseValue
    {
        public sealed class Body
        {
            public sealed class Scope
            {
                public Scope(string name, bool expensive)
                {
                    this.name = name;
                    this.expensive = expensive;
                }
                public bool expensive;
                public string name;

                [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
                public int? variablesReference;
            }

            public Scope[] scopes;
        }

        public Body body = new Body();
    }

    #endregion

    internal class ScopesResponse : CommandResponse<ScopesResponseValue>
    {
        public ScopesResponse(string commandName)
            : base(commandName)
        {
            this.ExpectedResponse.body.scopes = new ScopesResponseValue.Body.Scope[] {
                new ScopesResponseValue.Body.Scope("Locals", false)
            };
        }
    }
}
