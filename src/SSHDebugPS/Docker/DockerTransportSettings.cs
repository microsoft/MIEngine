// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;

namespace Microsoft.SSHDebugPS.Docker
{
    internal abstract class DockerTransportSettings : IPipeTransportSettings
    {
        protected string ContainerName { get; private set; }
        protected bool IsUnix { get; private set; }

        public DockerTransportSettings(string containerName, bool isUnix)
        {
            ContainerName = containerName;
            IsUnix = isUnix;
        }

        private static string WindowsExe => "docker.exe";
        private static string UnixExe => "docker";

        #region IPipeTransportSettings
        public abstract string ExeCommandArgs { get; }

        public string ExeCommand => IsUnix ? UnixExe : WindowsExe;

        public string ExeNotFoundErrorMessage => string.Format(CultureInfo.InvariantCulture, "{0} not found.", IsUnix ? UnixExe : WindowsExe);
        #endregion
    }

    internal class DockerExecShellSettings : DockerExecSettings
    {
        public DockerExecShellSettings(string containerName, bool isUnix)
            : base(containerName, "/bin/sh", isUnix)
        { }

    }

    internal class DockerExecSettings : DockerTransportSettings
    {
        private const string _exeArgsFormat = "exec -i {0} {1}";
        private string _command;

        public DockerExecSettings(string containerName, string command, bool isUnix)
        : base(containerName, isUnix)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(command), "Exec command cannot be null");
            _command = command;
        }

        public override string ExeCommandArgs
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, _exeArgsFormat, ContainerName, _command);
            }
        }
    }

    internal class DockerCopySettings : DockerTransportSettings
    {
        // {0} = container, {1} = source, {2} = destination
        private string _copyFormatToContainer = "cp {1} {0}:{2}";

        private string _sourcePath;
        private string _destinationPath;

        public DockerCopySettings(string sourcePath, string destinationPath, string containerName, bool isUnix)
            : base(containerName, isUnix)
        {
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
        }

        public override string ExeCommandArgs
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, _copyFormatToContainer, ContainerName, _sourcePath, _destinationPath);
            }
        }
    }
}

