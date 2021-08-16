// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands.Responses
{
    public sealed class CompletionItem
    {
        public string label;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string text;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string sortText;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string type;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? start;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? length;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? selectionStart;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? selectionLength;
    }

    public sealed class CompletionsResponseValue : CommandResponseValue
    {
        public sealed class Body
        {
            public CompletionItem[] targets;
        }

        public Body body = new Body();
    }
}