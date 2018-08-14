// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using System.IO;
using System.Globalization;
using liblinux;
using liblinux.Shell;

namespace Microsoft.SSHDebugPS
{
    internal class Connection
    {
        private liblinux.UnixSystem _remoteSystem;
        private liblinux.Services.GdbServer _gdbserver = null;

        public Connection(liblinux.UnixSystem remoteSystem)
        {
            _remoteSystem = remoteSystem;
        }

        internal ConnectionInfo ConnectionInfo
        {
            get
            {
                return _remoteSystem.ConnectionInfo;
            }
        }

        internal List<PSOutputParser.Process> ListProcesses()
        {
            var command = _remoteSystem.Shell.ExecuteCommand(PSOutputParser.CommandText);
            if (command.ExitCode != 0)
            {
                throw new CommandFailedException(StringResources.Error_PSFailed);
            }

            return PSOutputParser.Parse(command.Output);
        }

        internal void BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand)
        {
            if (runInShell)
            {
                var command = new AD7UnixAsyncShellCommand(new StreamingShell(_remoteSystem), callback);
                command.Start(commandText);
                asyncCommand = command;
            }
            else
            {
                var command = new AD7UnixAsyncCommand(_remoteSystem, callback);
                command.Start(commandText);
                asyncCommand = command;
            }
        }

        internal int ExecuteCommand(string commandText, int timeout, out string commandOutput)
        {
            var command = _remoteSystem.Shell.ExecuteCommand(commandText, timeout);
            commandOutput = command.Output;
            return command.ExitCode;
        }

        /// <summary>
        /// Copy a single file from the local machine to the remote machine.
        /// </summary>
        /// <param name="sourcePath">File on the local machine.</param>
        /// <param name="destinationPath">Destination path on the remote machine.</param>
        internal void CopyFile(string sourcePath, string destinationPath)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentNullException(sourcePath);
            }

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, StringResources.Error_SourceFileNotFound, sourcePath));
            }

            _remoteSystem.FileSystem.UploadFile(sourcePath, destinationPath);
        }

        /// <summary>
        /// Creates directory provided the path. Does not fail if the directory already exists.
        /// </summary>
        /// <param name="path">Path on the remote machine.</param>
        /// <returns>Full path of the created directory.</returns>
        internal string MakeDirectory(string path)
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
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, StringResources.Error_InvalidDirectory, path), nameof(path));
            }
        }

        internal string GetUserHomeDirectory()
        {
            return _remoteSystem.FileSystem.GetDirectory(liblinux.IO.SpecialDirectory.Home).FullPath;
        }

        public string AttachToProcess(int pid, string preAttachCommand)
        {
            var gdbStart = new liblinux.Services.GdbServerStartInfo();
            gdbStart.ProcessId = pid;   // indicates an attach operation
            gdbStart.PreLaunchCommand = preAttachCommand;
            _gdbserver = _remoteSystem.Services.GdbServer.Start(gdbStart); // throws on failure
            return "localhost:" + _gdbserver.StartInfo.LocalPort.ToString(CultureInfo.InvariantCulture);
        }

        internal bool IsOSX()
        {
            return _remoteSystem.Properties.Id == SystemId.OSX;
        }

        internal bool IsLinux()
        {
            var command = _remoteSystem.Shell.ExecuteCommand("uname");
            if (command.ExitCode != 0)
            {
                return false;
            }

            return command.Output.Trim().Equals("Linux");
        }

        internal void Clean()
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
