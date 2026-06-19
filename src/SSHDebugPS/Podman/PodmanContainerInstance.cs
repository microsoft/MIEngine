// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.DebugEngineHost;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.SSHDebugPS.Podman
{
    public class PodmanContainerInstance : ContainerInstance
    {
        public static bool TryCreate(string json, out PodmanContainerInstance instance)
        {
            instance = null;
            try
            {
                JObject obj = JObject.Parse(json);
                instance = obj.ToObject<PodmanContainerInstance>();
            }
            catch (Exception e)
            {
                HostTelemetry.SendEvent(TelemetryHelper.Event_PodmanPSParseFailure, new KeyValuePair<string, object>[] {
                    new KeyValuePair<string, object>(TelemetryHelper.Property_ExceptionName, e.GetType().Name)
                });

                string error = e.ToString();
                VsOutputWindowWrapper.WriteLine(StringResources.Error_PodmanPSParseFailed.FormatCurrentCultureWithArgs(json, error), StringResources.Podman_PSName);
                Debug.Fail(error);
            }
            return instance != null;
        }

        [JsonProperty("Command")]
        [JsonConverter(typeof(PodmanJsonConverter))]
        public override string Command { get; protected set; }

        [JsonProperty("Ports")]
        [JsonConverter(typeof(PodmanJsonConverter))]
        public override string Ports { get; set; }

        [JsonProperty("Names")]
        [JsonConverter(typeof(PodmanJsonConverter))]
        public override string Name { get; set; }

        protected override bool EqualsInternal(ContainerInstance instance)
        {
            if (instance is PodmanContainerInstance other)
            {
                return String.Equals(Id, other.Id, StringComparison.Ordinal) ||
                    Id.StartsWith(other.Id, StringComparison.Ordinal) ||
                    other.Id.StartsWith(Id, StringComparison.Ordinal);
            }

            return false;
        }
    }
}
