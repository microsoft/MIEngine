// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS
{
    internal interface IConnection
    {
        string Name { get; }

        void Close();

        void BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand);

        int ExecuteCommand(string commandText, int timeout, out string commandOutput);

        List<Process> ListProcesses();
    }

    internal abstract class Connection : IConnection, IDebugUnixShellPort, IDebugUnixShellPort2
    {
        protected virtual int DefaultTimeout { get; set; }
        
        #region IDebugUnixShellPort 
        public abstract void ExecuteSyncCommand(string commandDescription, string commandText, out string commandOutput, int timeout, out int exitCode);

        public abstract void BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand);

        public abstract void CopyFile(string sourcePath, string destinationPath);

        public abstract string MakeDirectory(string path);

        public abstract string GetUserHomeDirectory();

        public abstract bool IsOSX();

        public abstract bool IsLinux();
        #endregion

        #region IDebugUnixShellPort2
        public abstract void CopyDirectory(string sourcePath, string destinationPath);
        #endregion

        #region IConnection
        public abstract string Name { get; }

        public abstract List<Process> ListProcesses();

        public abstract void Close();

        public abstract int ExecuteCommand(string commandText, int timeout, out string commandOutput);
        #endregion

        public bool DoesCommandExist(string command)
        {
            int exitCode = ExecuteCommand("which " + command, DefaultTimeout, out string commandOutput);

            if (exitCode != 0 || string.IsNullOrWhiteSpace(commandOutput))
                return false;

            return true;
        }
    }

    public class Process
    {
        public uint Id { get; private set; }
        public string CommandLine { get; private set; }
        public string UserName { get; private set; }
        public bool IsSameUser { get; private set; }

        public Process(uint id, string userName, string commandLine, bool isSameUser)
        {
            this.Id = id;
            this.UserName = userName;
            this.CommandLine = commandLine;
            this.IsSameUser = isSameUser;
        }
    }
}
