// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using Microsoft.DebugEngineHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Microsoft.MIDebugEngine
{
    /// <summary>
    /// Handles launching and monitoring a target program in noDebug mode (Run without Debugging).
    /// The program is launched directly without a debugger (gdb/lldb). The MI transport, command
    /// factory, thread cache, and natvis are all bypassed. The existing AD7 event pipeline handles
    /// lifecycle: AD7ProgramCreateEvent triggers the DAP InitializedEvent, and OnProcessExit
    /// triggers ExitedEvent/TerminatedEvent.
    /// </summary>
    internal class NonDebugProcess
    {
        private readonly LaunchOptions _launchOptions;
        private readonly ISampleEngineCallback _callback;

        private Process _process;
        private int _exitFired;

        public NonDebugProcess(LaunchOptions launchOptions, ISampleEngineCallback callback)
        {
            _launchOptions = launchOptions ?? throw new ArgumentNullException(nameof(launchOptions));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        /// <summary>
        /// Launches the target program. Dispatches to the appropriate strategy based on
        /// the launch option type (local vs pipe transport) and host capabilities.
        /// </summary>
        public void Launch()
        {
            if (_launchOptions is PipeLaunchOptions pipeOptions)
            {
                LaunchViaPipe(pipeOptions);
            }
            else if (_launchOptions is LocalLaunchOptions localOptions)
            {
                LaunchLocal(localOptions);
            }
            else
            {
                // Defense-in-depth: LaunchOptions.GetInstance validates this earlier,
                // but guard here in case NonDebugProcess is constructed with an unexpected type.
                throw new InvalidOperationException(ResourceStrings.NoDebugUnsupportedTransport);
            }
        }

        /// <summary>
        /// Terminates the noDebug process. Safe to call multiple times — the exit callback
        /// is guarded by <see cref="_exitFired"/>.
        /// </summary>
        public void Terminate()
        {
            if (_process != null)
            {
                try { if (!_process.HasExited) _process.Kill(); } catch { }
            }

            uint exitCode = 0;
            try
            {
                if (_process != null)
                    exitCode = (uint)_process.ExitCode;
            }
            catch { }

            FireProcessExit(exitCode);
        }

        #region Launch strategies

        private void LaunchLocal(LocalLaunchOptions localOptions)
        {
            List<string> cmdArgs = new List<string> { _launchOptions.ExePath };
            if (_launchOptions.ExeArgumentList != null && _launchOptions.ExeArgumentList.Count > 0)
            {
                cmdArgs.AddRange(_launchOptions.ExeArgumentList);
            }
            else if (!string.IsNullOrEmpty(_launchOptions.ExeArguments))
            {
                // XML launch configs don't populate ExeArgumentList; fall back to the single joined string.
                cmdArgs.Add(_launchOptions.ExeArguments);
            }

            Dictionary<string, string> envVars = ToDictionary(_launchOptions.Environment);

            if (HostRunInTerminal.IsRunInTerminalAvailable())
            {
                LaunchInTerminal(
                    _launchOptions.WorkingDirectory ?? string.Empty,
                    localOptions.UseExternalConsole,
                    cmdArgs,
                    envVars);
            }
            else
            {
                StartProcess(new ProcessStartInfo
                {
                    FileName = _launchOptions.ExePath,
                    Arguments = _launchOptions.ExeArguments ?? string.Empty,
                    WorkingDirectory = _launchOptions.WorkingDirectory ?? string.Empty,
                }, envVars);
            }
        }

        private void LaunchViaPipe(PipeLaunchOptions pipeOptions)
        {
            string remoteCmd = BuildRemoteShellCommand();

            // Use RawPipeArguments (without the debugger command that PipeArguments includes)
            string rawPipeArgs = pipeOptions.RawPipeArguments ?? string.Empty;
            Dictionary<string, string> pipeEnvVars = ToDictionary(pipeOptions.PipeEnvironment);

            if (HostRunInTerminal.IsRunInTerminalAvailable())
            {
                List<string> cmdArgs = new List<string> { pipeOptions.PipePath };
                if (pipeOptions.RawPipeArgumentList != null)
                {
                    cmdArgs.AddRange(pipeOptions.RawPipeArgumentList);
                }
                cmdArgs.Add(remoteCmd);

                LaunchInTerminal(
                    pipeOptions.PipeCwd ?? string.Empty,
                    useExternalConsole: false,
                    cmdArgs,
                    pipeEnvVars);
            }
            else
            {
                string fullArgs = string.IsNullOrEmpty(rawPipeArgs)
                    ? remoteCmd
                    : rawPipeArgs + " " + remoteCmd;

                StartProcess(new ProcessStartInfo
                {
                    FileName = pipeOptions.PipePath,
                    Arguments = fullArgs,
                    WorkingDirectory = pipeOptions.PipeCwd ?? string.Empty,
                }, pipeEnvVars);
            }
        }

        /// <summary>
        /// Builds a shell command string for remote execution: "cd 'cwd' &amp;&amp; VAR=val 'program' args"
        /// </summary>
        private string BuildRemoteShellCommand()
        {
            string cmd = ShellEscapeArgument(_launchOptions.ExePath);

            if (!string.IsNullOrEmpty(_launchOptions.ExeArguments))
            {
                cmd += " " + _launchOptions.ExeArguments;
            }

            if (!string.IsNullOrEmpty(_launchOptions.WorkingDirectory))
            {
                cmd = FormattableString.Invariant($"cd {ShellEscapeArgument(_launchOptions.WorkingDirectory)} && {cmd}");
            }

            if (_launchOptions.Environment != null && _launchOptions.Environment.Count > 0)
            {
                string envPrefix = string.Join(" ", _launchOptions.Environment.Select(e =>
                {
                    ValidateEnvironmentVariableName(e.Name);
                    return FormattableString.Invariant($"{e.Name}={ShellEscapeArgument(e.Value)}");
                }));
                cmd = envPrefix + " " + cmd;
            }

            return cmd;
        }

        #endregion

        #region Process management

        /// <summary>
        /// Launches a program via the DAP RunInTerminal reverse request.
        /// The terminal owns the process; we monitor it for exit via the returned PID.
        /// </summary>
        private void LaunchInTerminal(string cwd, bool useExternalConsole, List<string> cmdArgs, Dictionary<string, string> envVars)
        {
            string title = FormattableString.Invariant($"cppdbg: {Path.GetFileName(_launchOptions.ExePath)}");
            HostRunInTerminal.RunInTerminal(title, cwd, useExternalConsole, cmdArgs, envVars,
                success: (pid) =>
                {
                    if (pid.HasValue)
                    {
                        MonitorProcess(pid.Value);
                    }
                    else
                    {
                        // Terminal did not return a PID — we cannot monitor process exit.
                        // This is valid per the DAP spec (processId is optional in the response).
                        // Without a PID we have no way to detect when the program exits, so
                        // fire exit immediately to avoid the session hanging indefinitely.
                        FireProcessExit(0);
                    }
                },
                failure: (error) =>
                {
                    FireProcessExit(1);
                });
        }

        /// <summary>
        /// Starts a child process directly, redirecting stdout/stderr to the debug output.
        /// </summary>
        private void StartProcess(ProcessStartInfo startInfo, Dictionary<string, string> envVars)
        {
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            foreach (var kvp in envVars)
            {
                startInfo.Environment[kvp.Key] = kvp.Value;
            }

            _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            _process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    _callback.OnOutputString(e.Data + Environment.NewLine);
            };
            _process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    _callback.OnOutputString(e.Data + Environment.NewLine);
            };
            _process.Exited += OnProcessExited;

            try
            {
                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
            }
            catch
            {
                try { if (!_process.HasExited) _process.Kill(); } catch { }
                _process.Dispose();
                _process = null;
                throw;
            }

            if (_process.HasExited)
            {
                OnProcessExited(_process, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Monitors an externally-launched process (from RunInTerminal) by PID.
        /// </summary>
        private void MonitorProcess(int pid)
        {
            try
            {
                Process process = Process.GetProcessById(pid);
                _process = process;
                process.EnableRaisingEvents = true;
                process.Exited += OnProcessExited;

                if (process.HasExited)
                {
                    OnProcessExited(process, EventArgs.Empty);
                }
            }
            catch (ArgumentException)
            {
                // Process already exited before we could attach
                FireProcessExit(0);
            }
        }

        /// <summary>
        /// Handles process exit. Guarded by Interlocked to prevent double-fire
        /// from both the Exited event and explicit HasExited checks.
        /// </summary>
        private void OnProcessExited(object sender, EventArgs e)
        {
            uint exitCode = 0;
            try
            {
                if (sender is Process p)
                    exitCode = (uint)p.ExitCode;
            }
            catch (InvalidOperationException) { }

            FireProcessExit(exitCode);
        }

        /// <summary>
        /// Central exit handler. Interlocked guard ensures the callback fires exactly once,
        /// even if Terminate(), OnProcessExited, and async callbacks race.
        /// </summary>
        private void FireProcessExit(uint exitCode)
        {
            if (Interlocked.Exchange(ref _exitFired, 1) != 0)
                return;

            try { _process?.Dispose(); } catch { }

            _callback.OnProcessExit(exitCode);
        }

        #endregion

        #region Utilities

        private static string ShellEscapeArgument(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return "''";
            return "'" + arg.Replace("'", "'\\''") + "'";
        }

        private static readonly Regex s_validEnvVarName = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

        /// <summary>
        /// Validates that an environment variable name is safe to embed in a shell command.
        /// Only allows POSIX-compliant names: letters, digits, and underscores, starting with a letter or underscore.
        /// </summary>
        private static void ValidateEnvironmentVariableName(string name)
        {
            if (string.IsNullOrEmpty(name) || !s_validEnvVarName.IsMatch(name))
            {
                throw new InvalidOperationException(
                    $"Environment variable name '{name}' contains invalid characters. " +
                    "Names must match [a-zA-Z_][a-zA-Z0-9_]* when used with pipe transport.");
            }
        }

        private static Dictionary<string, string> ToDictionary(IEnumerable<EnvironmentEntry> entries)
        {
            var dict = new Dictionary<string, string>();
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    dict[entry.Name] = entry.Value;
                }
            }
            return dict;
        }

        #endregion
    }
}
