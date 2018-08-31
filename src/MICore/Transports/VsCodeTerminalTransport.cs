// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DebugEngineHost;

namespace MICore
{
    public class VsCodeTerminalTransport : StreamTransport
    {
        private int _debuggerPid;
        private string _dbgStdInPipeName;
        private string _dbgStdOutPipeName;
        private string _pidPipeName;

        private string _dbgCmdScript;

        private FileStream _pidReader;

        private ProcessMonitor _shellProcessMonitor;
        private CancellationTokenSource _streamReadPidCancellationTokenSource = new CancellationTokenSource();

        public override int DebuggerPid
        {
            get
            {
                return _debuggerPid;
            }
        }


        public override void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer)
        {
            LocalLaunchOptions localOptions = options as LocalLaunchOptions;

            Encoding encNoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            Stream inStream;
            Stream outStream;

            if (PlatformUtilities.IsWindows())
            {
                NamedPipeServerStream inputToDebugger = new NamedPipeServerStream(_dbgStdInPipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte);
                NamedPipeServerStream outputFromDebugger = new NamedPipeServerStream(_dbgStdOutPipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte);
                NamedPipeServerStream pidPipe = new NamedPipeServerStream(_pidPipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte);

                inStream = inputToDebugger;
                outStream = outputFromDebugger;
            }
            else
            {
                // Do Linux
                _dbgStdInPipeName = UnixUtilities.MakeFifo(identifier: "In", logger: Logger);
                _dbgStdOutPipeName = UnixUtilities.MakeFifo(identifier: "Out", logger: Logger);
                _pidPipeName = UnixUtilities.MakeFifo(identifier: "Pid", logger: Logger);

                _dbgCmdScript = UnixUtilities.GetTemporaryFilename(identifier: "Cmd");

                // Create filestreams
                FileStream stdInStream = new FileStream(_dbgStdInPipeName, FileMode.Open);
                FileStream stdOutStream = new FileStream(_dbgStdOutPipeName, FileMode.Open);
                _pidReader = new FileStream(_pidPipeName, FileMode.Open);

                string debuggerCmd = UnixUtilities.GetDebuggerCommand(localOptions);

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

                string launchDebuggerCommand = UnixUtilities.LaunchLocalDebuggerCommand(
                    debuggeeDir,
                    _dbgStdInPipeName,
                    _dbgStdOutPipeName,
                    _pidPipeName,
                    _dbgCmdScript,
                    debuggerCmd,
                    localOptions.GetMiDebuggerArgs());

                using (FileStream dbgCmdStream = new FileStream(_dbgCmdScript, FileMode.CreateNew))
                using (StreamWriter dbgCmdWriter = new StreamWriter(dbgCmdStream, encNoBom) { AutoFlush = true })
                {
                    dbgCmdWriter.Write(launchDebuggerCommand);
                    dbgCmdWriter.Close();
                }

                VSCodeRunInTerminalLauncher launcher = new VSCodeRunInTerminalLauncher(Path.GetFileName(options.ExePath), _dbgCmdScript, localOptions.Environment);

                if (!launcher.Launch(
                        localOptions.UseExternalConsole,
                        LaunchSuccess,
                        (error) =>
                        {
                            Logger.WriteTextBlock("console error:", error);
                            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_RunInTerminalFailure, error));
                        },
                        Logger))
                {
                    throw new InvalidOperationException(MICoreResources.Error_RunInTerminalUnavailable);
                }

                outStream = stdOutStream;
                inStream = stdInStream;
            }

            // Mono seems to stop responding when the debugger sends a large response unless we specify a larger buffer here
            writer = new StreamWriter(inStream, encNoBom, UnixUtilities.StreamBufferSize);
            reader = new StreamReader(outStream, Encoding.UTF8, true, UnixUtilities.StreamBufferSize);
        }

        private Action<int> debuggerPidCallback;
        public void RegisterDebuggerPidCallback(Action<int> pidCallback)
        {
            debuggerPidCallback = pidCallback;
        }

        private void LaunchSuccess(int? pid)
        {
            using (StreamReader pidReader = new StreamReader(_pidReader, Encoding.UTF8, true, UnixUtilities.StreamBufferSize))
            {
                int shellPid;
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

            if (debuggerPidCallback != null)
            {
                debuggerPidCallback(_debuggerPid);
            }
        }

        private void ShellExited(object sender, EventArgs e)
        {
            _shellProcessMonitor.ProcessExited -= ShellExited;
            Logger?.WriteLine("Shell exited, stop debugging");
            this.Callback.OnDebuggerProcessExit(null);
        }

        public override void Init(ITransportCallback transportCallback, LaunchOptions options, Logger logger, HostWaitLoop waitLoop = null)
        {
            // await _waitForConnections;
            base.Init(transportCallback, options, logger, waitLoop);
        }

        public override void Close()
        {
            base.Close();

            _shellProcessMonitor?.Dispose();
            _streamReadPidCancellationTokenSource.Cancel();
            _streamReadPidCancellationTokenSource.Dispose();
        }

        protected override string GetThreadName()
        {
            return "MI.VsCodeTerminalTransport";
        }
        
        public override bool CanExecuteCommand()
        {
            return false;
        }

        public override int ExecuteSyncCommand(string commandDescription, string commandText, int timeout, out string output, out string error)
        {
            throw new NotImplementedException();
        }
    }
}
