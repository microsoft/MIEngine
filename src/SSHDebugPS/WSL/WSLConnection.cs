// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using System.Collections.Generic;
using System;
using System.Text;
using System.Threading;
using SysProcess = System.Diagnostics.Process;
using System.Threading.Tasks;

namespace Microsoft.SSHDebugPS.WSL
{
    internal class WSLConnection : PipeConnection
    {
        public WSLConnection(string distribution) : base(outerConnection:null, name: distribution)
        {
        }

        /// <inheritdoc/>
        public override int ExecuteCommand(string commandText, int timeout, out string commandOutput, out string errorMessage)
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                Task<ProcessResult> task = LocalProcessAsyncRunner.ExecuteProcessAsync(WSLCommandLine.GetExecStartInfo(this.Name, commandText), cancellationTokenSource.Token);
                if (!task.Wait(timeout))
                {
                    cancellationTokenSource.Cancel();
                    throw new TimeoutException();
                }

                ProcessResult result = task.Result;
                commandOutput = string.Join("\n", result.StdOut);
                errorMessage = string.Join("\n", result.StdErr);
                return result.ExitCode;
            }
        }

        /// <inheritdoc/>
        public override void BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand)
        {
            if (IsClosed)
            {
                throw new ObjectDisposedException(nameof(WSLConnection));
            }

            string args = WSLCommandLine.GetExecCommandLineArgs(this.Name, commandText);
            ICommandRunner commandRunner = LocalCommandRunner.CreateInstance(handleRawOutput: runInShell == false, WSLCommandLine.ExePath, args);
            asyncCommand = new PipeAsyncCommand(commandRunner, callback);
        }

        /// <inheritdoc/>
        public override void CopyFile(string sourcePath, string destinationPath)
        {
            bool IsFullyQualifiedWindowsLocalPath(string path)
            {
                if (path == null)
                    return false;

                if (path.Length < 3)
                    return false;

                char driveLetter = sourcePath[0];
                bool isDriveLetter = (driveLetter >= 'a' && driveLetter <= 'z') || (driveLetter >= 'A' && driveLetter <= 'Z');
                if (!isDriveLetter)
                {
                    return false;
                }

                if (sourcePath[1] != ':')
                {
                    return false;
                }

                char slash = sourcePath[2];
                if (slash != '\\' && slash != '/')
                {
                    return false;
                }

                return true;
            }

            if (!IsFullyQualifiedWindowsLocalPath(sourcePath))
            {
                throw new ArgumentOutOfRangeException(nameof(sourcePath));
            }

            StringBuilder commandBuilder = new StringBuilder();
            commandBuilder.Append("cp /mnt/");
            commandBuilder.Append(char.ToLowerInvariant(sourcePath[0]));

            // Append all the other characters of the path with special handling for backslash and space
            for (int c = 2; c < sourcePath.Length; c++)
            {
                char ch = sourcePath[c];
                switch (ch)
                {
                    case '\\':
                        commandBuilder.Append('/');
                        break;

                    case ' ':
                        commandBuilder.Append(@"\ ");
                        break;

                    default:
                        commandBuilder.Append(ch);
                        break;
                }
            }

            commandBuilder.Append(' ');
            commandBuilder.Append(destinationPath);

            this.ExecuteCommand(commandBuilder.ToString(), Timeout.Infinite);
        }

        /// <inheritdoc/>
        public override bool IsLinux()
        {
            return true;
        }

        /// <inheritdoc/>
        public override bool IsOSX()
        {
            return false;
        }
    }
}