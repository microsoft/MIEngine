// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SSHDebugPS.Docker
{
    internal sealed class DockerCommandSettings : ContainerCommandSettings
    {
        public DockerCommandSettings(string hostname, bool hostIsUnix)
            : base(hostname, hostIsUnix, DockerContainerTransportSettings.WindowsExeName, DockerContainerTransportSettings.UnixExeName, DockerContainerTransportSettings.HostFlag)
        { }
    }
}
