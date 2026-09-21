// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.UI;

namespace Microsoft.SSHDebugPS.Podman
{
    internal sealed class PodmanDiscoveryStrategy : IContainerDiscoveryStrategy
    {
        public string ConnectionLabel => UIResources.Podman_ConnectionLabel;
        public string HostnameLabel => UIResources.Podman_HostnameLabel;
        public string HostnameTip => UIResources.Podman_HostnameTip;
        public string ConnectionToolTip => UIResources.Podman_ConnectionToolTip;
        public string HostnameAutomationName => UIResources.Podman_HostnameAutomationName;

        public IEnumerable<ContainerInstance> GetLocalContainers(string hostname, out int totalContainers)
        {
            return PodmanHelper.GetLocalPodmanContainers(hostname, out totalContainers);
        }

        public IEnumerable<ContainerInstance> GetRemoteContainers(IConnection connection, string hostname, out int totalContainers)
        {
            return PodmanHelper.GetRemotePodmanContainers(connection, hostname, out totalContainers);
        }

        public void AssignPlatforms(IEnumerable<ContainerInstance> containers, string hostname)
        {
            // Podman only supports Linux containers
            foreach (ContainerInstance container in containers)
            {
                container.Platform = "Linux";
            }
        }
    }
}
