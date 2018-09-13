// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DebugEngineHost;

namespace MICore
{
    public class VsCodeTerminalTransport : StreamTransport
    {
        private int _debuggerPid;
        private Stream _pidReader;

        private ProcessMonitor _shellProcessMonitor;
        private CancellationTokenSource _streamReadPidCancellationTokenSource = new CancellationTokenSource();
        private Task _waitForConnection = null;

        private Stream _commandStream = null;
        private Stream _outputStream = null;

        private Stream _errorStream = null;

        public override int DebuggerPid
        {
            get
            {
                return _debuggerPid;
            }
        }

        public override async void Init(ITransportCallback transportCallback, LaunchOptions options, Logger logger, HostWaitLoop waitLoop = null)
        {
            LocalLaunchOptions localOptions = options as LocalLaunchOptions;

            Encoding encNoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            string commandPipeName;
            string outputPipeName;
            string pidPipeName;
            List<string> cmdArgs = new List<string>();

            if (PlatformUtilities.IsWindows())
            {
                // Create Windows Named pipes
                commandPipeName = Utilities.GetMIEngineTemporaryFilename("In");
                outputPipeName = Utilities.GetMIEngineTemporaryFilename("Out");
                pidPipeName = Utilities.GetMIEngineTemporaryFilename("Pid");
                string errorPipeName = Utilities.GetMIEngineTemporaryFilename("Error");

                NamedPipeServerStream inputToDebugger = new NamedPipeServerStream(commandPipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte);
                NamedPipeServerStream outputFromDebugger = new NamedPipeServerStream(outputPipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte);
                NamedPipeServerStream errorFromDebugger = new NamedPipeServerStream(errorPipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte);
                NamedPipeServerStream pidPipe = new NamedPipeServerStream(pidPipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte);

                _pidReader = pidPipe;

                string thisModulePath = typeof(VsCodeTerminalTransport).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName;
                string launchCommand = Path.Combine(Path.GetDirectoryName(thisModulePath), "WindowsDebugLauncher.exe");

                if (!File.Exists(launchCommand))
                {
                    throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_InternalFileMissing, launchCommand));
                }

                cmdArgs.Add(launchCommand);
                cmdArgs.Add("--stdin=" + commandPipeName);
                cmdArgs.Add("--stdout=" + outputPipeName);
                cmdArgs.Add("--stderr=" + errorPipeName);
                cmdArgs.Add("--pid=" + pidPipeName);
                cmdArgs.Add("--dbgExe=" + localOptions.MIDebuggerPath);
                cmdArgs.Add(localOptions.GetMiDebuggerArgs());

                _waitForConnection = Task.WhenAll(
                        inputToDebugger.WaitForConnectionAsync(),
                        outputFromDebugger.WaitForConnectionAsync(),
                        errorFromDebugger.WaitForConnectionAsync(),
                        pidPipe.WaitForConnectionAsync());

                _commandStream = inputToDebugger;
                _outputStream = outputFromDebugger;
                _errorStream = errorFromDebugger;
            }
            else
            {
                // Do Linux style pipes
                commandPipeName = UnixUtilities.MakeFifo(identifier: "In", logger: logger);
                outputPipeName = UnixUtilities.MakeFifo(identifier: "Out", logger: logger);
                pidPipeName = UnixUtilities.MakeFifo(identifier: "Pid", logger: logger);

                // Create filestreams
                FileStream stdInStream = new FileStream(commandPipeName, FileMode.Open);
                FileStream stdOutStream = new FileStream(outputPipeName, FileMode.Open);
                _pidReader = new FileStream(pidPipeName, FileMode.Open);

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

                string dbgCmdScript = Path.Combine(Path.GetTempPath(), Utilities.GetMIEngineTemporaryFilename(identifier: "Cmd"));
                string launchDebuggerCommand = UnixUtilities.LaunchLocalDebuggerCommand(
                    debuggeeDir,
                    commandPipeName,
                    outputPipeName,
                    pidPipeName,
                    dbgCmdScript,
                    debuggerCmd,
                    localOptions.GetMiDebuggerArgs());

                logger?.WriteTextBlock("DbgCmd:", launchDebuggerCommand);

                using (FileStream dbgCmdStream = new FileStream(dbgCmdScript, FileMode.CreateNew))
                using (StreamWriter dbgCmdWriter = new StreamWriter(dbgCmdStream, encNoBom) { AutoFlush = true })
                {
                    dbgCmdWriter.Write(launchDebuggerCommand);
                    dbgCmdWriter.Flush();
                }

                if (PlatformUtilities.IsOSX())
                {
                    string thisModulePath = typeof(VsCodeTerminalTransport).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName;
                    string launchScript = Path.Combine(Path.GetDirectoryName(thisModulePath), "osxlaunchhelper.scpt");
                    if (!File.Exists(launchScript))
                    {
                        string message = string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_InternalFileMissing, launchScript);
                        throw new FileNotFoundException(message);
                    }

                    cmdArgs.Add("/usr/bin/osascript");
                    cmdArgs.Add(launchScript);
                    cmdArgs.Add(FormattableString.Invariant($"{Path.GetFileName(options.ExePath)}"));
                    cmdArgs.Add(FormattableString.Invariant($"sh {dbgCmdScript} ; "));
                }
                else
                {
                    cmdArgs.Add("sh");
                    cmdArgs.Add(dbgCmdScript);
                }

