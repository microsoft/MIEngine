// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.SSHDebugPS.Podman
{
    // Handles JSON values that may be a string, array of strings, or array of objects (port mappings).
    internal sealed class PodmanJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(string);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            switch (token.Type)
            {
                case JTokenType.String:
                    return token.Value<string>();
                case JTokenType.Array:
                    return string.Join(", ", token.Select(t =>
                    {
                        if (t.Type == JTokenType.String)
                            return t.Value<string>();
                        if (t.Type == JTokenType.Object)
                        {
                            // Handle Podman port mapping objects: {"host_ip":"0.0.0.0","container_port":80,"host_port":8080,"range":1,"protocol":"tcp"}
                            var hostIp = t.Value<string>("host_ip") ?? "0.0.0.0";
                            var hostPort = t.Value<int?>("host_port");
                            var containerPort = t.Value<int?>("container_port");
                            var protocol = t.Value<string>("protocol") ?? "tcp";
                            if (hostPort.HasValue && containerPort.HasValue)
                                return $"{hostIp}:{hostPort}->{containerPort}/{protocol}";
                        }
                        return t.ToString();
                    }));
                case JTokenType.Null:
                    return string.Empty;
                default:
                    return token.ToString();
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }
    }
}
