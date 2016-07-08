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
    [Guid("808CC6CA-45B1-47D5-9779-62BAA597BA50")]
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
        /// <param name="callback">Callback which will receive the output and events 
        /// from the command</param>
        /// <param name="asyncCommand">Returned command object</param>
        void BeginExecuteAsyncCommand(string commandText, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand);
    }

    /// <summary>
    /// Interface representing an executing asynchronous command. This is returned from 
    /// <see cref="IDebugUnixShellPort.BeginExecuteAsyncCommand(string, IDebugUnixShellCommandCallback, out IDebugUnixShellAsyncCommand)"/>.
    /// </summary>
    [ComImport()]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0BB77830-931D-4B54-822E-7BB56FA10049")]
    public interface IDebugUnixShellAsyncCommand
    {
        /// <summary>
        /// Writes to the standard input steam of the executing command
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
    /// <see cref="IDebugUnixShellPort.BeginExecuteAsyncCommand(string, IDebugUnixShellCommandCallback, out IDebugUnixShellAsyncCommand)"/>.
    /// </summary>
    [ComImport()]
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0CE7C176-1AF3-4DBD-BA4E-1BBDB242B190")]
    public interface IDebugUnixShellCommandCallback
    {
        /// <summary>
        /// Fired when a line of text is sent by the command
        /// </summary>
        void OnOutputLine(string line);

        /// <summary>
        /// Fired when the command finishes
        /// </summary>
        /// <param name="exitCode">The exit code of the command, assuming one was printed.</param>
        void OnExit(string exitCode);
    };
}