                _outputStream = stdOutStream;
                _commandStream = stdInStream;
            }

            VSCodeRunInTerminalLauncher launcher = new VSCodeRunInTerminalLauncher(
                Path.GetFileName(options.ExePath), 
                localOptions.Environment);

            if (!launcher.Launch(
                    cmdArgs,
                    localOptions.UseExternalConsole,
                    LaunchSuccess,
                    (error) =>
                    {
                        logger?.WriteTextBlock("console  error:", error);
                        throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_RunInTerminalFailure, error));
                    },
                    logger))
            {
                throw new InvalidOperationException(MICoreResources.Error_RunInTerminalUnavailable);
            }

            logger?.WriteLine("Wait for connection completion.");

            if (_waitForConnection != null)
            {
                await _waitForConnection;
            }

            base.Init(transportCallback, options, logger, waitLoop);
        }

        private void LogDebuggerErrors()
        {
            if (_errorStream != null)
            {
                StreamReader reader = new StreamReader(_errorStream, new UTF8Encoding(), false, 1024 * 4);

                while (!_streamReadPidCancellationTokenSource.IsCancellationRequested)
                {
                    string line = this.GetLineFromStream(reader);
                    Logger?.WriteTextBlock("dbgerr:", line);
                }
            }
        }

        public override void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer)
        {
            // Mono seems to stop responding when the debugger sends a large response unless we specify a larger buffer here
            writer = new StreamWriter(_commandStream, new UTF8Encoding(false), UnixUtilities.StreamBufferSize);
            reader = new StreamReader(_outputStream, new UTF8Encoding(false), false, UnixUtilities.StreamBufferSize);
        }

        private Action<int> debuggerPidCallback;
        public void RegisterDebuggerPidCallback(Action<int> pidCallback)
        {
            debuggerPidCallback = pidCallback;
        }

        private void LaunchSuccess(int? pid)
        {
            if (_pidReader != null)
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
                        throw new TimeoutException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_RunInTerminalFailure, MICoreResources.Error_TimeoutWaitingForConnection));
                    }

                    if (!PlatformUtilities.IsWindows())
                    {
                        _shellProcessMonitor = new ProcessMonitor(shellPid);
                        _shellProcessMonitor.ProcessExited += ShellExited;
                        _shellProcessMonitor.Start();
                    }
                    else
                    {
                        Process shellProcess = Process.GetProcessById(shellPid);
                        shellProcess.EnableRaisingEvents = true;
                        shellProcess.Exited += ShellExited;
                    }

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
                        throw new OperationCanceledException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_RunInTerminalFailure, MICoreResources.Error_UnableToEstablishConnectionToLauncher));
                    }
                }
            }

            if (debuggerPidCallback != null)
            {
                debuggerPidCallback(_debuggerPid);
            }
        }

        private void ShellExited(object sender, EventArgs e)
        {
            if (sender is ProcessMonitor)
            {
                ((ProcessMonitor)sender).ProcessExited -= ShellExited;
            }

            if (sender is Process)
            {
                ((Process)sender).Exited -= ShellExited;
            }

            Logger?.WriteLine("Shell exited, stop debugging");
            this.Callback.OnDebuggerProcessExit(null);

            Close();
        }

        public override void Close()
        {
            base.Close();

            _shellProcessMonitor?.Dispose();

            try
            {
                _commandStream?.Dispose();
                _outputStream?.Dispose();
                _errorStream?.Dispose();
                _pidReader?.Dispose();
            }
            catch
            { }
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
