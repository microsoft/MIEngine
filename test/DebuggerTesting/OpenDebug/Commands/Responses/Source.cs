// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands.Responses
{
    /// <summary>
    /// This data is used in multiple command args and responses
    /// </summary>
    public sealed class Source
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string name;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string path;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? sourceReference;
    }
}
