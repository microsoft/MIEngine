// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS
{
    internal abstract class PipeConnection : Connection
    {
        private readonly object _lock = new object();
        private readonly IPipeTransportSettings _settings;
        private readonly Connection _outerConnection;

        private readonly ShellExecutionManager _shellExecutionManager;
        private readonly List<ICommandRunner> _shellList = new List<ICommandRunner>();
        private bool _isClosed;
        private string _name;

        public override string Name => _name;

        protected Connection OuterConnection => _outerConnection;
        protected IPipeTransportSettings TransportSettings => _settings;
        protected bool IsClosed => _isClosed;

        /// <summary>
        /// Create a new pipe connection object
        /// </summary>
        /// <param name="pipeTransportSettings">Settings</param>
        /// <param name="outerConnection">[Optional] the SSH connection (or maybe something else in future) used to connect to the target.</param>
        /// <param name="name">The full name of this connection</param>
        public PipeConnection(IPipeTransportSettings pipeTransportSettings, Connection outerConnection, string name)
        {
            Debug.Assert(pipeTransportSettings != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(name));

            _name = name;
            _settings = pipeTransportSettings;
            _outerConnection = outerConnection;
            _shellExecutionManager = new ShellExecutionManager(CreateShellFromSettings(_settings, _outerConnection));
        }

        public override int ExecuteCommand(string commandText, int timeout, out string commandOutput)
        {
            if (_isClosed)
            {
                throw new ObjectDisposedException(nameof(PipeConnection));
            }

            return _shellExecutionManager.ExecuteCommand(commandText, timeout, out commandOutput); ;
        }

        public override void Close()
        {
            _isClosed = true;

            lock (_lock)
            {
                foreach (ICommandRunner rawShell in _shellList)
                {
                    rawShell.Dispose();
                }

                _shellList.Clear();
            }
        }

        protected ICommandRunner CreateShellFromSettings(IPipeTransportSettings settings, Connection outerConnection, bool isCommandShell = false)
        {
            ICommandRunner rawShell;
            if (_outerConnection == null)
            {
                if (isCommandShell)
                    rawShell = new LocalRawCommandRunner(settings.ExeCommand, settings.ExeCommandArgs);
                else
                    rawShell = new LocalBufferedCommandRunner(settings.ExeCommand, settings.ExeCommandArgs);
            }
            else
            {
                rawShell = new RemoteCommandRunner(settings.ExeCommand, settings.ExeCommandArgs, outerConnection);
            }

            lock (_lock)
            {
                _shellList.Add(rawShell);
            }

            rawShell.Closed += (sender, eventArgs) =>
            {
                if (_isClosed)
                    return;

                lock (_lock)
                {
                    if (_isClosed)
                        return;

                    _shellList.Remove(rawShell);
                }
            };

            return rawShell;
        }

        public override string MakeDirectory(string path)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(path), nameof(path));
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string commandOutput;
            string command = "mkdir -p \"" + path + "\""; // -p ignores if the directory is already there
            ExecuteCommand(command, Timeout.Infinite, throwOnFailure: true, commandOutput: out commandOutput);

            return GetFullPath(path);
        }

        private string GetFullPath(string path)
        {
            string fullpath;
            string output; // throw away variable

            string pwd;
            ExecuteCommand("pwd", Timeout.Infinite, throwOnFailure: true, commandOutput: out pwd);
            ExecuteCommand($"cd \"{path}\"; pwd", Timeout.Infinite, throwOnFailure: true, commandOutput: out fullpath);
            ExecuteCommand($"cd \"{pwd}\"", Timeout.Infinite, throwOnFailure: false, commandOutput: out output); //This might fail in some instances, so ignore a failure and ignore output

            return fullpath;
        }

        public override string GetUserHomeDirectory()
        {
            string command = "echo $HOME";
            string commandOutput;
            ExecuteCommand(command, Timeout.Infinite, throwOnFailure: true, commandOutput: out commandOutput);

            return commandOutput.TrimEnd('\n', '\r');
        }

        public override bool IsOSX()
        {
            return false;
        }

        public override bool IsLinux()
        {
            string command = "uname";
            string commandOutput;
            if(!ExecuteCommand(command, Timeout.Infinite, throwOnFailure: false, commandOutput: out commandOutput))
            {
                return false;
            }
            return commandOutput.StartsWith("Linux", StringComparison.OrdinalIgnoreCase);
        }

        public bool TryGetUsername(out string username)
        {
            username = string.Empty;
            string command = "id -u -n";
            string commandOutput;
            if (ExecuteCommand(command, Timeout.Infinite, throwOnFailure: false, commandOutput: out commandOutput))
            {

                username = commandOutput;
                return true;
            }

            return false;
        }

        public override List<Process> ListProcesses()
        {
            string username;
            TryGetUsername(out username);

            string commandOutput;

            if (!ExecuteCommand(PSOutputParser.PSCommandLine, Timeout.Infinite, false, out commandOutput))
            {
                if (!ExecuteCommand(PSOutputParser.AltPSCommandLine, Timeout.Infinite, false, out commandOutput))
                {
                    throw new CommandFailedException(StringResources.Error_PSFailed);
                }
            }

            return PSOutputParser.Parse(commandOutput, username);
        }

        /// <summary>
        /// Checks command exit code and if it is non-zero, it will throw a CommandFailedException with an error message if 'throwOnFailure' is true.
        /// </summary>
        /// <returns>true if command succeeded.</returns>
        protected bool ExecuteCommand(string command, int timeout, bool throwOnFailure, out string commandOutput)
        {
            commandOutput = string.Empty;
            int exitCode = ExecuteCommand(command, timeout, out commandOutput);
            if (throwOnFailure && exitCode != 0)
            {
                string error = String.Format(CultureInfo.InvariantCulture, StringResources.CommandFailedMessageFormat, command, exitCode, commandOutput);
                throw new CommandFailedException(error);
            }
            else
                return exitCode == 0;
        }
    }
}
