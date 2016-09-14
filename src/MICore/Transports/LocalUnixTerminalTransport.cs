// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MICore
{
    public class LocalUnixTerminalTransport : StreamTransport
    {
        private string _dbgStdInName;
        private string _dbgStdOutName;
        private int _debuggerPid = -1;
        private ProcessMonitor _shellProcessMonitor;
        private CancellationTokenSource _streamReadPidCancellationTokenSource = new CancellationTokenSource();

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

            // Used for testing
            Logger?.WriteLine(string.Concat("TempFile=", _dbgStdInName));
            Logger?.WriteLine(string.Concat("TempFile=", _dbgStdOutName));
            Logger?.WriteLine(string.Concat("TempFile=", pidFifo));

            // Setup the streams on the fifos as soon as possible.
            FileStream dbgStdInStream = new FileStream(_dbgStdInName, FileMode.Open);
            FileStream dbgStdOutStream = new FileStream(_dbgStdOutName, FileMode.Open);
            FileStream pidStream = new FileStream(pidFifo, FileMode.Open);

            string debuggerCmd = UnixUtilities.GetDebuggerCommand(localOptions);
            string launchDebuggerCommand = UnixUtilities.LaunchLocalDebuggerCommand(
                debuggeeDir,
                _dbgStdInName,
                _dbgStdOutName,
                pidFifo,
                debuggerCmd);

            // Only pass the environment to launch clrdbg. For other modes, there are commands that set the environment variables
            // directly for the debuggee.
            ReadOnlyCollection<EnvironmentEntry> environmentForDebugger = localOptions.DebuggerMIMode == MIMode.Clrdbg ?
                localOptions.Environment :
                new ReadOnlyCollection<EnvironmentEntry>(new EnvironmentEntry[] { }); ;

            TerminalLauncher terminal = TerminalLauncher.MakeTerminal("DebuggerTerminal", launchDebuggerCommand, localOptions.Environment);
            terminal.Launch(debuggeeDir);

            int shellPid = -1;

            using (StreamReader pidReader = new StreamReader(pidStream, Encoding.UTF8, true, UnixUtilities.StreamBufferSize))
            {
                Task<string> readShellPidTask = pidReader.ReadLineAsync();
                if (readShellPidTask.Wait(TimeSpan.FromSeconds(10)))
                {
                    shellPid = int.Parse(readShellPidTask.Result, CultureInfo.InvariantCulture);
                    // Used for testing
                    Logger?.WriteLine(string.Concat("ShellPid=", shellPid));
                }
                else
                {
                    // Something is wrong because we didn't get the pid of shell
                    ForceDisposeStreamReader(pidReader);
                    Close();
                    throw new TimeoutException(MICoreResources.Error_LocalUnixTerminalDebuggerInitializationFailed);
                }

                _shellProcessMonitor = new ProcessMonitor(shellPid);
                _shellProcessMonitor.ProcessExited += ShellExited;
                _shellProcessMonitor.Start();

                Task<string> readDebuggerPidTask = pidReader.ReadLineAsync();
                try
                {
                    readDebuggerPidTask.Wait(_streamReadPidCancellationTokenSource.Token);
                    _debuggerPid = int.Parse(readDebuggerPidTask.Result, CultureInfo.InvariantCulture);
                }
                catch (OperationCanceledException)
                {
                    // Something is wrong because we didn't get the pid of the debugger
                    ForceDisposeStreamReader(pidReader);
                    Close();
                    throw new OperationCanceledException(MICoreResources.Error_LocalUnixTerminalDebuggerInitializationFailed);
                }
            }

            // The in/out names are confusing in this case as they are relative to gdb.
            // What that means is the names are backwards wrt miengine hence the reader
            // being the writer and vice-versa
            // Mono seems to hang when the debugger sends a large response unless we specify a larger buffer here
            writer = new StreamWriter(dbgStdInStream, new UTF8Encoding(false, true), UnixUtilities.StreamBufferSize);
            reader = new StreamReader(dbgStdOutStream, Encoding.UTF8, true, UnixUtilities.StreamBufferSize);
        }

        private void ShellExited(object sender, EventArgs e)
        {
            _shellProcessMonitor.ProcessExited -= ShellExited;
            Logger?.WriteLine("Shell exited, stop debugging");
            this.Callback.OnDebuggerProcessExit(null);
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

            _shellProcessMonitor?.Dispose();
            _streamReadPidCancellationTokenSource.Cancel();
            _streamReadPidCancellationTokenSource.Dispose();
        }
    }
}
