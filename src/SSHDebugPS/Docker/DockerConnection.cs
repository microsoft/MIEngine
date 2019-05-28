// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS.Docker
{
    internal class DockerConnection : PipeConnection
    {
        private string _containerName;

        public DockerConnection(DockerTransportSettings settings, Connection outerConnection, string name, string containerName)
        : base(settings, outerConnection, name)
        {
            _containerName = containerName;
        }

        public override void BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand)
        {
            if (IsClosed)
            {
                throw new ObjectDisposedException(nameof(PipeConnection));
            }

            if (runInShell)
            {
                AD7UnixAsyncShellCommand command = new AD7UnixAsyncShellCommand(CreateShellFromSettings(TransportSettings, OuterConnection), callback, closeShellOnComplete: true);
                command.Start(commandText);

                asyncCommand = command;
            }
            else
            {
                DockerExecSettings settings = new DockerExecSettings(_containerName, commandText, true);
                AD7UnixAsyncCommand command = new AD7UnixAsyncCommand(CreateShellFromSettings(settings, OuterConnection, true), callback, closeShellOnComplete: true);

                asyncCommand = command;
            }
        }

        public override void CopyFile(string sourcePath, string destinationPath)
        {
            DockerCopySettings settings;
            string tmpFile = null;

            if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, StringResources.Error_CopyFile_SourceNotFound, sourcePath), nameof(sourcePath));
            }

            if (OuterConnection != null)
            {
                tmpFile = "/tmp" + "/" + StringResources.CopyFile_TempFilePrefix + Guid.NewGuid();
                OuterConnection.CopyFile(sourcePath, tmpFile);
                settings = new DockerCopySettings(tmpFile, destinationPath, _containerName, true);
            }
            else
            {
                settings = new DockerCopySettings(sourcePath, destinationPath, _containerName, true);
            }

            ICommandRunner shell = CreateShellFromSettings(settings, OuterConnection);

            ManualResetEvent resetEvent = new ManualResetEvent(false);
            int exitCode = -1;
            shell.Closed += (e, args) =>
            {
                exitCode = args;
                resetEvent.Set();
                try
                {
                    if (OuterConnection != null && !string.IsNullOrEmpty(tmpFile))
                    {
                        string output;
                        // Don't error on failing to remove the temporary file.
                        int exit = OuterConnection.ExecuteCommand("rm " + tmpFile, Timeout.Infinite, out output);
                        Debug.Assert(exit == 0, FormattableString.Invariant($"Removing file exited with {exit} and message {output}"));
                    }
                }
                catch (Exception ex) // don't error on cleanup
                {
                    Debug.Fail("Exception thrown while cleaning up temp file. " + ex.Message);
                }
            };

            bool complete = resetEvent.WaitOne(Timeout.Infinite);
            if (!complete || exitCode != 0)
            {
                throw new CommandFailedException(StringResources.Error_CopyFileFailed);
            }
        }

        public override void ExecuteSyncCommand(string commandDescription, string commandText, out string commandOutput, int timeout, out int exitCode)
        {
            int exit = -1;
            string output = string.Empty;
            if (OuterConnection != null)
            {
                string dockerCommand = string.Format(CultureInfo.InvariantCulture, StringResources.DockerExecCommandFormat, _containerName, commandText);
                string waitMessage = string.Format(CultureInfo.InvariantCulture, StringResources.WaitingOp_ExecutingCommand, commandDescription);
                VS.VSOperationWaiter.Wait(waitMessage, true, () =>
                {
                    exit = OuterConnection.ExecuteCommand(dockerCommand, timeout, out output);
                });
            }
            else
            {
                //local exec command
                exit = ExecuteCommand(commandText, timeout, out output);
            }

            exitCode = exit;
            commandOutput = output;
        }
    }
}
