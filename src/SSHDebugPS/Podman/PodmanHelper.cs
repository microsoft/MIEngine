// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.SSHDebugPS.Docker;

namespace Microsoft.SSHDebugPS.Podman
{
    /// <summary>
    /// Podman-specific container helper. Uses podman CLI for container discovery and management.
    /// </summary>
    internal class PodmanHelper : ContainerHelper
    {
        private static readonly PodmanHelper _instance = new PodmanHelper();
        private static ContainerHelper Base => _instance;

        protected override ContainerCommandSettingsBase CreateCommandSettings(string hostname, bool hostIsUnix)
        {
            return new PodmanCommandSettings(hostname, hostIsUnix);
        }

        protected override bool TryCreateContainerInstance(string json, out DockerContainerInstance instance)
        {
            return PodmanContainerInstance.TryCreate(json, out instance);
        }

        // Static convenience methods
        internal static bool TryGetLCOW(string hostname, out bool lcow) => Base.TryGetLCOW(hostname, out lcow);
        internal static bool TryGetServerOS(string hostname, out string serverOS) => Base.TryGetServerOS(hostname, out serverOS);
        internal static bool TryGetContainerPlatform(string hostname, string containerName, out string containerPlatform)
            => Base.TryGetContainerPlatform(hostname, containerName, out containerPlatform);
        internal static IEnumerable<DockerContainerInstance> GetLocalPodmanContainers(string hostname, out int totalContainers)
            => Base.GetLocalContainers(hostname, out totalContainers);
        internal static bool IsContainerRunning(string hostName, string containerName, Connection remoteConnection)
            => Base.IsContainerRunning(hostName, containerName, remoteConnection);
        internal static IEnumerable<DockerContainerInstance> GetRemotePodmanContainers(IConnection connection, string hostname, out int totalContainers)
            => Base.GetRemoteContainers(connection, hostname, out totalContainers);
    }
}
