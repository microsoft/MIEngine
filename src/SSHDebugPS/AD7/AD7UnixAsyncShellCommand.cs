// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS
{
    internal class AD7UnixAsyncShellCommand : AD7UnixAsyncCommand
    {
        private readonly object _lock = new object();
        private readonly string _beginMessage;
        private readonly string _exitMessagePrefix;
        private int _firedOnExit;
        private int _bClosed = 0;
        private bool _beginReceived;
        private string _startCommand;
        private LineBuffer _lineBuffer = new LineBuffer();

        public AD7UnixAsyncShellCommand(IRawShell shell, IDebugUnixShellCommandCallback callback, bool closeShellOnComplete)
            : base(shell, callback, closeShellOnComplete)
        {
            Guid id = Guid.NewGuid();
            _beginMessage = string.Format(CultureInfo.InvariantCulture, "Begin:{0}", id);
            _exitMessagePrefix = string.Format(CultureInfo.InvariantCulture, "Exit:{0}-", id);
        }

        internal void Start(string commandText)
        {
            _startCommand = string.Format(CultureInfo.InvariantCulture, "echo \"{0}\"; {1}; echo \"{2}$?\"", _beginMessage, commandText, _exitMessagePrefix);
            Shell.WriteLine(_startCommand);
        }

        protected override void OnOutputReceived(object sender, string e)
        {
            IEnumerable<string> linesToSend = null;

            if (string.IsNullOrEmpty(e))
                return;

            _lineBuffer.ProcessText(e, out linesToSend);

            foreach (string line in linesToSend)
            {
                if (_bClosed == 1)
                {
                    return;
                }

                if (_startCommand != null)
                {
                    if (line.EndsWith(_startCommand, StringComparison.Ordinal))
                    {
                        // When logged in as root, shell sends a copy of stdin to stdout.
                        // This ignores the shell command that was used to launch the debugger.
                        continue;
                    }

                    int endCommandIndex = line.IndexOf(_exitMessagePrefix, StringComparison.Ordinal);
                    if (endCommandIndex >= 0)
                    {
                        if (Interlocked.CompareExchange(ref _firedOnExit, 1, 0) == 0)
                        {
                            string exitCode = SplitExitCode(line, endCommandIndex + _exitMessagePrefix.Length);
                            Callback.OnExit(exitCode);
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
                }

                Callback.OnOutputLine(line);
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