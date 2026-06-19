// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SSHDebugPS.Podman
{
    internal sealed class PodmanContainerTransportSettings : ContainerTargetTransportSettings
    {
        internal const string WindowsExeName = "podman.exe";
        internal const string UnixExeName = "podman";
        internal const string HostFlag = "--url \"{0}\"";

        public PodmanContainerTransportSettings(string hostname, string containerName, bool hostIsUnix)
            : base(hostname, containerName, hostIsUnix, WindowsExeName, UnixExeName, HostFlag)
        { }

        public PodmanContainerTransportSettings(PodmanContainerTransportSettings settings)
            : base(settings)
        { }
    }

    internal sealed class PodmanExecSettings : ContainerExecSettings
    {
        public PodmanExecSettings(PodmanContainerTransportSettings settings, string command, bool runInShell, bool makeInteractive = true)
            : base(settings, command, runInShell, makeInteractive)
        { }
    }

    internal sealed class PodmanCopySettings : ContainerCopySettings
    {
        public PodmanCopySettings(string hostname, string sourcePath, string destinationPath, string containerName, bool hostIsUnix)
            : base(hostname, sourcePath, destinationPath, containerName, hostIsUnix, PodmanContainerTransportSettings.WindowsExeName, PodmanContainerTransportSettings.UnixExeName, PodmanContainerTransportSettings.HostFlag)
        { }

        public PodmanCopySettings(PodmanContainerTransportSettings settings, string sourcePath, string destinationPath)
            : base(settings, sourcePath, destinationPath)
        { }
    }

    internal sealed class PodmanCommandSettings : ContainerCommandSettings
    {
        public PodmanCommandSettings(string hostname, bool hostIsUnix)
            : base(hostname, hostIsUnix, PodmanContainerTransportSettings.WindowsExeName, PodmanContainerTransportSettings.UnixExeName, PodmanContainerTransportSettings.HostFlag)
        { }
    }
}

