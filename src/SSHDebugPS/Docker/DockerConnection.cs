// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS.Docker
{
    internal class DockerConnection : PipeConnection
    {
        private string _containerName;

        public DockerConnection(DockerTransportSettings settings, Connection outerConnection, string name, string containerName)
            : this(settings, outerConnection, name, containerName, null)
        { }

        public DockerConnection(DockerTransportSettings settings, Connection outerConnection, string name, string containerName, int? timeout)
        : base(settings, outerConnection, name, timeout)
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
                /// TODO: figure out what RunInShell means?
                DockerExecSettings settings = new DockerExecSettings(_containerName, commandText, true);
                AD7UnixAsyncCommand command = new AD7UnixAsyncCommand(CreateShellFromSettings(settings, OuterConnection, true), callback, closeShellOnComplete: true);

                asyncCommand = command;
            }
        }

        /// <summary>
        /// Docker uses its own command system to copy files and directories into and out of the container.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        public override void CopyDirectory(string sourcePath, string destinationPath)
        {
            Copy(sourcePath, destinationPath);
        }

        public override void CopyFile(string sourcePath, string destinationPath)
        {
            Copy(sourcePath, destinationPath);
        }

        private void Copy(string source, string destination)
        {
            DockerCopySettings settings;
            string tmpFile = null;

            if(!Directory.Exists(source) && !File.Exists(source))
            {
                throw new ArgumentException(FormattableString.Invariant($"Local path: '{source}' does not exist"), nameof(source));
            }

            if (OuterConnection != null)
            {
                tmpFile = "/tmp" + "/" + Path.GetRandomFileName();
                OuterConnection.CopyFile(source, tmpFile);
                settings = new DockerCopySettings(tmpFile, destination, _containerName, true);
            }
            else
            {
                settings = new DockerCopySettings(source, destination, _containerName, true);
            }

            IRawShell shell = CreateShellFromSettings(settings, OuterConnection);

            ManualResetEvent resetEvent = new ManualResetEvent(false);
            int exitCode = -1;
            shell.Closed += (e, args) =>
            {
                exitCode = args;
                resetEvent.Set();
                if (OuterConnection != null && !string.IsNullOrEmpty(tmpFile))
                {
                    string output;
                    // Don't error on failing to remove the temporary file.
                    int exit = OuterConnection.ExecuteCommand("rm " + tmpFile, this.DefaultTimeout, out output);
                    Debug.Assert(exit == 0, FormattableString.Invariant($"Removing file exited with {exit} and message {output}"));
                }
            };

            bool complete = resetEvent.WaitOne(this.DefaultTimeout);
            if (!complete || exitCode != 0)
            {
                throw new CommandFailedException("CopyDirectory");
            }
        }
    }
}
