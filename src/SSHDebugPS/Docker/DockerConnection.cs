// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.DebugEngineHost;
using Microsoft.SSHDebugPS.Utilities;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS.Docker
{
    internal class DockerConnection : PipeConnection
    {
        #region Statics

        internal const string SshPrefixRegex = @"^[Ss]{2}[Hh]\s*=\s*";
        internal const string SshPrefix = "ssh=";
        internal const string DockerHostPrefixRegex = @"^host\s*=\s*";
        internal const string DockerHostPrefix = "host=";
        internal const char Separator = ';';

        internal static string CreateConnectionString(string containerName, string remoteConnectionName, string hostName)
        {
            string connectionString = containerName;
            if (!string.IsNullOrWhiteSpace(remoteConnectionName))
            {
                connectionString += Separator + SshPrefix + remoteConnectionName;
            }

            if (!string.IsNullOrWhiteSpace(hostName))
            {
                connectionString += Separator + DockerHostPrefix + hostName;
            }

            return connectionString;
        }

        internal static bool TryConvertConnectionStringToSettings(string connectionString, out DockerContainerTransportSettings settings, out Connection remoteConnection)
        {
            remoteConnection = null;
            settings = null;

            string containerName = string.Empty;
            string hostName = string.Empty;
            bool invalidString = false;

            // Assume format is <containername>;ssh=<sshconnection>;host=<dockerhostvalue> or some mixture
            string[] connectionStrings = connectionString.Split(Separator);

            if (connectionStrings.Length <= 3 && connectionStrings.Length > 0)
            {
                Regex SshRegex = new Regex(SshPrefixRegex);
                Regex dockerHostRegex = new Regex(DockerHostPrefixRegex);

                foreach (var item in connectionStrings)
                {
                    string segment = item.Trim(' ');
                    if (SshRegex.IsMatch(segment))
                    {
                        Match match = SshRegex.Match(segment);
                        remoteConnection = ConnectionManager.GetSSHConnection(segment.Substring(match.Length));
                    }
                    else if (dockerHostRegex.IsMatch(segment))
                    {
                        Match match = dockerHostRegex.Match(segment);
                        hostName = segment.Substring(match.Length);
                    }
                    else if (segment.Contains("="))
                    {
                        invalidString = true;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(containerName))
                        {
                            Debug.Fail("containerName should be empty");
                            invalidString = true;
                        }
                        else
                        {
                            containerName = segment;
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(containerName) && !invalidString)
            {
                settings = new DockerContainerTransportSettings(hostName, containerName, remoteConnection != null);
                return true;
            }

            return false;
        }

        #endregion

        private readonly string _containerName;
        private readonly DockerExecutionManager _dockerExecutionManager;
        private readonly DockerContainerTransportSettings _settings;

        public DockerConnection(DockerContainerTransportSettings settings, Connection outerConnection, string name)
            : base(outerConnection, name)
        {
            _settings = settings;
            _containerName = settings.ContainerName;
            _dockerExecutionManager = new DockerExecutionManager(settings, outerConnection);
        }

        public override int ExecuteCommand(string commandText, int timeout, out string commandOutput, out string errorMessage)
        {
            return _dockerExecutionManager.ExecuteCommand(commandText, timeout, out commandOutput, out errorMessage);
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
            DockerCopySettings settings;
            string tmpFile = null;

            if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
            {
                throw new ArgumentException(StringResources.Error_CopyFile_SourceNotFound.FormatCurrentCultureWithArgs(sourcePath), nameof(sourcePath));
            }

            if (OuterConnection != null)
            {
                tmpFile = "/tmp" + "/" + StringResources.CopyFile_TempFilePrefix + Guid.NewGuid();
                OuterConnection.CopyFile(sourcePath, tmpFile);
                settings = new DockerCopySettings(_settings, tmpFile, destinationPath);
            }
            else
            {
                settings = new DockerCopySettings(_settings, sourcePath, destinationPath);
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
                        // Don't error on failing to remove the temporary file.
                        int exit = OuterConnection.ExecuteCommand("rm " + tmpFile, 5000, out output, out errorMessage);
                        Debug.Assert(exit == 0, FormattableString.Invariant($"Removing file exited with {exit} and message {output}. {errorMessage}"));
                    }
                }
                catch (Exception ex) // don't error on cleanup
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

        // Base implementation was evaluating '$HOME' on the host machine and not the client machine, causing the home directory to be wrong.
        public override string GetUserHomeDirectory()
        {
            return ExecuteCommand("eval echo '~'", Timeout.Infinite);
        }

        private ICommandRunner GetExecCommandRunner(string commandText, bool handleRawOutput = false)
        {
            var execSettings = new DockerExecSettings(this._settings, commandText, handleRawOutput);

            return GetCommandRunner(execSettings, handleRawOutput: handleRawOutput);
        }

        private ICommandRunner GetCommandRunner(DockerContainerTransportSettings settings, bool handleRawOutput = false)
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
                HostTelemetry.SendEvent(TelemetryHelper.Event_ProcFSError);
                return String.Concat(base.ProcFSErrorMessage, Environment.NewLine, StringResources.Error_EnsureDockerContainerIsLinux);
            }
        }
    }
}
