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
    /// <summary>
    /// Parses Podman's JSON output into DockerContainerInstance objects.
    /// Podman uses different field names/types than Docker (e.g., "Id" vs "ID",
    /// arrays vs strings for Names/Ports/Command, "State" vs "Status").
    /// </summary>
    internal static class PodmanContainerInstance
    {
        public static bool TryCreate(string json, out DockerContainerInstance instance)
        {
            instance = null;
            try
            {
                JObject obj = JObject.Parse(json);

                // Map Podman's JSON fields to DockerContainerInstance-compatible fields
                var mapped = new JObject();

                // ID: Podman uses "Id" (PascalCase), Docker uses "ID" (uppercase)
                mapped["ID"] = obj["Id"] ?? obj["ID"] ?? "";

                // Names: Podman returns an array, Docker returns a string
                var names = obj["Names"];
                if (names is JArray namesArray)
                {
                    mapped["Names"] = namesArray.FirstOrDefault()?.ToString() ?? "";
                }
                else
                {
                    mapped["Names"] = names?.ToString() ?? "";
                }

                // Image: same in both
                mapped["Image"] = obj["Image"]?.ToString() ?? "";

                // Ports: Podman returns an array of port objects, Docker returns a formatted string
                var ports = obj["Ports"];
                if (ports is JArray portsArray && portsArray.Count > 0)
                {
                    var portStrings = new List<string>();
                    foreach (var port in portsArray)
                    {
                        string hostIp = port["host_ip"]?.ToString();
                        int containerPort = port["container_port"]?.Value<int>() ?? 0;
                        int hostPort = port["host_port"]?.Value<int>() ?? 0;
                        string protocol = port["protocol"]?.ToString() ?? "tcp";

                        if (hostPort > 0)
                        {
                            string binding = string.IsNullOrEmpty(hostIp) ? "0.0.0.0" : hostIp;
                            portStrings.Add($"{binding}:{hostPort}->{containerPort}/{protocol}");
                        }
                        else
                        {
                            portStrings.Add($"{containerPort}/{protocol}");
                        }
                    }
                    mapped["Ports"] = string.Join(", ", portStrings);
                }
                else if (ports is JValue)
                {
                    mapped["Ports"] = ports.ToString();
                }
                else
                {
                    mapped["Ports"] = "";
                }

                // Command: Podman returns an array, Docker returns a string
                var command = obj["Command"];
                if (command is JArray commandArray)
                {
                    mapped["Command"] = string.Join(" ", commandArray.Select(c => c.ToString()));
                }
                else
                {
                    mapped["Command"] = command?.ToString() ?? "";
                }

                // Status: Podman often returns empty Status, use State instead
                string status = obj["Status"]?.ToString();
                if (string.IsNullOrEmpty(status))
                {
                    status = obj["State"]?.ToString() ?? "";
                }
                mapped["Status"] = status;

                // CreatedAt: Podman may have empty CreatedAt, use Created (ISO timestamp) instead
                string createdAt = obj["CreatedAt"]?.ToString();
                if (string.IsNullOrEmpty(createdAt))
                {
                    createdAt = obj["Created"]?.ToString() ?? "";
                }
                mapped["CreatedAt"] = createdAt;

                instance = mapped.ToObject<DockerContainerInstance>();
            }
            catch (Exception e)
            {
                string error = e.ToString();
                VsOutputWindowWrapper.WriteLine(StringResources.Error_DockerPSParseFailed.FormatCurrentCultureWithArgs(json, error), StringResources.Podman_PSName);
                Debug.WriteLine($"PodmanContainerInstance parse failure: {error}");
            }
            return instance != null;
        }
    }
}
