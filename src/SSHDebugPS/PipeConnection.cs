// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.SSHDebugPS.Utilities;

namespace Microsoft.SSHDebugPS
{
    /// <summary>
    /// Abstract base class for connections based on a pipe connection to a command line tool (docker.exe, wsl.exe, etc)
    /// </summary>
    internal abstract class PipeConnection : Connection
    {
        private readonly object _lock = new object();

        private Connection _outerConnection;
        private bool _isClosed;
        private readonly string _name;

        public override string Name => _name;

        protected Connection OuterConnection => _outerConnection;
        protected bool IsClosed => _isClosed;

        /// <summary>
        /// Create a new pipe connection object
        /// </summary>
        /// <param name="outerConnection">[Optional] the SSH connection (or maybe something else in future) used to connect to the target.</param>
        /// <param name="name">The full name of this connection</param>
        public PipeConnection(Connection outerConnection, string name)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(name));
            _name = name;
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

            string command = "mkdir -p '{0}'".FormatInvariantWithArgs(path); // -p ignores if the directory is already there
            ExecuteCommand(command, Timeout.Infinite);

            return GetFullPath(path);
        }

        private string GetFullPath(string path)
        {
            string pwd = ExecuteCommand("pwd", Timeout.Infinite);
            string fullpath = ExecuteCommand($"cd '{path}'; pwd", Timeout.Infinite);
            ExecuteCommand($"cd '{pwd}'", Timeout.Infinite);

            return fullpath;
        }

        public override string GetUserHomeDirectory()
        {
            string commandOutput = ExecuteCommand("echo $HOME", Timeout.Infinite);

            return commandOutput.TrimEnd('\n', '\r');
        }

        public override bool IsOSX()
        {
            return false;
        }

        public override bool IsLinux()
        {
            if (!ExecuteCommand("uname", Timeout.Infinite, commandOutput: out string commandOutput, errorMessage: out string errorMessage, out _))
            {
                return false;
            }
            return commandOutput.StartsWith("Linux", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Retrieves system information such as username and architecture
        /// </summary>
        /// <returns>SystemInformation containing username and architecture. If it was unable to obtain any of these, the value will be set to string.Empty.</returns>
        public SystemInformation GetSystemInformation()
        {
            string commandOutput;
            string errorMessage;
            int exitCode;

            string username = string.Empty;
            if (ExecuteCommand("id -u -n", Timeout.Infinite, commandOutput: out commandOutput, errorMessage: out errorMessage, exitCode: out exitCode))
            {
                username = commandOutput;
            }

            string architecture = string.Empty;
            if (ExecuteCommand("uname -m", Timeout.Infinite, commandOutput: out commandOutput, errorMessage: out errorMessage, exitCode: out exitCode))
            {
                architecture = commandOutput;
            }

            return new SystemInformation(username, architecture);
        }

        public override List<Process> ListProcesses()
        {
            SystemInformation systemInformation = GetSystemInformation();

            List<Process> processes;
            string psErrorMessage;
            // Try using 'ps' first
            if (!PSListProcess(systemInformation, out psErrorMessage, out processes))
            {
                string procErrorMessage;
                // try using the /proc file system
                if (!ProcFSListProcess(systemInformation, out procErrorMessage, out processes))
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
        private bool PSListProcess(SystemInformation systemInformation, out string errorMessage, out List<Process> processes)
        {
            errorMessage = string.Empty;
            string commandOutput;
            int exitCode;
            if (!ExecuteCommand(PSOutputParser.PSCommandLine, Timeout.Infinite, out commandOutput, out errorMessage, out exitCode))
            {
                // Clear output and errorMessage
                commandOutput = string.Empty;
                errorMessage = string.Empty;
                if (!ExecuteCommand(PSOutputParser.AltPSCommandLine, Timeout.Infinite, out commandOutput, out errorMessage, out exitCode))
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

            processes = PSOutputParser.Parse(commandOutput, systemInformation);
            return true;
        }

        protected virtual string ProcFSErrorMessage => StringResources.Error_ProcFSError;

        /// <summary>
        /// Query /proc for a list of processes
        /// </summary>
        private bool ProcFSListProcess(SystemInformation systemInformation, out string errorMessage, out List<Process> processes)
        {
            errorMessage = string.Empty;
            processes = null;

            int exitCode;
            string commandOutput;
            if (!ExecuteCommand(ProcFSOutputParser.CommandText, Timeout.Infinite, out commandOutput, out errorMessage, out exitCode))
            {
                errorMessage = ProcFSErrorMessage.FormatCurrentCultureWithArgs(errorMessage);
                return false;
            }

            processes = ProcFSOutputParser.Parse(commandOutput, systemInformation);
            return true;
        }
    }
}
