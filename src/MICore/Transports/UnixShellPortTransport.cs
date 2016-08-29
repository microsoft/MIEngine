// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DebugEngineHost;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using System.Globalization;

namespace MICore
{
    /// <summary>
    /// Transport used when the debugger is started through IDebugUnixShellPort  (SSH, and possible other things in the future).
    /// </summary>
    public class UnixShellPortTransport : ITransport, IDebugUnixShellCommandCallback
    {
        private readonly object _closeLock = new object();
        private ITransportCallback _callback;
        private Logger _logger;
        private string _startRemoteDebuggerCommand;
        private IDebugUnixShellAsyncCommand _asyncCommand;
        private bool _bQuit;
        private bool _debuggerLaunched = false;
        private UnixShellPortLaunchOptions _launchOptions;

        private static string s_errorPrefix = "Error:";

        public UnixShellPortTransport()
        {
        }

        public void Init(ITransportCallback transportCallback, LaunchOptions options, Logger logger, HostWaitLoop waitLoop = null)
        {
            _launchOptions = (UnixShellPortLaunchOptions)options;
            _callback = transportCallback;
            _logger = logger;
            _startRemoteDebuggerCommand = _launchOptions.StartRemoteDebuggerCommand;

            waitLoop?.SetText(MICoreResources.Info_InstallingDebuggerOnRemote);

            _callback.AppendToInitializationLog("Starting unix command: " + _startRemoteDebuggerCommand);
            _launchOptions.UnixPort.BeginExecuteAsyncCommand(_startRemoteDebuggerCommand, this, out _asyncCommand);
        }

        public void Close()
        {
            lock (_closeLock)
            {
                if (_bQuit)
                    return;
                _bQuit = true;

                _asyncCommand.Abort();
            }
        }

        public void Send(string cmd)
        {
            _logger?.WriteLine("<-" + cmd);
            _logger?.Flush();
            _asyncCommand.WriteLine(cmd);
        }

        int ITransport.DebuggerPid
        {
            get
            {
                return 0; // this isn't known, and shouldn't be needed
            }
        }

        bool ITransport.IsClosed
        {
            get { return _bQuit; }
        }

        void IDebugUnixShellCommandCallback.OnOutputLine(string line)
        {
            if (!_debuggerLaunched)
            {
                if (_launchOptions.DebuggerMIMode != MIMode.Clrdbg)
                {
                    _debuggerLaunched = true;
                }
                else
                {
                    if (line != null && line.StartsWith(s_errorPrefix, System.StringComparison.OrdinalIgnoreCase))
                    {
                        _callback.OnStdErrorLine(line.Substring(s_errorPrefix.Length).Trim());
                    }
                    else
                    {
                        _callback.OnStdOutLine(line);
                    }

                    if (line.Equals("Info: Launching clrdbg"))
                    {
                        _debuggerLaunched = true;
                        UnixShellPortLaunchOptions.SetSuccessfulLaunch(_launchOptions);
                        return;
                    }
                }
            }

            _logger?.WriteLine("->" + line);
            _logger?.Flush();

            if (!string.IsNullOrEmpty(line))
            {
                _callback.OnStdOutLine(line);
            }
        }

        void IDebugUnixShellCommandCallback.OnExit(string exitCode)
        {
            if (!_bQuit)
            {
                _callback.AppendToInitializationLog(string.Format(CultureInfo.InvariantCulture, "{0} exited with code {1}.", _startRemoteDebuggerCommand, exitCode ?? "???"));

                _bQuit = true;
                try
                {
                    _callback.OnDebuggerProcessExit(exitCode);
                }
                catch
                {
                    // eat exceptions on this thread so we don't bring down VS
                }
            }
        }
    }
}
