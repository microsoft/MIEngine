// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MICore
{
    public class LocalUnixTerminalTransport : StreamTransport
    {
        private string _dbgStdInName;
        private string _dbgStdOutName;
        private int _debuggerPid = -1;

        /// <summary>
        /// When the debugger exits, it will write into the fifo associated with this reader.
        /// This is useful in the case of attach when the root password is required, but the
        /// request is canceled. In that case, the debugger won't have started yet (so no messages
        /// will be processed from it), but the trap will execute and write into this fifo.
        /// </summary>
        private StreamReader _exitReader;

        public override void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer)
        {
            LocalLaunchOptions localOptions = (LocalLaunchOptions)options;

            // Default working directory is next to the app
            string debuggeeDir;
            if (Path.IsPathRooted(options.ExePath) && File.Exists(options.ExePath))
            {
                debuggeeDir = Path.GetDirectoryName(options.ExePath);
            }
            else
            {
                // If we don't know where the app is, default to HOME, and if we somehow can't get that, go with the root directory.
                debuggeeDir = Environment.GetEnvironmentVariable("HOME");
                if (string.IsNullOrEmpty(debuggeeDir))
                    debuggeeDir = "/";
            }

            _dbgStdInName = UnixUtilities.MakeFifo(Logger);
            _dbgStdOutName = UnixUtilities.MakeFifo(Logger);
            string pidFifo = UnixUtilities.MakeFifo(Logger);
            string exitFifo = UnixUtilities.MakeFifo(Logger);

            // Setup the streams on the fifos as soon as possible.
            FileStream dbgStdInStream = new FileStream(_dbgStdInName, FileMode.Open);
            FileStream dbgStdOutStream = new FileStream(_dbgStdOutName, FileMode.Open);
            FileStream pidStream = new FileStream(pidFifo, FileMode.Open);
            FileStream exitStream = new FileStream(exitFifo, FileMode.Open);

            string debuggerCmd = UnixUtilities.GetDebuggerCommand(localOptions);
            string launchDebuggerCommand = UnixUtilities.LaunchLocalDebuggerCommand(
                debuggeeDir,
                exitFifo,
                _dbgStdInName,
                _dbgStdOutName,
                pidFifo,
                debuggerCmd);

            TerminalLauncher terminal = TerminalLauncher.MakeTerminal("DebuggerTerminal", launchDebuggerCommand, localOptions.Environment);
            terminal.Launch(debuggeeDir);

            using (StreamReader pidReader = new StreamReader(pidStream, Encoding.UTF8, true, UnixUtilities.StreamBufferSize))
            {
                _debuggerPid = int.Parse(pidReader.ReadLine(), CultureInfo.InvariantCulture);
            }

            _exitReader = new StreamReader(exitStream, Encoding.UTF8, true, UnixUtilities.StreamBufferSize);
            Task<string> task = _exitReader.ReadLineAsync();
            task.ContinueWith(DebuggerExited, TaskContinuationOptions.OnlyOnRanToCompletion);

            // The in/out names are confusing in this case as they are relative to gdb.
            // What that means is the names are backwards wrt miengine hence the reader
            // being the writer and vice-versa
            // Mono seems to hang when the debugger sends a large response unless we specify a larger buffer here
            writer = new StreamWriter(dbgStdInStream, new UTF8Encoding(false, true), UnixUtilities.StreamBufferSize);
            reader = new StreamReader(dbgStdOutStream, Encoding.UTF8, true, UnixUtilities.StreamBufferSize);
        }

        private void DebuggerExited(Task<string> task)
        {
            if (task.Result == UnixUtilities.ExitString)
            {
                Logger?.WriteLine("Debugger exited, stop debugging");
                _exitReader?.Dispose();
                this.Callback.OnDebuggerProcessExit(null);
            }
        }

        public override int DebuggerPid
        {
            get
            {
                return _debuggerPid;
            }
        }

        protected override string GetThreadName()
        {
            return "MI.LocalUnixTerminalTransport";
        }

        public override void Close()
        {
            base.Close();

            // If we are shutting down before the _exitReader has read a line it's possible that
            // there is a thread blocked doing a read() syscall. 
            ForceDisposeStreamReader(_exitReader);
        }
    }
}
