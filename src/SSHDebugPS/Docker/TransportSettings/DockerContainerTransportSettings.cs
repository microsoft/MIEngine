// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SSHDebugPS.Utilities;
using System.Diagnostics;

namespace Microsoft.SSHDebugPS.Docker
{
    internal class DockerContainerTransportSettings : DockerTransportSettingsBase
    {
        internal string ContainerName { get; private set; }

        public DockerContainerTransportSettings(string hostname, string containerName, bool hostIsUnix)
            : base(hostname, hostIsUnix)
        {
            ContainerName = containerName;
        }

        public DockerContainerTransportSettings(DockerContainerTransportSettings settings)
            : base(settings)
        {
            ContainerName = settings.ContainerName;
        }

        protected override string SubCommand => throw new System.NotImplementedException();
        protected override string SubCommandArgs => throw new System.NotImplementedException();
    }

    internal class DockerContainerExecSettings : DockerContainerTransportSettings
    {
        private bool _runInShell;
        private string _commandToExecute;
        // 0 = container, 1 = command to execute
        private const string _subCommandArgsFormat = "{0} {1}";
        private const string _subCommandArgsFormatWithShell = "{0} /bin/sh -c '{1}'"; // Single quote the argument so variable resolution does not happen until it is in the container.
        private const string _interactiveFlag = "-i ";

        private bool _makeInteractive;

        public DockerContainerExecSettings(DockerContainerTransportSettings settings, string command, bool runInShell, bool makeInteractive = true)
            : base(settings)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(command), "Exec command cannot be null");
            _runInShell = runInShell;
            _commandToExecute = command;
            _makeInteractive = makeInteractive;
        }

        protected override string SubCommand => "exec";
        protected override string SubCommandArgs => (_makeInteractive ? _interactiveFlag : string.Empty) + (_runInShell ? _subCommandArgsFormatWithShell : _subCommandArgsFormat).FormatInvariantWithArgs(ContainerName, _commandToExecute);
    }

    internal class DockerCopySettings : DockerContainerTransportSettings
    {
        // {0} = container, {1} = source, {2} = destination
        private string _copyFormatToContainer = "{1} {0}:{2}";

        private string _sourcePath;
        private string _destinationPath;

        /// <summary>
        /// Settings to copy from host to the docker container
        /// </summary>
        /// <param name="sourcePath">Local path on host</param>
        /// <param name="destinationPath">Remote path within the docker container</param>
        /// <param name="containerName">Name of container</param>
        /// <param name="hostIsUnix">Host is Unix</param>
        public DockerCopySettings(string hostname, string sourcePath, string destinationPath, string containerName, bool hostIsUnix)
            : base(hostname, containerName, hostIsUnix)
        {
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
        }

        public DockerCopySettings(DockerContainerTransportSettings settings, string sourcePath, string destinationPath)
            : base(settings)
        {
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
        }

        protected override string SubCommand => "cp";
        protected override string SubCommandArgs => _copyFormatToContainer.FormatInvariantWithArgs(ContainerName, _sourcePath, _destinationPath);
    }
}
