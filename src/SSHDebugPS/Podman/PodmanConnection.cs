// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.Utilities;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.SSHDebugPS.Podman
{
    internal sealed class PodmanConnection : PipeConnection
    {
        #region Statics

        internal static string CreateConnectionString(string containerName, string remoteConnectionName, string hostName)
        {
            // Reuses the same format as Docker connection strings
            return DockerConnection.CreateConnectionString(containerName, remoteConnectionName, hostName);
        }

        internal static bool TryConvertConnectionStringToSettings(string connectionString, out PodmanContainerTransportSettings settings, out Connection remoteConnection)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (DockerConnection.TryConvertConnectionStringToSettings(connectionString, out DockerContainerTransportSettings dockerSettings, out remoteConnection))
            {
                settings = new PodmanContainerTransportSettings(dockerSettings.HostName, dockerSettings.ContainerName, remoteConnection != null);
                return true;
            }

            settings = null;
            return false;
        }

        #endregion

        private readonly string _containerName;
        private readonly PodmanExecutionManager _executionManager;
        private readonly PodmanContainerTransportSettings _settings;

        public PodmanConnection(PodmanContainerTransportSettings settings, Connection outerConnection, string name)
            : base(outerConnection, name)
        {
            _settings = settings;
            _containerName = settings.ContainerName;
            _executionManager = new PodmanExecutionManager(settings, outerConnection);
        }

        public override int ExecuteCommand(string commandText, int timeout, out string commandOutput, out string errorMessage)
        {
            return _executionManager.ExecuteCommand(commandText, timeout, out commandOutput, out errorMessage);
        }

        /// <inheritdoc/>
        public override void BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand)
        {
            if (IsClosed)
            {
                throw new ObjectDisposedException(nameof(PipeConnection));
            }

            var commandRunner = GetExecCommandRunner(commandText, handleRawOutput: runInShell == false);
            asyncCommand = new PipeAsyncCommand(commandRunner, callback);
        }

        public override void CopyFile(string sourcePath, string destinationPath)
        {
            PodmanCopySettings settings;
            string tmpFile = null;

            if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
            {
                throw new ArgumentException(StringResources.Error_CopyFile_SourceNotFound.FormatCurrentCultureWithArgs(sourcePath), nameof(sourcePath));
            }

            if (OuterConnection != null)
            {
                tmpFile = "/tmp" + "/" + StringResources.CopyFile_TempFilePrefix + Guid.NewGuid();
                OuterConnection.CopyFile(sourcePath, tmpFile);
                settings = new PodmanCopySettings(_settings, tmpFile, destinationPath);
            }
            else
            {
                settings = new PodmanCopySettings(_settings, sourcePath, destinationPath);
            }

            ICommandRunner runner = GetCommandRunner(settings);

            ManualResetEvent resetEvent = new ManualResetEvent(false);
            int exitCode = -1;
            runner.Closed += (e, args) =>
            {
                exitCode = args;
                resetEvent.Set();
                try
                {
                    if (OuterConnection != null && !string.IsNullOrEmpty(tmpFile))
                    {
                        string output;
                        string errorMessage;
                        int exit = OuterConnection.ExecuteCommand("rm " + tmpFile, 5000, out output, out errorMessage);
                        Debug.Assert(exit == 0, FormattableString.Invariant($"Removing file exited with {exit} and message {output}. {errorMessage}"));
                    }
                }
                catch (Exception ex)
                {
                    Debug.Fail("Exception thrown while cleaning up temp file. " + ex.Message);
                }
            };

            runner.Start();

            bool complete = resetEvent.WaitOne(Timeout.Infinite);
            if (!complete || exitCode != 0)
            {
                throw new CommandFailedException(StringResources.Error_CopyFileFailed);
            }
        }

        public override string GetUserHomeDirectory()
        {
            return ExecuteCommand("eval echo '~'", Timeout.Infinite);
        }

        private ICommandRunner GetExecCommandRunner(string commandText, bool handleRawOutput = false)
        {
            var execSettings = new PodmanExecSettings(this._settings, commandText, handleRawOutput);
            return GetCommandRunner(execSettings, handleRawOutput: handleRawOutput);
        }

        private ICommandRunner GetCommandRunner(IPipeTransportSettings settings, bool handleRawOutput = false)
        {
            if (OuterConnection == null)
            {
                return LocalCommandRunner.CreateInstance(handleRawOutput, settings);
            }
            else
            {
                return new RemoteCommandRunner(settings, OuterConnection, handleRawOutput);
            }
        }

        protected override string ProcFSErrorMessage
        {
            get
            {
                return String.Concat(base.ProcFSErrorMessage, Environment.NewLine, StringResources.Error_EnsurePodmanContainerIsLinux);
            }
        }
    }
}
