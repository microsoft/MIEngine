// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using Microsoft.SSHDebugPS.Utilities;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS.Docker
{
    internal class ShellCommandCallback : IDebugUnixShellCommandCallback
    {
        private ManualResetEvent _commandCompleteEvent;
        private int _exitCode = -1;
        private readonly StringBuilder _outputBuilder = new StringBuilder();
        private readonly StringBuilder _errorBuilder = new StringBuilder();

        public int ExitCode => _exitCode;
        public string CommandOutput => _outputBuilder.ToString();
        public string ErrorOutput => _errorBuilder.ToString();

        public ShellCommandCallback(ManualResetEvent commandCompleteEvent)
        {
            _commandCompleteEvent = commandCompleteEvent;
        }

        public void OnOutputLine(string line)
        {
            _outputBuilder.AppendLine(line);
        }

        public void OnExit(string exitCode)
        {
            if (!string.IsNullOrWhiteSpace(exitCode))
            {
                int exitCodeValue;
                if (int.TryParse(exitCode.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out exitCodeValue))
                {
                    _exitCode = exitCodeValue;
                }
            }

            _commandCompleteEvent.Set();
        }
    }

    internal class DockerExecutionManager
    {
        private DockerAsyncCommand _currentCommand;

        private Connection _outerConnection = null;
        private DockerTransportSettingsBase _baseSettings;
        private readonly ManualResetEvent _commandCompleteEvent = new ManualResetEvent(false);

        public DockerExecutionManager(DockerTransportSettingsBase baseSettings, Connection outerConnection)
        {
            _baseSettings = baseSettings;
            _outerConnection = outerConnection;
        }

        private ICommandRunner GetExecCommandRunner(string command, bool runInShell, bool makeInteractive)
        {
            if (_baseSettings is DockerContainerTransportSettings containerTransportSettings)
            {
                var execSettings = new DockerContainerExecSettings(containerTransportSettings, command, runInShell, makeInteractive);

                if (_outerConnection == null)
                {
                    return new LocalCommandRunner(execSettings);
                }
                else
                    return new RemoteCommandRunner(execSettings, _outerConnection);
            }
            else if (_baseSettings is DockerCommandSettings commandSettings)
            {
                var execSettings = new DockerExecSettings(commandSettings, command, string.Empty);
                if (_outerConnection == null)
                {
                    return new LocalCommandRunner(execSettings);
                }
                else
                    return new RemoteCommandRunner(execSettings, _outerConnection);
            }
            else
            {
                Debug.Assert(_baseSettings is DockerCommandSettings, "baseSettings should be DockerCommandSettings");
                throw new InvalidOperationException("Unexpected command settings");
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
                DockerAsyncCommand command = new DockerAsyncCommand(commandRunner, commandCallback);

                try
                {
                    _currentCommand = command;
                    if (!_commandCompleteEvent.WaitOne(timeout))
                    {
                        errorMessage = StringResources.Error_OperationTimedOut;
                        return ExitCodes.OPERATION_TIMEDOUT; // ERROR_TIMEOUT
                    }

                    commandOutput = commandCallback.CommandOutput.Trim('\n', '\r'); // trim ending newlines
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
