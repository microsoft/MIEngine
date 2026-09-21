// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SSHDebugPS.Utilities;
using System.Diagnostics;

namespace Microsoft.SSHDebugPS
{
    internal abstract class ContainerTransportSettingsBase : IPipeTransportSettings
    {
        protected abstract string SubCommand { get; }
        protected abstract string SubCommandArgs { get; }

        private readonly string _windowsExe;
        private readonly string _unixExe;
        private readonly string _hostnameFormat;

        internal string HostName { get; private set; }
        internal bool HostIsUnix { get; private set; }

        public ContainerTransportSettingsBase(string hostname, bool hostIsUnix, string windowsExe, string unixExe, string hostnameFormat)
        {
            HostIsUnix = hostIsUnix;
            _windowsExe = windowsExe;
            _unixExe = unixExe;
            _hostnameFormat = hostnameFormat;
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
            : this(settings.HostName, settings.HostIsUnix, settings._windowsExe, settings._unixExe, settings._hostnameFormat)
        { }

        // 0 = command parameters (e.g. --host/--url)
        // 1 = subcommand (e.g. exec, cp, ps)
        // 2 = subcommand parameters
        private const string _baseCommandFormat = "{0} {1} {2}";

        private string GenerateExeCommandArgs()
        {
            var hostnameArg = string.Empty;
            if (!string.IsNullOrWhiteSpace(this.HostName))
                hostnameArg = _hostnameFormat.FormatInvariantWithArgs(this.HostName);

            return _baseCommandFormat.FormatInvariantWithArgs(hostnameArg, SubCommand, SubCommandArgs);
        }

        #region IPipeTransportSettings

        public string CommandArgs => GenerateExeCommandArgs();

        public string Command => HostIsUnix ? _unixExe : _windowsExe;

        #endregion
    }

    internal abstract class ContainerTargetTransportSettings : ContainerTransportSettingsBase
    {
        internal string ContainerName { get; private set; }

        public ContainerTargetTransportSettings(string hostname, string containerName, bool hostIsUnix, string windowsExe, string unixExe, string hostnameFormat)
            : base(hostname, hostIsUnix, windowsExe, unixExe, hostnameFormat)
        {
            ContainerName = containerName;
        }

        public ContainerTargetTransportSettings(ContainerTargetTransportSettings settings)
            : base(settings)
        {
            ContainerName = settings.ContainerName;
        }

        protected override string SubCommand => throw new System.NotImplementedException();
        protected override string SubCommandArgs => throw new System.NotImplementedException();
    }

    internal abstract class ContainerExecSettings : ContainerTargetTransportSettings
    {
        private bool _runInShell;
        private string _commandToExecute;
        // 0 = container, 1 = command to execute
        private const string _subCommandArgsFormat = "{0} {1}";
        private const string _subCommandArgsFormatWithShell = "{0} /bin/sh -c \"{1}\"";
        private const string _subCommandArgsFormatWithShellLinuxHost = "{0} /bin/sh -c '{1}'";
        private const string _interactiveFlag = "-i ";

        private bool _makeInteractive;

        public ContainerExecSettings(ContainerTargetTransportSettings settings, string command, bool runInShell, bool makeInteractive = true)
            : base(settings)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(command), "Exec command cannot be null");
            _runInShell = runInShell;
            _commandToExecute = command;
            _makeInteractive = makeInteractive;
        }

        protected override string SubCommand => "exec";
        protected override string SubCommandArgs
        {
            get
            {
                string subCommandFormat = this.HostIsUnix ? _subCommandArgsFormatWithShellLinuxHost : _subCommandArgsFormatWithShell;
                // Escape single quotes on Linux so variable resolution does not happen until it is in the container.
                string command = this.HostIsUnix ? _commandToExecute.Replace("'", "'\\''") : _commandToExecute;
                return (_makeInteractive ? _interactiveFlag : string.Empty) +
                    (_runInShell ? subCommandFormat : _subCommandArgsFormat).FormatInvariantWithArgs(ContainerName, command);
            }
        }
    }

    internal abstract class ContainerCopySettings : ContainerTargetTransportSettings
    {
        // {0} = container, {1} = source, {2} = destination
        private const string _copyFormatToContainer = "{1} {0}:{2}";

        private string _sourcePath;
        private string _destinationPath;

        public ContainerCopySettings(string hostname, string sourcePath, string destinationPath, string containerName, bool hostIsUnix, string windowsExe, string unixExe, string hostnameFormat)
            : base(hostname, containerName, hostIsUnix, windowsExe, unixExe, hostnameFormat)
        {
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
        }

        public ContainerCopySettings(ContainerTargetTransportSettings settings, string sourcePath, string destinationPath)
            : base(settings)
        {
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
        }

        protected override string SubCommand => "cp";
        protected override string SubCommandArgs => _copyFormatToContainer.FormatInvariantWithArgs(ContainerName, _sourcePath, _destinationPath);
    }

    internal abstract class ContainerCommandSettings : ContainerTransportSettingsBase
    {
        private string _cmd;
        private string _args;

        public ContainerCommandSettings(string hostname, bool hostIsUnix, string windowsExe, string unixExe, string hostnameFormat)
            : base(hostname, hostIsUnix, windowsExe, unixExe, hostnameFormat)
        { }

        public void SetCommand(string cmd, string args)
        {
            _cmd = cmd;
            _args = args;
        }

        protected override string SubCommand => _cmd;
        protected override string SubCommandArgs => _args;
    }
}
