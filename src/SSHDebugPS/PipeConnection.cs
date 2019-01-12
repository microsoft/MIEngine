// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS
{
    internal abstract class PipeConnection : Connection
    {
        private readonly object _lock = new object();
        private readonly IPipeTransportSettings _settings;
        private readonly Connection _outerConnection;

        private readonly ShellExecutionManager _shellExecutionManager;
        private readonly LinkedList<IRawShell> _shellList = new LinkedList<IRawShell>();
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
        /// <param name="container">The name of the container. For local, this is the same as 'name'.</param>
        public PipeConnection(IPipeTransportSettings pipeTransportSettings, Connection outerConnection, string name, int? timeout)
        {
            Debug.Assert(pipeTransportSettings != null);
            Debug.Assert(!string.IsNullOrEmpty(name));
            //Debug.Assert(!string.IsNullOrEmpty(container));

            _name = name;
            _settings = pipeTransportSettings;
            if (outerConnection != null)
            {
                _outerConnection = outerConnection;
                DefaultTimeout = 15000;
            }
            else
            {
                DefaultTimeout = timeout.HasValue ? timeout.Value : 5000;
            }

            _shellExecutionManager = new ShellExecutionManager(CreateShellFromSettings(_settings, _outerConnection));
        }

        public override void BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand)
        {
            throw new NotImplementedException();
        }

        public override int ExecuteCommand(string commandText, int timeout, out string commandOutput)
        {
            if (_isClosed)
            {
                throw new ObjectDisposedException(nameof(PipeConnection));
            }

            return _shellExecutionManager.ExecuteCommand(commandText, timeout, out commandOutput);
        }

        public override void Close()
        {
            _isClosed = true;

            lock (_lock)
            {
                foreach (IRawShell rawShell in _shellList)
                {
                    rawShell.Dispose();
                }

                _shellList.Clear();
            }
        }

        protected IRawShell CreateShellFromSettings(IPipeTransportSettings settings, Connection outerConnection, bool isCommandShell = false)
        {
            IRawShell rawShell;
            if (_outerConnection == null)
            {
                if (isCommandShell)
                    rawShell = new CommandShell(settings.ExeCommand, settings.ExeCommandArgs);
                else
                    rawShell = new RawLocalShell(settings.ExeCommand, settings.ExeCommandArgs);
            }
            else
            {
                rawShell = new RawRemoteShell(settings.ExeCommand, settings.ExeCommandArgs, outerConnection);
            }

            LinkedListNode<IRawShell> node;
            lock (_lock)
            {
                node = _shellList.AddLast(rawShell);
            }

            rawShell.Closed += (sender, eventArgs) =>
            {
                if (_isClosed)
                    return;

                lock (_lock)
                {
                    if (_isClosed)
                        return;

                    if (node != null)
                    {
                        _shellList.Remove(node);
                        node = null;
                    }
                }
            };

            return rawShell;
        }

        public override void ExecuteSyncCommand(string commandDescription, string commandText, out string commandOutput, int timeout, out int exitCode)
        {
            throw new NotImplementedException();
        }

        public override string MakeDirectory(string path)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(path), nameof(path));

            if (!DoesCommandExist("mkdir"))
            {
                throw new CommandFailedException("mkdir");
            }

            string commandOutput;
            // -p ignores if the directory is already there
            string command = "mkdir -p \"" + path + "\"";

            // Create the directory
            if (ExecuteCommand(command, DefaultTimeout, out commandOutput) != 0)
            {
                throw new CommandFailedException(command);
            }

            return GetFullPath(path);
        }

        private string GetFullPath(string path)
        {
            string fullpath;
            string output;

            string pwd;
            if (ExecuteCommand("pwd", DefaultTimeout, out pwd) == 0
                && ExecuteCommand($"cd \"{path}\"; pwd", DefaultTimeout, out fullpath) == 0
                && ExecuteCommand($"cd \"{pwd}\"", DefaultTimeout, out output) == 0)
            {
                return fullpath;
            }

            throw new CommandFailedException("Unable to get FullPath");
        }

        public override string GetUserHomeDirectory()
        {
            string command = "echo $HOME";
            string commandOutput;
            if (ExecuteCommand(command, DefaultTimeout, out commandOutput) != 0)
            {
                throw new CommandFailedException(command);
            }

            return commandOutput.TrimEnd('\n', '\r');
        }

        public override bool IsOSX()
        {
            return false;
        }

        public override bool IsLinux()
        {
            string command = "uname";
            if (!DoesCommandExist(command))
            {
                return false;
            }
            string commandOutput;
            if (ExecuteCommand(command, DefaultTimeout, out commandOutput) != 0)
            {
                throw new CommandFailedException(command);
            }

            return commandOutput.StartsWith("Linux", StringComparison.OrdinalIgnoreCase);
        }

        public bool TryGetUsername(out string username)
        {
            username = string.Empty;
            string command = "id -u -n";
            if (!DoesCommandExist("id"))
            {
                Debug.Fail("Unable to locate command: 'id'");
                return false;
            }

            string commandOutput;
            int exitCode = ExecuteCommand(command, DefaultTimeout, out commandOutput);
            if (exitCode != 0)
            {
                Debug.Fail(string.Format(CultureInfo.InvariantCulture, "Command {0} failed with exit code: {1}", command, exitCode));
                return false;
            }

            username = commandOutput;
            return true;
        }

        public override List<Process> ListProcesses()
        {
            string username;
            TryGetUsername(out username);

            if (!DoesCommandExist("ps"))
            {
                throw new CommandFailedException(StringResources.Error_PSFailed);
            }

            string commandOutput;
            int exitCode = ExecuteCommand(PSOutputParser.CommandText, DefaultTimeout, out commandOutput);
            if (exitCode != 0)
            {
                exitCode = ExecuteCommand(PSOutputParser.AltCommandText, DefaultTimeout, out commandOutput);
                if (exitCode != 0)
                {
                    throw new CommandFailedException("Unable to get process list");
                }
            }

            return PSOutputParser.Parse(commandOutput, username);
        }
    }
}
