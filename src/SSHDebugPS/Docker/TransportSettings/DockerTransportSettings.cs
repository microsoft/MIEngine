// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SSHDebugPS.Utilities;

namespace Microsoft.SSHDebugPS.Docker
{
    internal abstract class DockerTransportSettingsBase : IPipeTransportSettings
    {
        protected abstract string SubCommand { get; }
        protected abstract string SubCommandArgs { get; }

        internal string HostName { get; private set; }

        internal bool HostIsUnix { get; private set; }

        public DockerTransportSettingsBase(string hostname, bool hostIsUnix)
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

        public DockerTransportSettingsBase(DockerTransportSettingsBase settings)
            : this(settings.HostName, settings.HostIsUnix)
        { }

        private static string WindowsExe => "docker.exe";
        private static string UnixExe => "docker";

        // 0 = docker command parameters
        // 1 = docker subcommand
        // 2 = docker subcommand parameters
        private const string _baseCommandFormat = "{0} {1} {2}";
        // 0 = hostname property
        private const string _hostnameFormat = "--host \"{0}\"";
        private string GenerateExeCommandArgs()
        {
            var hostnameArg = string.Empty;
            if (!string.IsNullOrWhiteSpace(this.HostName))
                hostnameArg = _hostnameFormat.FormatInvariantWithArgs(this.HostName);

            return _baseCommandFormat.FormatInvariantWithArgs(hostnameArg, SubCommand, SubCommandArgs);
        }

        #region IPipeTransportSettings

        public string CommandArgs => GenerateExeCommandArgs();

        public string Command => HostIsUnix ? UnixExe : WindowsExe;
        #endregion
    }

    internal class DockerCommandSettings : DockerTransportSettingsBase
    {
        private string _cmd;
        private string _args;

        public DockerCommandSettings(string hostname, bool hostIsUnix)
            : base(hostname, hostIsUnix)
        { }

        public void SetCommand(string cmd, string args)
        {
            _cmd = cmd;
            _args = args;
        }

        protected override string SubCommand => _cmd;
        protected override string SubCommandArgs => _args;
    }

    internal class DockerExecSettings : DockerCommandSettings
    {
        public DockerExecSettings(DockerCommandSettings settings, string command, string commandArgs)
            : base(settings.HostName, settings.HostIsUnix)
        {
            this.SetCommand(command, commandArgs);
        }
    }

}

