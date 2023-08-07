// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.DebugEngineHost;
using Microsoft.SSHDebugPS.Utilities;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS
{
    // Needs to be public to support the ContainerPicker UI
    public interface IConnection
    {
        string Name { get; }

        void Close();

        int ExecuteCommand(string commandText, int timeout, out string commandOutput, out string errorMessage);

        List<Process> ListProcesses();
    }

    internal abstract class Connection : IConnection
    {
        #region Methods for implementing IDebugUnixShellPort 

        /// <inheritdoc cref="IDebugUnixShellPort.BeginExecuteAsyncCommand(string, bool, IDebugUnixShellCommandCallback, out IDebugUnixShellAsyncCommand)"/>
        public abstract void BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand);

        /// <inheritdoc cref="IDebugUnixShellPort.CopyFile(string, string)"/>
        public abstract void CopyFile(string sourcePath, string destinationPath);

        /// <inheritdoc cref="IDebugUnixShellPort.MakeDirectory(string)"/>
        public abstract string MakeDirectory(string path);

        /// <inheritdoc cref="IDebugUnixShellPort.GetUserHomeDirectory"/>
        public abstract string GetUserHomeDirectory();

        /// <inheritdoc cref="IDebugUnixShellPort.IsOSX"/>
        public abstract bool IsOSX();

        /// <inheritdoc cref="IDebugUnixShellPort.IsLinux"/>
        public abstract bool IsLinux();
        #endregion

        #region IConnection
        public abstract string Name { get; }

        public abstract List<Process> ListProcesses();

        public abstract void Close();

        // TODO: This is wrong. It doesn't show UI but allows an infinite timeout

        /// <summary>
        /// Exceutes the specified command on the remote system
        /// </summary>
        /// <param name="commandText">The shell command text to execute</param>
        /// <param name="timeout">Timeout to wait for the command to complete before aborting</param>
        /// <param name="commandOutput">The stdout produced by the command</param>
        /// <param name="errorMessage">The stderr produced by the command</param>
        /// <returns>The exit code of the command</returns>
        public abstract int ExecuteCommand(string commandText, int timeout, out string commandOutput, out string errorMessage);
        #endregion

        /// <summary>
        /// Executes the specified command, throwing a CommandFailedException if it failed
        /// </summary>
        /// <param name="commandText">Text of the command to execute</param>
        /// <param name="timeout">timeout in milliseconds</param>
        /// <returns>The stdout text the command produced</returns>
        public string ExecuteCommand(string commandText, int timeout)
        {
            int exitCode = ExecuteCommand(commandText, timeout, out string commandOutput, out string errorMessage);
            if (exitCode != 0)
            {
                string error = StringResources.CommandFailedMessageFormat.FormatCurrentCultureWithArgs(commandText, exitCode, errorMessage);
                throw new CommandFailedException(error);
            }

            return commandOutput;
        }

        /// <summary>
        /// Executes the specified command, return false if the exit code is non-zero
        /// </summary>
        /// <param name="commandText">The shell command text to execute</param>
        /// <param name="timeout">Timeout to wait for the command to complete before aborting</param>
        /// <param name="commandOutput">The stdout produced by the command</param>
        /// <param name="errorMessage">The stderr produced by the command</param>
        /// <param name="exitCode">The exit code of the command</param>
        /// <returns>true if command succeeded.</returns>
        public bool ExecuteCommand(string commandText, int timeout, out string commandOutput, out string errorMessage, out int exitCode)
        {
            exitCode = ExecuteCommand(commandText, timeout, out commandOutput, out errorMessage);
            return exitCode == 0;
        }
    }

    public class Process
    {
        public uint Id { get; private set; }
        /// <summary>
        /// Only used by the PSOutputParser
        /// </summary>
        public uint? Flags { get; private set; }
        public string SystemArch { get; private set; }
        public string CommandLine { get; private set; }
        public string UserName { get; private set; }
        public bool IsSameUser { get; private set; }

        public Process(uint id, string arch, uint? flags, string userName, string commandLine, bool isSameUser)
        {
            this.Id = id;
            this.Flags = flags;
            this.UserName = userName;
            this.CommandLine = commandLine;
            this.IsSameUser = isSameUser;
            this.SystemArch = arch;
        }
    }

    internal static class OperatingSystemStringConverter
    {
        internal static PlatformID ConvertToPlatformID(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = value.ToLowerInvariant();
                if (value.Contains("darwin"))
                {
                    return PlatformID.MacOSX;
                } else if (value.Contains("linux"))
                {
                    return PlatformID.Unix;
                }
            }
            Debug.Fail($"Expected a valid platform '{value}' of darwin or linux, but falling back to linux.");
            return PlatformID.Unix;
        }
    }

    internal class SystemInformation
    {
        public string UserName { get; private set; }
        public string Architecture { get; private set; }
        public PlatformID Platform { get; private set; }

        public SystemInformation(string username, string architecture, PlatformID platform)
        {
            this.UserName = username;
            this.Architecture = architecture;
            Platform = platform;
        }
    }
}