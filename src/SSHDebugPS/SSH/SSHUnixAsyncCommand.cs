// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Threading;
using liblinux;
using liblinux.Shell;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS.SSH
{
    internal class SSHUnixAsyncCommand : IDebugUnixShellAsyncCommand
    {
        private readonly object _lock = new object();
        private readonly IDebugUnixShellCommandCallback _callback;

        private IRemoteSystem _remoteSystem;
        private NonHostedCommand _command;

        public SSHUnixAsyncCommand(IRemoteSystem remoteSystem, IDebugUnixShellCommandCallback callback)
        {
            _remoteSystem = remoteSystem;
            _callback = callback;
        }

        internal void Start(string commandText)
        {
            if (!_remoteSystem.IsConnected)
            {
                _remoteSystem.Connect(_remoteSystem.ConnectionInfo);
            }

            if (_remoteSystem.IsConnected)
            {
                _command = _remoteSystem.Shell.ExecuteCommandAsynchronously(commandText, Timeout.Infinite);
                _command.Finished += (sender, e) => _callback.OnExit(((NonHostedCommand)sender).ExitCode.ToString(CultureInfo.InvariantCulture));
                _command.OutputReceived += (sender, e) => _callback.OnOutputLine(e.Output);

                _command.RedirectErrorOutputToOutput = true;
                _command.BeginOutputRead();
            }
            else
            {
                Debug.Fail("Remote System not connected.");
            }
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
                    _command.Write(text + "\n");
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
