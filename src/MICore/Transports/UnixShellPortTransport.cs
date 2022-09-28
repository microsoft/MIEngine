// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DebugEngineHost;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

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

        private const string ErrorPrefix = "Error:";

        private class KillCommandCallback: IDebugUnixShellCommandCallback
        {
            private readonly Logger _logger;
            public KillCommandCallback(Logger logger)
            {
                this._logger = logger;
            }

            public void OnOutputLine(string line)
            {
                _logger?.WriteLine(LogLevel.Verbose, "[kill] ->" + line);
            }

            public void OnExit(string exitCode)
            {
            }
        }

        public UnixShellPortTransport()
        {
        }

        public void Init(ITransportCallback transportCallback, LaunchOptions options, Logger logger, HostWaitLoop waitLoop = null)
        {
            _launchOptions = (UnixShellPortLaunchOptions)options;
            _callback = transportCallback;
            _logger = logger;
            _startRemoteDebuggerCommand = _launchOptions.StartRemoteDebuggerCommand;

            _callback.AppendToInitializationLog(string.Format(CultureInfo.CurrentCulture, MICoreResources.Info_StartingUnixCommand, _startRemoteDebuggerCommand));
            _launchOptions.UnixPort.BeginExecuteAsyncCommand(_startRemoteDebuggerCommand, runInShell: true, this, out _asyncCommand);
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
            _logger?.WriteLine(LogLevel.Verbose, "<-" + cmd);
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
                _debuggerLaunched = true;
            }

            if (!string.IsNullOrEmpty(line))
            {
                _callback.OnStdOutLine(line);
            }

            _logger?.WriteLine(LogLevel.Verbose, "->" + line);
            _logger?.Flush();
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

        public int ExecuteSyncCommand(string commandDescription, string commandText, int timeout, out string output, out string error)
        {
            int errorCode = -1;
            error = null; // In SSH transport, stderr is printed on stdout.
            _launchOptions.UnixPort.ExecuteSyncCommand(commandDescription, commandText, out output, timeout, out errorCode);
            return errorCode;
        }

        public bool CanExecuteCommand()
        {
            return true;
        }

        public bool Interrupt(int pid)
        {
            string killCmd = string.Format(CultureInfo.InvariantCulture, "/bin/sh -c \"kill -5 {0}\"", pid);

            try
            {
                IDebugUnixShellAsyncCommand asyncCommand;
                KillCommandCallback callbacks = new KillCommandCallback(_logger);
                _launchOptions.UnixPort.BeginExecuteAsyncCommand(killCmd, runInShell: true, callbacks, out asyncCommand);
            }
            catch (Exception e)
            {
                this._callback.OnStdErrorLine(string.Format(CultureInfo.InvariantCulture, MICoreResources.Warn_ProcessException, killCmd, e.Message));
                return false;
            }

            return true;
        }
    }
}
