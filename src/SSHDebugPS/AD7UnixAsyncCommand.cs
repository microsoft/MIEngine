// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using liblinux.Shell;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.SSHDebugPS
{
    internal class AD7UnixAsyncCommand : IDebugUnixShellAsyncCommand
    {
        private readonly object _lock = new object();
        private readonly string _beginMessage;
        private readonly string _exitMessagePrefix;
        private IStreamingShell _streamingShell;
        private readonly IDebugUnixShellCommandCallback _callback;
        private readonly LineBuffer _lineBuffer = new LineBuffer();
        private int _firedOnExit;
        private bool _beginReceived;
        private bool _isClosed;

        private string _startCommand;

        public AD7UnixAsyncCommand(IStreamingShell streamingShell, IDebugUnixShellCommandCallback callback)
        {
            _streamingShell = streamingShell;
            _callback = callback;
            Guid id = Guid.NewGuid();
            _beginMessage = string.Format("Begin:{0}", id);
            _exitMessagePrefix = string.Format("Exit:{0}-", id);
            _streamingShell.OutputReceived += OnOutputReceived;
            _streamingShell.Closed += OnClosedOrDisconnected;
            _streamingShell.Disconnected += OnClosedOrDisconnected;
            _streamingShell.ErrorOccured += OnError;
        }

        internal void Start(string commandText)
        {
            _startCommand = string.Format("echo \"{0}\"; {1}; echo \"{2}$?\"", _beginMessage, commandText, _exitMessagePrefix);
            _streamingShell.WriteLine(_startCommand);
            _streamingShell.Flush();
            _streamingShell.BeginOutputRead();
        }

        void IDebugUnixShellAsyncCommand.WriteLine(string text)
        {
            lock (_lock)
            {
                if (_isClosed)
                {
                    return;
                }
            }

            _streamingShell.WriteLine(text);
            _streamingShell.Flush();
        }

        void IDebugUnixShellAsyncCommand.Abort()
        {
            Close();
        }

        private void OnOutputReceived(object sender, OutputReceivedEventArgs e)
        {
            IEnumerable<string> linesToSend = null;

            lock (_lock)
            {
                if (string.IsNullOrEmpty(e.Output))
                    return;

                _lineBuffer.ProcessText(e.Output, out linesToSend);
            }

            foreach (string line in linesToSend)
            {
                if (_isClosed)
                {
                    return;
                }

                if (line.Equals(_startCommand, StringComparison.Ordinal))
                {
                    // When logged in as root, shell sends a copy of stdin to stdout.
                    // This ignores the shell command that was used to launch the debugger.
                    continue;
                }

                int endCommandIndex = line.IndexOf(_exitMessagePrefix);
                if (endCommandIndex >= 0)
                {
                    if (Interlocked.CompareExchange(ref _firedOnExit, 1, 0) == 0)
                    {
                        string exitCode = SplitExitCode(line, endCommandIndex + _exitMessagePrefix.Length);
                        _callback.OnExit(exitCode);
                    }
                    Close();
                    return;
                }

                if (!_beginReceived)
                {
                    if (line.Contains(_beginMessage))
                    {
                        _beginReceived = true;
                    }
                    continue;
                }

                _callback.OnOutputLine(line);
            }
        }

        private void OnError(object sender, liblinux.ErrorOccuredEventArgs e)
        {
            if (Interlocked.CompareExchange(ref _firedOnExit, 1, 0) == 0)
            {
                _callback.OnExit(null);
            }

            Close();
        }

        private void OnClosedOrDisconnected(object sender, EventArgs e)
        {
            if (_firedOnExit == 0 && _isClosed == false)
            {
                Debug.Fail("Why was the SSH session closed?");
                OnError(sender, null);
            }
        }

        private void Close()
        {
            lock (_lock)
            {
                if (_isClosed)
                    return;

                _isClosed = true;

                try
                {
                    if (_streamingShell != null && _streamingShell.IsOpen)
                    {
                        _streamingShell.Close();
                    }

                    if (_streamingShell != null)
                    {
                        _streamingShell.Dispose();
                        _streamingShell = null;
                    }
                }
                catch (ThreadInterruptedException)
                {
                    // This can happen if the command failed on a timeout. Say dotnet restore failed because of insufficient permissions etc.
                }
            }
        }

        private static string SplitExitCode(string line, int startIndex)
        {
            string exitCode = line.Substring(startIndex);

            // If there was some extra cruft at the end of the line after the exit code, remove it
            if ((exitCode.Length > 0 && char.IsDigit(exitCode[0])) ||
                (exitCode.Length > 1 && exitCode[0] == '-' && char.IsDigit(exitCode[1])))
            {
                for (int c = 1; c < exitCode.Length; c++)
                {
                    if (!char.IsDigit(exitCode[c]))
                    {
                        return exitCode.Substring(0, c);
                    }
                }
            }

            return exitCode;
        }
    }
}