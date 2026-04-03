// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SSHDebugPS.Docker;

namespace Microsoft.SSHDebugPS.Podman
{
    /// <summary>
    /// Podman-specific transport settings base. Overrides the executable name to use podman/podman.exe.
    /// </summary>
    internal abstract class PodmanTransportSettingsBase : ContainerTransportSettingsBase
    {
        protected override string WindowsExe => "podman.exe";
        protected override string UnixExe => "podman";

        // Podman uses --url instead of --host for remote connections
        protected override string HostnameArgFormat => "--url \"{0}\"";

        public PodmanTransportSettingsBase(string hostname, bool hostIsUnix)
            : base(hostname, hostIsUnix) { }

        public PodmanTransportSettingsBase(ContainerTransportSettingsBase settings)
            : base(settings) { }
    }

    internal class PodmanCommandSettings : ContainerCommandSettingsBase
    {
        protected override string WindowsExe => "podman.exe";
        protected override string UnixExe => "podman";
        protected override string HostnameArgFormat => "--url \"{0}\"";

        public PodmanCommandSettings(string hostname, bool hostIsUnix)
            : base(hostname, hostIsUnix)
        { }
    }
}
