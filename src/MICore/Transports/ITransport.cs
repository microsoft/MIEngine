// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MICore
{
    public delegate void OnCommand(string cmd);

    public interface ITransport
    {
        void Init(ITransportCallback transportCallback, LaunchOptions options, Logger logger);
        void Send(string cmd);
        void Close();
        bool IsClosed { get; }

        /// <summary>
        /// Process ID of the debugger process (clrdbg/lldb/gdb).
        /// This value is only valid when using local launch options. When using non-local
        /// options this may throw (e.g., TcpTransport) or provide bogus data (e.g., PipeTransport).
        /// It is used to know whether to fake a response from the debugger
        /// acknowledging that it has exited.
        /// </summary>
        int DebuggerPid { get; }
    }
    public interface ISignalingTransport: ITransport
    {
        ManualResetEvent StartedEvent { get; }
    }


    /// <summary>
    /// Interface implemented by the Debugger class to recieve notifications from the transport
    /// </summary>
    public interface ITransportCallback
    {
        /// <summary>
        /// Fired when a line of text is sent by the MI debugger over stdout
        /// </summary>
        /// <param name="line">[Required] Line of text that the target program wrote</param>
        void OnStdOutLine(string line);

        /// <summary>
        /// Fired when a line of text is sent by the MI debugger (or plink.exe or similar
        /// program connecting us to the MI debugger) over stderr.
        /// 
        /// ***NOTE*** at least using plink, if the target debugger/shell script writes
        /// text on unix to stderr, it still shows up in stdout on the Windows side.
        /// </summary>
        /// <param name="line">[Required] Line of text that the target program wrote</param>
        void OnStdErrorLine(string line);

        /// <summary>
        /// Fired when either the target process exits or when the stdout stream is closed.
        /// </summary>
        /// <param name="exitCode">[Optional] exit code from the target process. null if unknown.</param>
        void OnDebuggerProcessExit(string exitCode);

        /// <summary>
        /// Appends a line of text to the initialization log which is dumped to the output
        /// window on a launch error.
        /// </summary>
        /// <param name="line">[Required] line of text to write</param>
        void AppendToInitializationLog(string line);

        /// <summary>
        /// Fired when the transport wishes to log a message to the debugger output window
        /// </summary>
        /// <param name="line">[Required] Line of text to be logged</param>
        void LogText(string line);
    };
}
