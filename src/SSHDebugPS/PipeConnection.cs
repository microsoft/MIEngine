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
using System.Text;

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

            string errorMessage;
            string commandOutput;
            string command = "mkdir -p '{0}'".FormatInvariantWithArgs(path); // -p ignores if the directory is already there
            ExecuteCommand(command, Timeout.Infinite, throwOnFailure: true, commandOutput: out commandOutput, errorMessage: out errorMessage);

            return GetFullPath(path);
        }

        private string GetFullPath(string path)
        {
            string fullpath;
            string output; // throw away variable

            string pwd;
            string errorMessage;
            ExecuteCommand("pwd", Timeout.Infinite, throwOnFailure: true, commandOutput: out pwd, errorMessage: out errorMessage);
            ExecuteCommand($"cd '{path}'; pwd", Timeout.Infinite, throwOnFailure: true, commandOutput: out fullpath, errorMessage: out errorMessage);
            ExecuteCommand($"cd '{pwd}'", Timeout.Infinite, throwOnFailure: false, commandOutput: out output, errorMessage: out errorMessage); //This might fail in some instances, so ignore a failure and ignore output

            return fullpath;
        }

        public override string GetUserHomeDirectory()
        {
            string command = "echo $HOME";
            string commandOutput;
            string errorMessage;
            ExecuteCommand(command, Timeout.Infinite, throwOnFailure: true, commandOutput: out commandOutput, errorMessage: out errorMessage);

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
            string errorMessage;
            if (!ExecuteCommand(command, Timeout.Infinite, throwOnFailure: false, commandOutput: out commandOutput, errorMessage: out errorMessage))
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
            string errorMessage;
            if (ExecuteCommand(command, Timeout.Infinite, throwOnFailure: false, commandOutput: out commandOutput, errorMessage: out errorMessage))
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

            List<Process> processes;
            string psErrorMessage;
            // Try using 'ps' first
            if (!PSListProcess(username, out psErrorMessage, out processes))
            {
                string procErrorMessage;
                // try using the /proc file system
                if (!ProcFSListProcess(username, out procErrorMessage, out processes))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(psErrorMessage);
                    sb.AppendLine(string.Empty);
                    sb.AppendLine(procErrorMessage);

                    VSMessageBoxHelper.PostErrorMessage(StringResources.Error_ProcessListFailedTitle, sb.ToString());

                    return new List<Process>(0);
                }
            }

            return processes;
        }

        /// <summary>
        /// Query 'ps' command for a list of processes
        /// </summary>
        private bool PSListProcess(string username, out string errorMessage, out List<Process> processes)
        {
            errorMessage = string.Empty;
            string commandOutput;
            int exitCode;
            if (!ExecuteCommand(PSOutputParser.PSCommandLine, Timeout.Infinite, false, out commandOutput, out errorMessage))
            {
                // Clear output and errorMessage
                commandOutput = string.Empty;
                errorMessage = string.Empty;
                if (!ExecuteCommand(PSOutputParser.AltPSCommandLine, Timeout.Infinite, false, out commandOutput, out errorMessage, out exitCode))
                {
                    if (exitCode == 127)
                    {
                        //command doesn't Exist
                        errorMessage = StringResources.Error_PSMissing;
                    }
                    else
                    {
                        errorMessage = StringResources.Error_PSErrorFormat.FormatCurrentCultureWithArgs(exitCode, errorMessage);
                    }

                    processes = null;
                    return false;
                }
            }

            processes = PSOutputParser.Parse(commandOutput, username);
            return true;
        }

        /// <summary>
        /// Query /proc for a list of processes
        /// </summary>
        private bool ProcFSListProcess(string username, out string errorMessage, out List<Process> processes)
        {
            errorMessage = string.Empty;
            processes = null;

            int exitCode;
            string commandOutput;
            // For a remote connection, the command will be passing through another instances of Linux, so we escape the text.
            string command = this.OuterConnection == null ?
                ProcFSOutputParser.CommandText :
                ProcFSOutputParser.EscapedCommandText;
            if (!ExecuteCommand(command, Timeout.Infinite, false, out commandOutput, out errorMessage, out exitCode))
            {
                errorMessage = StringResources.Error_ProcFSError.FormatCurrentCultureWithArgs(errorMessage);
                return false;
            }

            processes = ProcFSOutputParser.Parse(commandOutput, username);
            return true;
        }

        /// <summary>
        /// Checks command exit code and if it is non-zero, it will throw a CommandFailedException with an error message if 'throwOnFailure' is true.
        /// </summary>
        /// <returns>true if command succeeded.</returns>
        protected bool ExecuteCommand(string command, int timeout, bool throwOnFailure, out string commandOutput, out string errorMessage, out int exitCode)
        {
            commandOutput = string.Empty;

            exitCode = ExecuteCommand(command, timeout, out commandOutput, out errorMessage);
            if (throwOnFailure && exitCode != 0)
            {
                string error = StringResources.CommandFailedMessageFormat.FormatCurrentCultureWithArgs(command, exitCode, errorMessage);
                throw new CommandFailedException(error);
            }
            else
                return exitCode == 0;
        }

        protected bool ExecuteCommand(string command, int timeout, bool throwOnFailure, out string commandOutput, out string errorMessage)
        {
            int exitCode;
            return ExecuteCommand(command, timeout, throwOnFailure, out commandOutput, out errorMessage, out exitCode);
        }
    }
}
