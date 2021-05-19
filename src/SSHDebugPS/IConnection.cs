// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
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

    internal abstract class Connection : IConnection, IDebugUnixShellPort
    {
        #region IDebugUnixShellPort 
        public abstract void ExecuteSyncCommand(string commandDescription, string commandText, out string commandOutput, int timeout, out int exitCode);

        public abstract void BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand);

        public abstract void CopyFile(string sourcePath, string destinationPath);

        public abstract string MakeDirectory(string path);

        public abstract string GetUserHomeDirectory();

        public abstract bool IsOSX();

        public abstract bool IsLinux();
        #endregion

        #region IConnection
        public abstract string Name { get; }

        public abstract List<Process> ListProcesses();

        public abstract void Close();

        public abstract int ExecuteCommand(string commandText, int timeout, out string commandOutput, out string errorMessage);
        #endregion
    }

    public class Process
    {
        public uint Id { get; private set; }
        /// <summary>
        /// Only used by the PSOutputParser
        /// </summary>
        public uint Flags { get; private set; }
        public string SystemArch { get; private set; }
        public string CommandLine { get; private set; }
        public string UserName { get; private set; }
        public bool IsSameUser { get; private set; }

        public Process(uint id, string arch, uint flags, string userName, string commandLine, bool isSameUser)
        {
            this.Id = id;
            this.Flags = flags;
            this.UserName = userName;
            this.CommandLine = commandLine;
            this.IsSameUser = isSameUser;
            this.SystemArch = arch;
        }
    }

    internal class SystemInformation
    {
        public string UserName { get; private set; }
        public string Architecture { get; private set; }

        public SystemInformation(string username, string architecture)
        {
            this.UserName = username;
            this.Architecture = architecture;
        }
    }
}