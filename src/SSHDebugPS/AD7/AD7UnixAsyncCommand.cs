// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS
{
    internal class AD7UnixAsyncCommand : IDebugUnixShellAsyncCommand
    {
        private readonly object _lock = new object();
        private int _bClosed = 0;
        private bool _closeShellOnComplete;

        protected IDebugUnixShellCommandCallback Callback { get; }
        protected ICommandRunner CommandRunner { get; private set; }

        public AD7UnixAsyncCommand(ICommandRunner commandRunner, IDebugUnixShellCommandCallback callback, bool closeShellOnComplete)
        {
            CommandRunner = commandRunner;
            Callback = callback;
            _closeShellOnComplete = closeShellOnComplete;
            CommandRunner.OutputReceived += OnOutputReceived;
            CommandRunner.Closed += OnClosed;
            CommandRunner.ErrorOccured += OnError;

            CommandRunner.Start();
        }

        void IDebugUnixShellAsyncCommand.Write(string text)
        {
            if (_bClosed == 1)
            {
                return;
            }

            lock (_lock)
            {
                if (CommandRunner != null)
                {
                    CommandRunner.Write(text);
                }
            }
        }

        void IDebugUnixShellAsyncCommand.WriteLine(string text)
        {
            if (_bClosed == 1)
            {
                return;
            }

            lock (_lock)
            {
                if (CommandRunner != null)
                {
                    CommandRunner.WriteLine(text);
                }
            }
        }

        void IDebugUnixShellAsyncCommand.Abort()
        {
            Close();
        }

        protected virtual void OnOutputReceived(object sender, string e)
        {
            if (string.IsNullOrEmpty(e))
                return;

            if (_bClosed == 1)
            {
                return;
            }

            Callback.OnOutputLine(e);
        }

        protected void OnError(object sender, ErrorOccuredEventArgs e)
        {
            Callback.OnOutputLine(e.ErrorMessage);
            Close();
        }

        protected void OnClosed(object sender, int exitCode)
        {
            Callback.OnExit(exitCode.ToString(CultureInfo.InvariantCulture));
            Close();
        }

        internal void Close()
        {
            // Don't want output processing thread and the PollThread to go through this at the same time
            // If PollThread issues Dispose and the output processing thread block on the _lock below, it will cause ThreadInterruptedException
            // in the output processing thread
            if (Interlocked.CompareExchange(ref _bClosed, 1, 0) == 0)
            {
                lock (_lock)
                {
                    CommandRunner.OutputReceived -= OnOutputReceived;
                    CommandRunner.ErrorOccured -= OnError;
                    CommandRunner.Closed -= OnClosed;

                    try
                    {
                        if (_closeShellOnComplete)
                            CommandRunner?.Dispose();
                    }
                    catch (ThreadInterruptedException)
                    {
                        // We will run into this when we are closing as a result of getting exit message
                        // in OnOutputReceived method in error cases. The method will be called on the thread
                        // that StreamingShell uses for output processing. Dispose tries to interrupt the
                        // same thread we are on leading to ThreadInterruptedException
                    }

                    CommandRunner = null;
                }
            }
        }
    }
}