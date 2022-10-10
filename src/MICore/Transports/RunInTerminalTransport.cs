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
    public class RunInTerminalTransport : StreamTransport
    {
        private int _debuggerPid;
        private StreamReader _pidReader;

        private ProcessMonitor _shellProcessMonitor;
        private CancellationTokenSource _streamReadPidCancellationTokenSource = new CancellationTokenSource();
        private Task _waitForConnection = null;

        private StreamWriter _commandStream = null;
        private StreamReader _outputStream = null;

        private StreamReader _errorStream = null;

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

            string windowtitle = FormattableString.Invariant($"cppdbg: {Path.GetFileName(options.ExePath)}");

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

                _pidReader = new StreamReader(pidPipe, encNoBom, false, UnixUtilities.StreamBufferSize);

                string thisModulePath = typeof(RunInTerminalTransport).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName;
                string launchCommand = Path.Combine(Path.GetDirectoryName(thisModulePath), "WindowsDebugLauncher.exe");

                if (!File.Exists(launchCommand))
                {
                    string errorMessage = string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_InternalFileMissing, launchCommand);
                    transportCallback.OnStdErrorLine(errorMessage);
                    transportCallback.OnDebuggerProcessExit(null);
                    return;
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

                _commandStream = new StreamWriter(inputToDebugger, encNoBom);
                _outputStream = new StreamReader(outputFromDebugger, encNoBom, false, UnixUtilities.StreamBufferSize);
                _errorStream = new StreamReader(errorFromDebugger, encNoBom, false, UnixUtilities.StreamBufferSize);
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
                _pidReader = new StreamReader(new FileStream(pidPipeName, FileMode.Open), encNoBom, false, UnixUtilities.StreamBufferSize);

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

                logger?.WriteTextBlock(LogLevel.Verbose, "DbgCmd:", launchDebuggerCommand);

                using (FileStream dbgCmdStream = new FileStream(dbgCmdScript, FileMode.CreateNew))
                using (StreamWriter dbgCmdWriter = new StreamWriter(dbgCmdStream, encNoBom) { AutoFlush = true })
                {
                    dbgCmdWriter.WriteLine("#!/usr/bin/env sh");
                    dbgCmdWriter.Write(launchDebuggerCommand);
                    dbgCmdWriter.Flush();
                }

                if (PlatformUtilities.IsOSX())
                {
                    string osxLaunchScript = GetOSXLaunchScript();

                    // Call osascript with a path to the AppleScript. The apple script takes 2 parameters: a title for the terminal and the launch script.
                    cmdArgs.Add("/usr/bin/osascript");
                    cmdArgs.Add(osxLaunchScript);
                    cmdArgs.Add(FormattableString.Invariant($"\"{windowtitle}\""));
                    cmdArgs.Add(FormattableString.Invariant($"sh {dbgCmdScript} ;")); // needs a semicolon because this command is running through the launchscript.
                }
                else
                {
                    cmdArgs.Add("/bin/sh");
                    cmdArgs.Add(dbgCmdScript);
                }

                _outputStream = new StreamReader(stdOutStream, encNoBom, false, UnixUtilities.StreamBufferSize);
                _commandStream = new StreamWriter(stdInStream, encNoBom);
            }

            // Do not pass the launchOptions Environment entries as those are used for the debuggee only.
            RunInTerminalLauncher launcher = new RunInTerminalLauncher(windowtitle, new List<EnvironmentEntry>(0).AsReadOnly());

            launcher.Launch(
                     cmdArgs,
                     localOptions.UseExternalConsole,
                     LaunchSuccess,
                     (error) =>
                     {
                         transportCallback.OnStdErrorLine(error);
                         throw new InvalidOperationException(error);
                     },
                     logger);
            logger?.WriteLine(LogLevel.Verbose, "Wait for connection completion.");

            if (_waitForConnection != null)
            {
                // Add a timeout for waiting for connection - 20 seconds
                Task waitOrTimeout = Task.WhenAny(_waitForConnection, Task.Delay(20000));
                await waitOrTimeout;
                if (waitOrTimeout.Status != TaskStatus.RanToCompletion)
                {
                    string errorMessage = String.Format(CultureInfo.CurrentCulture, MICoreResources.Error_DebuggerInitializeFailed_NoStdErr, "WindowsDebugLauncher.exe");
                    transportCallback.OnStdErrorLine(errorMessage);
                    transportCallback.OnDebuggerProcessExit(null);
                    return;
                }
            }

            base.Init(transportCallback, options, logger, waitLoop);
        }

        private static string GetOSXLaunchScript()
        {
            string thisModulePath = typeof(RunInTerminalTransport).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName;
            string launchScript = Path.Combine(Path.GetDirectoryName(thisModulePath), "osxlaunchhelper.scpt");
            if (!File.Exists(launchScript))
            {
                string message = string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_InternalFileMissing, launchScript);
                throw new FileNotFoundException(message);
            }

            return launchScript;
        }

        private void LogDebuggerErrors()
        {
            if (_errorStream != null)
            {
                while (!_streamReadPidCancellationTokenSource.IsCancellationRequested)
                {
                    string line = this.GetLineFromStream(_errorStream, _streamReadPidCancellationTokenSource.Token);
                    if (line == null)
                        break;
                    Logger?.WriteTextBlock(LogLevel.Error, "dbgerr:", line);
                }
            }
        }

        public override void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer)
        {
            // Mono seems to stop responding when the debugger sends a large response unless we specify a larger buffer here
            writer = _commandStream;
            reader = _outputStream;
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
                int shellPid;
                Task<string> readShellPidTask = _pidReader.ReadLineAsync();
                if (readShellPidTask.Wait(TimeSpan.FromSeconds(10)))
                {
                    shellPid = int.Parse(readShellPidTask.Result, CultureInfo.InvariantCulture);
                    // Used for testing
                    Logger?.WriteLine(LogLevel.Verbose, string.Concat("ShellPid=", shellPid));
                }
                else
                {
                    // Something is wrong because we didn't get the pid of shell
                    ForceDisposeStreamReader(_pidReader);
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

                Task<string> readDebuggerPidTask = _pidReader.ReadLineAsync();
                try
                {
                    readDebuggerPidTask.Wait(_streamReadPidCancellationTokenSource.Token);
                    _debuggerPid = int.Parse(readDebuggerPidTask.Result, CultureInfo.InvariantCulture);
                }
                catch (OperationCanceledException)
                {
                    // Something is wrong because we didn't get the pid of the debugger
                    ForceDisposeStreamReader(_pidReader);
                    Close();
                    throw new OperationCanceledException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_RunInTerminalFailure, MICoreResources.Error_UnableToEstablishConnectionToLauncher));
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

            Logger?.WriteLine(LogLevel.Verbose, "Shell exited, stop debugging");
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
                if (_outputStream != null)
                {
                    ForceDisposeStreamReader(_outputStream);
                }

                if (_errorStream != null)
                {
                    ForceDisposeStreamReader(_errorStream);
                }

                if (_pidReader != null)
                {
                    ForceDisposeStreamReader(_pidReader);
                }
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
