// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using liblinux.Shell;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using liblinux;

namespace Microsoft.SSHDebugPS
{
    internal class AD7UnixAsyncCommand : IDebugUnixShellAsyncCommand
    {
        private readonly object _lock = new object();
        private readonly IDebugUnixShellCommandCallback _callback;

        private IRemoteSystem _remoteSystem;
        private NonHostedCommand _command;

        public AD7UnixAsyncCommand(IRemoteSystem remoteSystem, IDebugUnixShellCommandCallback callback)
        {
            _remoteSystem = remoteSystem;
            _callback = callback;
        }

        internal void Start(string commandText)
        {
            _command = _remoteSystem.Shell.ExecuteCommandAsynchronously(commandText, Timeout.Infinite);
            _command.Finished += (sender, e) => _callback.OnExit(_command.ExitCode.ToString());
            _command.OutputReceived += (sender, e) => _callback.OnOutputLine(e.Output);

            _command.RedirectErrorOutputToOutput = true;
            _command.BeginOutputRead();
        }

        void IDebugUnixShellAsyncCommand.Write(string text)
        {
            lock (_lock)
            {
                if (_command != null)
                {
                    _command.Write(text);
                }
            }
        }

        void IDebugUnixShellAsyncCommand.WriteLine(string text)
        {
            lock (_lock)
            {
                if (_command != null)
                {
                    _command.Write(text + "\r");
                }
            }
        }

        void IDebugUnixShellAsyncCommand.Abort()
        {
            Close();
        }

        private void Close()
        {
            lock (_lock)
            {
                if (_command != null)
                {
                    _command.Dispose();
                    _command = null;
                }
            }
        }
    }
}