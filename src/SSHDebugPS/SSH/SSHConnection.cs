// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using liblinux;
using liblinux.Shell;
using Microsoft.SSHDebugPS.Utilities;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS.SSH
{
    internal class SSHConnection : Connection
    {
        private liblinux.UnixSystem _remoteSystem;
        private liblinux.Services.GdbServer _gdbserver = null;

        public SSHConnection(liblinux.UnixSystem remoteSystem)
        {
            if (remoteSystem == null)
                throw new ArgumentNullException(nameof(remoteSystem));

            _remoteSystem = remoteSystem;
        }

        public override string Name
        {
            get
            {
                return SSHPortSupplier.GetFormattedSSHConnectionName(_remoteSystem.ConnectionInfo);
            }
        }

        public string AttachToProcess(int pid, string preAttachCommand)
        {
            var gdbStart = new liblinux.Services.GdbServerStartInfo();
            gdbStart.ProcessId = pid;   // indicates an attach operation
            gdbStart.PreLaunchCommand = preAttachCommand;
            _gdbserver = _remoteSystem.Services.GdbServer.Start(gdbStart); // throws on failure
            return "localhost:" + _gdbserver.StartInfo.LocalPort.ToString(CultureInfo.InvariantCulture);
        }

        public override List<Process> ListProcesses()
        {
            string username = string.Empty;
            var usernameCommand = _remoteSystem.Shell.ExecuteCommand("id -u -n", Timeout.InfiniteTimeSpan);
            if (usernameCommand.ExitCode == 0)
            {
                username = usernameCommand.Output.TrimEnd('\n', '\r'); // trim line endings because 'id' command ends with a newline
            }

            string operatingSystem = string.Empty;
            var operatingSystemCommand = _remoteSystem.Shell.ExecuteCommand("uname", Timeout.InfiniteTimeSpan);
            if (operatingSystemCommand.ExitCode == 0)
            {
                operatingSystem = operatingSystemCommand.Output.TrimEnd('\n', '\r'); // trim line endings because 'uname' command ends with a newline
            }

            string architecture = string.Empty;
            var architectureCommand = _remoteSystem.Shell.ExecuteCommand("uname -m", Timeout.InfiniteTimeSpan);
            if (architectureCommand.ExitCode == 0)
            {
                architecture = architectureCommand.Output.TrimEnd('\n', '\r'); // trim line endings because 'uname -m' command ends with a newline
            }

            SystemInformation systemInformation = new SystemInformation(username, architecture, operatingSystem.ConvertToPlatformID());

            PSOutputParser psOutputParser = new PSOutputParser(systemInformation);

            var command = _remoteSystem.Shell.ExecuteCommand(psOutputParser.PSCommandLine, Timeout.InfiniteTimeSpan);
            if (command.ExitCode != 0)
            {
                throw new CommandFailedException(StringResources.Error_PSFailed);
            }

            return psOutputParser.Parse(command.Output);
        }

        /// <inheritdoc/>
        public override void BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand)
        {
            if (runInShell)
            {
                var command = new AD7UnixAsyncShellCommand(new SSHRemoteShell(_remoteSystem), callback, true);
                command.Start(commandText);
                asyncCommand = command;
            }
            else
            {
                var command = new SSHUnixAsyncCommand(_remoteSystem, callback);
                command.Start(commandText);
                asyncCommand = command;
            }
        }

        public override int ExecuteCommand(string commandText, int timeout, out string commandOutput, out string errorMessage)
        {
            INonHostedCommand command = _remoteSystem.Shell.ExecuteCommand(commandText, timeout);
            commandOutput = command.Output;
            errorMessage = command.ErrorOutput;
            return command.ExitCode;
        }

        /// <summary>
        /// Copy a single file from the local machine to the remote machine.
        /// </summary>
        /// <param name="sourcePath">File on the local machine.</param>
        /// <param name="destinationPath">Destination path on the remote machine.</param>
        public override void CopyFile(string sourcePath, string destinationPath)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentNullException(sourcePath);
            }

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(StringResources.Error_SourceFileNotFound.FormatCurrentCultureWithArgs(sourcePath));
            }

            _remoteSystem.FileSystem.UploadFile(sourcePath, destinationPath);
        }

        /// <summary>
        /// Creates directory provided the path. Does not fail if the directory already exists.
        /// </summary>
        /// <param name="path">Path on the remote machine.</param>
        /// <returns>Full path of the created directory.</returns>
        public override string MakeDirectory(string path)
        {
            bool directoryExists = false;
            liblinux.IO.IRemoteFileSystemInfo stat = null;
            try
            {
                stat = _remoteSystem.FileSystem.Stat(path);
                directoryExists = stat.IsDirectory();
            }
            catch
            {
                // Catching and eating all exceptions.
                // Unfortunately the exceptions that are thrown by liblinux are not public, so we can't specialize it.
            }

            if (stat == null && !directoryExists)
            {
                return _remoteSystem.FileSystem.CreateDirectory(path).FullPath;
            }
            else if (stat != null && directoryExists)
            {
                return _remoteSystem.FileSystem.GetDirectory(path).FullPath;
            }
            else
            {
                // This may happen if the user does not have permissions or if it is a file, etc.
                throw new ArgumentException(StringResources.Error_InvalidDirectory.FormatCurrentCultureWithArgs(path), nameof(path));
            }
        }

        public override string GetUserHomeDirectory()
        {
            return _remoteSystem.FileSystem.GetDirectory(liblinux.IO.SpecialDirectory.Home).FullPath;
        }

        public override bool IsOSX()
        {
            return _remoteSystem.Properties.Id == SystemId.OSX;
        }

        public override bool IsLinux()
        {
            var command = _remoteSystem.Shell.ExecuteCommand("uname", Timeout.InfiniteTimeSpan);
            if (command.ExitCode != 0)
            {
                return false;
            }

            
            return string.Equals(command.Output?.Trim(), "Linux", StringComparison.Ordinal);
        }

        public override void Close()
        {
            if (_gdbserver != null)
            {
                _gdbserver.Stop();
                _gdbserver = null;
            }

            if (_remoteSystem != null)
            {
                _remoteSystem.Dispose();
                _remoteSystem = null;
            }
        }
    }
}
