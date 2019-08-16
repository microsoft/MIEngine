// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.SSHDebugPS.Utilities;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS.Docker
{
    internal class DockerConnection : PipeConnection
    {
        #region Statics

        internal const string SshPrefix = "ssh=";
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
                foreach (var segment in connectionStrings)
                {
                    if (segment.StartsWith(SshPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        remoteConnection = ConnectionManager.GetSSHConnection(segment.Substring(SshPrefix.Length));
                    }
                    else if (segment.StartsWith(DockerHostPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        hostName = segment.Substring(DockerHostPrefix.Length);
                    }
                    else if (segment.Contains("="))
                    {
                        invalidString = true;
                    }
                    else
                    {
                        Debug.Assert(string.IsNullOrWhiteSpace(containerName), "containerName should be empty");
                        containerName = segment;
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

        private string _containerName;
        private readonly DockerExecutionManager _dockerExecutionManager;

        internal new DockerContainerTransportSettings TransportSettings => (DockerContainerTransportSettings)base.TransportSettings;

        public DockerConnection(DockerContainerTransportSettings settings, Connection outerConnection, string name)
            : base(settings, outerConnection, name)
        {
            _containerName = settings.ContainerName;
            _dockerExecutionManager = new DockerExecutionManager(settings, outerConnection);
        }

        public override int ExecuteCommand(string commandText, int timeout, out string commandOutput, out string errorMessage)
        {
            return _dockerExecutionManager.ExecuteCommand(commandText, timeout, out commandOutput, out errorMessage);
        }

        public override void BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand)
        {
            if (IsClosed)
            {
                throw new ObjectDisposedException(nameof(PipeConnection));
            }

            // Assume in the Begin Async that we are expecting raw output from the process
            var commandRunner = GetExecCommandRunner(commandText, handleRawOutput: true);
            asyncCommand = new DockerAsyncCommand(commandRunner, callback);
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
                settings = new DockerCopySettings(TransportSettings, tmpFile, destinationPath);
            }
            else
            {
                settings = new DockerCopySettings(TransportSettings, sourcePath, destinationPath);
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
            string command = "eval echo '~'";
            string commandOutput;
            string errorMessage;
            ExecuteCommand(command, Timeout.Infinite, throwOnFailure: true, commandOutput: out commandOutput, errorMessage: out errorMessage);

            return commandOutput;
        }

        // Execute a command and wait for a response. No more interaction
        public override void ExecuteSyncCommand(string commandDescription, string commandText, out string commandOutput, int timeout, out int exitCode)
        {
            int exit = -1;
            string output = string.Empty;

            var settings = new DockerExecSettings(TransportSettings, commandText, runInShell: false, makeInteractive: false);
            var runner = GetCommandRunner(settings, true);

            string dockerCommand = "{0} {1}".FormatInvariantWithArgs(settings.Command, settings.CommandArgs);
            string waitMessage = StringResources.WaitingOp_ExecutingCommand.FormatCurrentCultureWithArgs(commandDescription);
            string errorMessage;
            VS.VSOperationWaiter.Wait(waitMessage, true, (cancellationToken) =>
            {
                if (OuterConnection != null)
                {
                    exit = OuterConnection.ExecuteCommand(dockerCommand, timeout, out output, out errorMessage);
                }
                else
                {
                    //local exec command
                    exit = ExecuteCommand(commandText, timeout, out output, out errorMessage);
                }
            });

            exitCode = exit;
            commandOutput = output;
        }

        private ICommandRunner GetExecCommandRunner(string commandText, bool handleRawOutput = false)
        {
            var execSettings = new DockerExecSettings(this.TransportSettings, commandText, handleRawOutput);

            return GetCommandRunner(execSettings, handleRawOutput: handleRawOutput);
        }

        private ICommandRunner GetCommandRunner(DockerContainerTransportSettings settings, bool handleRawOutput = false)
        {
            if (OuterConnection == null)
            {
                if (handleRawOutput)
                {
                    return new RawLocalCommandRunner(settings);
                }
                else
                    return new LocalCommandRunner(settings);
            }
            else
                return new RemoteCommandRunner(settings, OuterConnection);
        }
    }
}
