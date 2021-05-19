// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Debugger.Interop;

namespace Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier
{
    /// <summary>
    /// Interface implemented by the IDebugPort2 object from the SSH port supplier or 
    /// any other port supplier that wants to support debugging via remote command 
    /// execution and standard I/O redirection.
    /// </summary>
    [ComImport()]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("5FE438B2-46BA-4637-88B3-E7B908D17331")]
    public interface IDebugUnixShellPort
    {
        /// <summary>
        /// Synchronously executes the specified shell command and returns the output and exit code
        /// of the command.
        /// </summary>
        /// <param name="commandDescription">Description of the command to use in a wait 
        /// dialog if command takes a long time to execute</param>
        /// <param name="commandText">Command line to execute on the remote system</param>
        /// <param name="commandOutput">Stdout/err which the command writes</param>
        /// <param name="timeout">timeout before the command should be aborted</param>
        /// <param name="exitCode">exit code of the command</param>
        void ExecuteSyncCommand(string commandDescription, string commandText, out string commandOutput, int timeout, out int exitCode);

        /// <summary>
        /// Starts the execution of the specified command, using call back interfaces to 
        /// receive its output, and using a command interface to send it input or abort 
        /// it.
        /// </summary>
        /// <param name="commandText">Text of the command to execut</param>
        /// <param name="runInShell">True if the command should be executed in a shell and PTY. It is important to note that callers should pass
        /// True unless their callback implementation is capable of handling raw output text, as <see cref="IDebugUnixShellCommandCallback.OnOutputLine(string)"/>
        /// will be sent unbuffered output when this is false, so the implementation must be able to handle partial lines or multiple lines in
        /// one call to OnOutputLine.</param>
        /// <param name="callback">Callback which will receive the output and events 
        /// from the command</param>
        /// <param name="asyncCommand">Returned command object</param>
        void BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand);

        /// <summary>
        /// Copy a single file from the local machine to the remote machine.
        /// </summary>
        /// <param name="sourcePath">File on the local machine.</param>
        /// <param name="destinationPath">Destination path on the remote machine.</param>
        void CopyFile(string sourcePath, string destinationPath);

        /// <summary>
        /// Creates directory provided the path. Does not fail if the directory already exists.
        /// </summary>
        /// <param name="path">Path on the remote machine.</param>
        /// <returns>Full path of the created directory.</returns>
        string MakeDirectory(string path);

        /// <summary>
        /// Gets the home directory of the user.
        /// </summary>
        /// <returns>Home directory of the user.</returns>
        string GetUserHomeDirectory();

       /// <returns>True if the remote machine is OSX.</returns>
        bool IsOSX();

        /// <returns>True if the remote machine is Linux.</returns>
        bool IsLinux();
    }

    /// <summary>
    /// Interface implemented by a port that supports explicit cleanup
    /// </summary>
    [ComImport()]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("1ECAAA80-36DB-4DA8-88B3-B298B0221BF6")]
    public interface IDebugPortCleanup
    {
        /// <summary>
        /// Clean up debugging resources
        /// </summary>
        void Clean();
    }

    /// <summary>
    /// Interface implemented by an IDebugPort2 that supports using gdbserver to attach to a remote process
    /// </summary>
    [ComImport()]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("C517EE50-4852-4D95-916E-1A1C89710466")]
    public interface IDebugGdbServerAttach
    {
        /// <summary>
        /// Attaches gdbserver to a process.
        /// </summary>
        /// <param name="processId">Id of the process.</param>
        /// <param name="preAttachCommand">Command to run before starting gdbserver.</param>
        /// <returns>Communications addr:port</returns>
        string GdbServerAttachProcess(int processId, string preAttachCommand);
   }

    /// <summary>
    /// Interface representing an executing asynchronous command. This is returned from 
    /// <see cref="IDebugUnixShellPort.BeginExecuteAsyncCommand(string, bool, IDebugUnixShellCommandCallback, out IDebugUnixShellAsyncCommand)"/>.
    /// </summary>
    [ComImport()]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0BB77830-931D-4B54-822E-7BB56FA10049")]
    public interface IDebugUnixShellAsyncCommand
    {
        /// <summary>
        /// Writes to the standard input stream of the executing command
        /// </summary>
        /// <param name="text">Text to send to the command</param>
        void Write(string text);

        /// <summary>
        /// Writes to the standard input steam of the executing command, appending a newline
        /// </summary>
        /// <param name="text">Text to send to the command</param>
        void WriteLine(string text);

        /// <summary>
        /// Aborts the executing command
        /// </summary>
        void Abort();
    }

    /// <summary>
    /// Interface to receive events from an executing async command. This is passed to 
    /// <see cref="IDebugUnixShellPort.BeginExecuteAsyncCommand(string, bool, IDebugUnixShellCommandCallback, out IDebugUnixShellAsyncCommand)"/>.
    /// </summary>
    [ComImport()]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0CE7C176-1AF3-4DBD-BA4E-1BBDB242B190")]
    public interface IDebugUnixShellCommandCallback
    {
        /// <summary>
        /// Notification fired when the command has output available
        /// </summary>
        /// <param name="line">Text that the executing command wrote to standard out or error.
        /// <para>If `false` is passed for `runInShell` when <see cref="IDebugUnixShellPort.BeginExecuteAsyncCommand(string, bool, IDebugUnixShellCommandCallback, out IDebugUnixShellAsyncCommand)"/>
        /// was called then this text will NOT be buffered, so either partial lines or multiple lines are possible, and this text will include new line
        /// characters.
        /// </para>
        /// <para>
        /// If `true` is passed for `runInShell` then this will be a single line of output with new line characters removed.
        /// </para>
        /// </param>
        void OnOutputLine(string line);

        /// <summary>
        /// Fired when the command finishes
        /// </summary>
        /// <param name="exitCode">The exit code of the command, assuming one was printed.</param>
        void OnExit(string exitCode);
    };

    /// <summary>
    /// Interface to query unix process architecture information.
    /// </summary>
    [ComImport()]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("61D4C2C4-7CA8-4472-8446-7B1DF5C9D16D")]
    public interface IDebugUnixProcess
    {
        /// <summary>
        /// Gets the architecture unix process.
        /// </summary>
        /// <returns>The string representation of the process's architecture. The format of this is architecture-specific and will match that returned by uname -m. Example: 'x86_64'</returns>
        string GetProcessArchitecture();
    };
}
