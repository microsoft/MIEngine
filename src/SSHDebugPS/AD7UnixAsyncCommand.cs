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
        private readonly IShellStream _shellStream;
        private readonly IDebugUnixShellCommandCallback _callback;
        private readonly LineBuffer _lineBuffer = new LineBuffer();
        private int _firedOnExit;
        private bool _beginReceived;
        private bool _isClosed;

        private string startCommand;

        public AD7UnixAsyncCommand(IShellStream shellStream, IDebugUnixShellCommandCallback callback)
        {
            _shellStream = shellStream;
            _callback = callback;
            Guid id = Guid.NewGuid();
            _beginMessage = string.Format("Begin:{0}", id);
            _exitMessagePrefix = string.Format("Exit:{0}-", id);
            _shellStream.OutputReceived += OnOutputReceived;
            _shellStream.Closed += OnClosed;
            _shellStream.ErrorOccured += OnError;
        }

        internal void Start(string commandText)
        {
            startCommand = string.Format("echo \"{0}\"; {1}; echo \"{2}$?\"", _beginMessage, commandText, _exitMessagePrefix);
            _shellStream.WriteLine(startCommand);
            _shellStream.Flush();
        }

        void IDebugUnixShellAsyncCommand.WriteLine(string text)
        {
            _shellStream.WriteLine(text);
            _shellStream.Flush();
        }

        void IDebugUnixShellAsyncCommand.Abort()
        {
            Close();
        }

        private void OnOutputReceived(object sender, string unorderedText)
        {
            // TODO: rajkumar42 The OutputReceived event in liblinux has some issues. If we stick with liblinux, we should fix it:
            // 1. It kicks off a different thread pool thread every time data is received. As such there is no
            // way to order the input.
            // 2. The shell in Linux keeps a queue of input. If a component only obtains the input through the
            // output received event, then nothing will ever drain the output.
            //
            // Suggested fix: Add a ReadLine async method to the shell and remove the output received event

            // TODO: rajkumar42, this breaks when logging in as root. 

            IEnumerable<string> linesToSend = null;

            lock (_lock)
            {
                string text = _shellStream.ReadToEnd();
                if (string.IsNullOrEmpty(text))
                    return;

                _lineBuffer.ProcessText(text, out linesToSend);
            }

            foreach (string line in linesToSend)
            {
                if (_isClosed)
                {
                    return;
                }

                if (line.Equals(startCommand, StringComparison.Ordinal))
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

        private void OnError(object sender)
        {
            if (Interlocked.CompareExchange(ref _firedOnExit, 1, 0) == 0)
            {
                _callback.OnExit(null);
            }

            Close();
        }

        private void OnClosed(object sender)
        {
            // TODO: When we implement ReadLineAsync this code should be able to go away
            if (_firedOnExit == 0 && _isClosed == false)
            {
                Thread.Sleep(200);
            }

            if (_firedOnExit == 0 && _isClosed == false)
            {
                Debug.Fail("Why was the SSH session closed?");
                OnError(sender);
            }
        }

        private void Close()
        {
            lock (_lock)
            {
                if (_isClosed)
                    return;

                _isClosed = true;
            }

            _shellStream.Close();
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