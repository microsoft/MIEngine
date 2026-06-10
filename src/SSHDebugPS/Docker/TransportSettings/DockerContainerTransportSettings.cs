// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SSHDebugPS.Docker
{
    internal class DockerContainerTransportSettings : ContainerTargetTransportSettings
    {
        internal const string WindowsExeName = "docker.exe";
        internal const string UnixExeName = "docker";
        internal const string HostFlag = "--host \"{0}\"";

        public DockerContainerTransportSettings(string hostname, string containerName, bool hostIsUnix)
            : base(hostname, containerName, hostIsUnix, WindowsExeName, UnixExeName, HostFlag)
        { }

        public DockerContainerTransportSettings(DockerContainerTransportSettings settings)
            : base(settings)
        { }
    }

    internal class DockerExecSettings : ContainerExecSettings
    {
        public DockerExecSettings(DockerContainerTransportSettings settings, string command, bool runInShell, bool makeInteractive = true)
            : base(settings, command, runInShell, makeInteractive)
        { }
    }

    internal class DockerCopySettings : ContainerCopySettings
    {
        public DockerCopySettings(string hostname, string sourcePath, string destinationPath, string containerName, bool hostIsUnix)
            : base(hostname, sourcePath, destinationPath, containerName, hostIsUnix, DockerContainerTransportSettings.WindowsExeName, DockerContainerTransportSettings.UnixExeName, DockerContainerTransportSettings.HostFlag)
        { }

        public DockerCopySettings(DockerContainerTransportSettings settings, string sourcePath, string destinationPath)
            : base(settings, sourcePath, destinationPath)
        { }
    }
}
