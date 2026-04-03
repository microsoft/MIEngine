// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.Utilities;
using System.Diagnostics;

namespace Microsoft.SSHDebugPS.Podman
{
    internal class PodmanContainerTransportSettings : PodmanTransportSettingsBase
    {
        internal string ContainerName { get; private set; }

        public PodmanContainerTransportSettings(string hostname, string containerName, bool hostIsUnix)
            : base(hostname, hostIsUnix)
        {
            ContainerName = containerName;
        }

        public PodmanContainerTransportSettings(PodmanContainerTransportSettings settings)
            : base(settings)
        {
            ContainerName = settings.ContainerName;
        }

        protected override string SubCommand => throw new System.NotImplementedException();
        protected override string SubCommandArgs => throw new System.NotImplementedException();
    }

    internal class PodmanExecSettings : PodmanContainerTransportSettings
    {
        private bool _runInShell;
        private string _commandToExecute;
        private const string _subCommandArgsFormat = "{0} {1}";
        private const string _subCommandArgsFormatWithShell = "{0} /bin/sh -c \"{1}\"";
        private const string _subCommandArgsFormatWithShellLinuxHost = "{0} /bin/sh -c '{1}'";
        private const string _interactiveFlag = "-i ";

        private bool _makeInteractive;

        public PodmanExecSettings(PodmanContainerTransportSettings settings, string command, bool runInShell, bool makeInteractive = true)
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
                string command = this.HostIsUnix ? _commandToExecute.Replace("'", "'\\''") : _commandToExecute;
                return (_makeInteractive ? _interactiveFlag : string.Empty) +
                    (_runInShell ? subCommandFormat : _subCommandArgsFormat).FormatInvariantWithArgs(ContainerName, command);
            }
        }
    }

    internal class PodmanCopySettings : PodmanContainerTransportSettings
    {
        private string _copyFormatToContainer = "{1} {0}:{2}";

        private string _sourcePath;
        private string _destinationPath;

        public PodmanCopySettings(string hostname, string sourcePath, string destinationPath, string containerName, bool hostIsUnix)
            : base(hostname, containerName, hostIsUnix)
        {
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
        }

        public PodmanCopySettings(PodmanContainerTransportSettings settings, string sourcePath, string destinationPath)
            : base(settings)
        {
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
        }

        protected override string SubCommand => "cp";
        protected override string SubCommandArgs => _copyFormatToContainer.FormatInvariantWithArgs(ContainerName, _sourcePath, _destinationPath);
    }
}
