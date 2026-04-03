// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SSHDebugPS.Docker;

namespace Microsoft.SSHDebugPS.Podman
{
    internal class PodmanContainerRuntime : IContainerRuntime
    {
        public static readonly PodmanContainerRuntime Instance = new PodmanContainerRuntime();

        private readonly PodmanHelper _helper = new PodmanHelper();

        public ContainerHelper Helper => _helper;

        public string CreateConnectionString(string containerName, string remoteConnectionName, string hostName)
        {
            return PodmanConnection.CreateConnectionString(containerName, remoteConnectionName, hostName);
        }
    }
}
