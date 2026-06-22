// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.SSHDebugPS.Docker;

namespace Microsoft.SSHDebugPS
{
    internal interface IContainerDiscoveryStrategy
    {
        string ConnectionLabel { get; }
        string HostnameLabel { get; }
        string HostnameTip { get; }
        string ConnectionToolTip { get; }
        string HostnameAutomationName { get; }

        IEnumerable<ContainerInstance> GetLocalContainers(string hostname, out int totalContainers);
        IEnumerable<ContainerInstance> GetRemoteContainers(IConnection connection, string hostname, out int totalContainers);
        void AssignPlatforms(IEnumerable<ContainerInstance> containers, string hostname);
    }
}
