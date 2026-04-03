// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.SSHDebugPS.Docker
{
    /// <summary>
    /// Abstraction for container runtimes (Docker, Podman, etc.) used by the container picker UI.
    /// </summary>
    internal interface IContainerRuntime
    {
        /// <summary>
        /// Gets the container helper instance for this runtime.
        /// </summary>
        ContainerHelper Helper { get; }

        /// <summary>
        /// Creates a connection string from the given container, remote connection, and hostname.
        /// </summary>
        string CreateConnectionString(string containerName, string remoteConnectionName, string hostName);
    }

    internal class DockerContainerRuntime : IContainerRuntime
    {
        public static readonly DockerContainerRuntime Instance = new DockerContainerRuntime();

        private readonly DockerHelper _helper = new DockerHelper();

        public ContainerHelper Helper => _helper;

        public string CreateConnectionString(string containerName, string remoteConnectionName, string hostName)
        {
            return DockerConnection.CreateConnectionString(containerName, remoteConnectionName, hostName);
        }
    }
}
