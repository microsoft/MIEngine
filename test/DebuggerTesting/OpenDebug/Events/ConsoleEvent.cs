// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Events
{

    #region ConsoleEventValue

    public sealed class ConsoleEventValue : EventValue
    {
        public sealed class Body
        {
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string output;

            public string category;
        }

        public Body body = new Body();
    }

    #endregion

    public class ConsoleEvent : Event<ConsoleEventValue>
    {
        public ConsoleEvent(string text) : base("console")
        {
            this.ExpectedResponse.body.category = "console";
            this.ExpectedResponse.body.output = text;
        }
    }
}
