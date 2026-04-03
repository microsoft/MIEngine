// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SSHDebugPS.Utilities;

namespace Microsoft.SSHDebugPS.Docker
{
    internal abstract class ContainerTransportSettingsBase : IPipeTransportSettings
    {
        protected abstract string SubCommand { get; }
        protected abstract string SubCommandArgs { get; }

        protected virtual string WindowsExe => "docker.exe";
        protected virtual string UnixExe => "docker";

        internal string HostName { get; private set; }
        internal bool HostIsUnix { get; private set; }

        public ContainerTransportSettingsBase(string hostname, bool hostIsUnix)
        {
            HostIsUnix = hostIsUnix;
            if (!string.IsNullOrWhiteSpace(hostname))
            {
                HostName = hostname;
            }
            else
            {
                HostName = string.Empty;
            }
        }

        public ContainerTransportSettingsBase(ContainerTransportSettingsBase settings)
            : this(settings.HostName, settings.HostIsUnix)
        { }

        // 0 = command parameters (e.g. --host)
        // 1 = subcommand (e.g. exec, cp, ps)
        // 2 = subcommand parameters
        private const string _baseCommandFormat = "{0} {1} {2}";
        // 0 = hostname property
        protected virtual string HostnameArgFormat => "--host \"{0}\"";
        private string GenerateExeCommandArgs()
        {
            var hostnameArg = string.Empty;
            if (!string.IsNullOrWhiteSpace(this.HostName))
                hostnameArg = HostnameArgFormat.FormatInvariantWithArgs(this.HostName);

            return _baseCommandFormat.FormatInvariantWithArgs(hostnameArg, SubCommand, SubCommandArgs);
        }

        #region IPipeTransportSettings

        public string CommandArgs => GenerateExeCommandArgs();

        public string Command => HostIsUnix ? UnixExe : WindowsExe;
        #endregion
    }

    /// <summary>
    /// A command settings class that allows setting the subcommand and args dynamically.
    /// Used by <see cref="ContainerHelper"/> for container discovery commands (ps, info, version, inspect).
    /// </summary>
    internal abstract class ContainerCommandSettingsBase : ContainerTransportSettingsBase
    {
        private string _cmd;
        private string _args;

        public ContainerCommandSettingsBase(string hostname, bool hostIsUnix)
            : base(hostname, hostIsUnix) { }

        public void SetCommand(string cmd, string args)
        {
            _cmd = cmd;
            _args = args;
        }

        protected override string SubCommand => _cmd;
        protected override string SubCommandArgs => _args;
    }

    // Preserves backward compatibility as a type alias
    internal abstract class DockerTransportSettingsBase : ContainerTransportSettingsBase
    {
        public DockerTransportSettingsBase(string hostname, bool hostIsUnix)
            : base(hostname, hostIsUnix) { }

        public DockerTransportSettingsBase(ContainerTransportSettingsBase settings)
            : base(settings) { }
    }

    internal class DockerCommandSettings : ContainerCommandSettingsBase
    {
        public DockerCommandSettings(string hostname, bool hostIsUnix)
            : base(hostname, hostIsUnix)
        { }
    }
}

