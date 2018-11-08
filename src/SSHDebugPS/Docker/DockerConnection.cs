// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS.Docker
{
    internal class DockerConnection : PipeConnection
    {
        public DockerConnection(DockerTransportSettings settings, Connection outerConnection, string name)
            : base(settings, outerConnection, name)
        { }

        public DockerConnection(DockerTransportSettings settings, Connection outerConnection, string name, int timeout)
        : base(settings, outerConnection, name, timeout)
        { }

        public override void BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand)
        {
            if (IsClosed)
            {
                throw new ObjectDisposedException(nameof(PipeConnection));
            }

            if (runInShell)
            {
                AD7UnixAsyncShellCommand command = new AD7UnixAsyncShellCommand(CreateShellForSettings(TransportSettings, OuterConnection), callback, closeShellOnComplete: true);
                command.Start(commandText);

                asyncCommand = command;
            }
            else
            {
                /// TODO: figure out what RunInShell means?
                DockerExecSettings settings = new DockerExecSettings(Name, commandText, true);
                AD7UnixAsyncCommand command = new AD7UnixAsyncCommand(CreateShellForSettings(settings, OuterConnection, true), callback, closeShellOnComplete: true);

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
            DockerCopySettings settings = new DockerCopySettings(source, destination, this.Name, true);
            IRawShell shell = CreateShellForSettings(settings, OuterConnection);

            int timeout = 20000;
            ManualResetEvent resetEvent = new ManualResetEvent(false);
            int exitCode = -1;
            shell.Closed += (e, args) =>
            {
                exitCode = args;
                resetEvent.Set();
            };

            bool complete = resetEvent.WaitOne(timeout);
            if (!complete || exitCode != 0)
            {
                throw new CommandFailedException("CopyDirectory");
            }
        }
    }
}
