// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.SSHDebugPS.Utilities;
using System.Windows.Documents;

namespace Microsoft.SSHDebugPS
{
    internal abstract class PipeConnection : Connection
    {
        private readonly object _lock = new object();
        private readonly IPipeTransportSettings _settings;

        private Connection _outerConnection;
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
        }

        public override void Close()
        {
            _isClosed = true;
            if (_outerConnection != null)
            {
                _outerConnection.Close();
                _outerConnection = null;
            }
        }

        public override string MakeDirectory(string path)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(path), nameof(path));
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string commandOutput;
            string command = "mkdir -p '{0}'".FormatInvariantWithArgs(path); // -p ignores if the directory is already there
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
            ExecuteCommand($"cd '{pwd}'", Timeout.Infinite, throwOnFailure: false, commandOutput: out output); //This might fail in some instances, so ignore a failure and ignore output

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
            if (!ExecuteCommand(command, Timeout.Infinite, throwOnFailure: false, commandOutput: out commandOutput))
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
            int exitCode;

            if (!ExecuteCommand(PSOutputParser.PSCommandLine, Timeout.Infinite, false, out commandOutput))
            {
                commandOutput = string.Empty;
                if (!ExecuteCommand(PSOutputParser.AltPSCommandLine, Timeout.Infinite, false, out commandOutput, out exitCode))
                {
                    string message;
                    if (exitCode == 127)
                    {
                        //command doesn't Exist
                        message = StringResources.Error_PSMissing;
                    }
                    else
                    {
                        message = StringResources.Error_PSErrorFormat.FormatCurrentCultureWithArgs(exitCode, commandOutput);
                    }

                    if (!string.IsNullOrWhiteSpace(commandOutput))
                    {
                        message = "{0} Output: {1}".FormatInvariantWithArgs(message, commandOutput);
                    }

                    VSMessageBoxHelper.ShowErrorMessage(StringResources.Error_CommandFailed, message);

                    return new List<Process>(0);
                }
            }

            return PSOutputParser.Parse(commandOutput, username);
        }

        /// <summary>
        /// Checks command exit code and if it is non-zero, it will throw a CommandFailedException with an error message if 'throwOnFailure' is true.
        /// </summary>
        /// <returns>true if command succeeded.</returns>
        protected bool ExecuteCommand(string command, int timeout, bool throwOnFailure, out string commandOutput, out int exitCode)
        {
            commandOutput = string.Empty;
            exitCode = ExecuteCommand(command, timeout, out commandOutput);
            if (throwOnFailure && exitCode != 0)
            {
                string error = String.Format(CultureInfo.InvariantCulture, StringResources.CommandFailedMessageFormat, command, exitCode, commandOutput);
                throw new CommandFailedException(error);
            }
            else
                return exitCode == 0;
        }

        protected bool ExecuteCommand(string command, int timeout, bool throwOnFailure, out string commandOutput)
        {
            int exitCode;
            return ExecuteCommand(command, timeout, throwOnFailure, out commandOutput, out exitCode);
        }
    }
}
