// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SSHDebugPS
{
    /// <summary>
    /// Identifies the container runtime to query when discovering containers.
    /// </summary>
    public enum ContainerRuntimeType
    {
        Unknown,
        Docker,
        Podman
    }
}
