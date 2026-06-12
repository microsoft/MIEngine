// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.UI;

namespace Microsoft.SSHDebugPS
{
    internal sealed class DockerDiscoveryStrategy : IContainerDiscoveryStrategy
    {
        private const string unknownOS = "Unknown";

        public string ConnectionLabel => UIResources.ConnectionLabel;
        public string HostnameLabel => UIResources.HostnameLabel;
        public string HostnameTip => UIResources.HostnameTip;
        public string ConnectionToolTip => UIResources.ConnectionToolTip;
        public string HostnameAutomationName => UIResources.HostnameAutomationName;

        public IEnumerable<DockerContainerInstance> GetLocalContainers(string hostname, out int totalContainers)
        {
            return DockerHelper.GetLocalDockerContainers(hostname, out totalContainers);
        }

        public IEnumerable<DockerContainerInstance> GetRemoteContainers(IConnection connection, string hostname, out int totalContainers)
        {
            return DockerHelper.GetRemoteDockerContainers(connection, hostname, out totalContainers);
        }

        public void AssignPlatforms(IEnumerable<DockerContainerInstance> containers, string hostname)
        {
            if (!containers.Any())
                return;

            string serverOS;
            if (DockerHelper.TryGetServerOS(hostname, out serverOS))
            {
                bool lcow;
                DockerHelper.TryGetLCOW(hostname, out lcow);
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

                if (lcow && serverOS.IndexOf("windows", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    foreach (DockerContainerInstance container in containers)
                    {
                        string containerPlatform = string.Empty;
                        if (DockerHelper.TryGetContainerPlatform(hostname, container.Name, out containerPlatform))
                        {
                            container.Platform = textInfo.ToTitleCase(containerPlatform);
                        }
                        else
                        {
                            container.Platform = unknownOS;
                        }
                    }
                }
                else
                {
                    string platform = textInfo.ToTitleCase(serverOS);
                    foreach (DockerContainerInstance container in containers)
                    {
                        container.Platform = platform;
                    }
                }
            }
            else
            {
                foreach (DockerContainerInstance container in containers)
                {
                    container.Platform = unknownOS;
                }
            }
        }
    }
}
