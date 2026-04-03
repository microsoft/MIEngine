// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.Utilities;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS.Podman
{
    internal class PodmanExecutionManager
    {
        private PipeAsyncCommand _currentCommand;

        private Connection _outerConnection = null;
        private PodmanContainerTransportSettings _baseSettings;
        private readonly ManualResetEvent _commandCompleteEvent = new ManualResetEvent(false);

        public PodmanExecutionManager(PodmanContainerTransportSettings baseSettings, Connection outerConnection)
        {
            _baseSettings = baseSettings;
            _outerConnection = outerConnection;
        }

        private ICommandRunner GetExecCommandRunner(string command, bool runInShell, bool makeInteractive)
        {
            var execSettings = new PodmanExecSettings(_baseSettings, command, runInShell, makeInteractive);

            if (_outerConnection == null)
            {
                return new LocalCommandRunner(execSettings);
            }
            else
            {
                return new RemoteCommandRunner(execSettings, _outerConnection, handleRawOutput: false);
            }
        }

        public int ExecuteCommand(string commandText, int timeout, out string commandOutput, out string errorMessage, bool runInShell = true, bool makeInteractive = true)
        {
            commandOutput = string.Empty;
            errorMessage = string.Empty;
            if (_currentCommand != null)
            {
                throw new InvalidOperationException("already a command processing");
            }
            _commandCompleteEvent.Reset();

            using (ICommandRunner commandRunner = GetExecCommandRunner(commandText, runInShell, makeInteractive))
            {
                ShellCommandCallback commandCallback = new ShellCommandCallback(_commandCompleteEvent);
                PipeAsyncCommand command = new PipeAsyncCommand(commandRunner, commandCallback);

                try
                {
                    _currentCommand = command;
                    if (!_commandCompleteEvent.WaitOne(timeout))
                    {
                        errorMessage = StringResources.Error_OperationTimedOut;
                        return ExitCodes.OPERATION_TIMEDOUT;
                    }

                    commandOutput = commandCallback.CommandOutput.Trim('\n', '\r');
                    errorMessage = _currentCommand.ErrorMessage;
                    return commandCallback.ExitCode;
                }
                catch (ObjectDisposedException ode)
                {
                    Debug.Fail("Why are we operating on a disposed object?");
                    errorMessage = ode.ToString();
                    return ExitCodes.OBJECTDISPOSED;
                }
                catch (Exception e)
                {
                    Debug.Fail(e.Message);
                    errorMessage = e.ToString();
                    return -1;
                }
                finally
                {
                    _currentCommand.Close();
                    _currentCommand = null;
                }
            }
        }
    }
}
