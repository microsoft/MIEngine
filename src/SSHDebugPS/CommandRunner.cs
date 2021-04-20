// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS
{
    internal class ErrorOccuredEventArgs : EventArgs
    {
        private string _errorMessage = null;
        public ErrorOccuredEventArgs(Exception e)
        {
            Exception = e;
        }

        public ErrorOccuredEventArgs(string message)
        {
            _errorMessage = message;
        }

        public string ErrorMessage { get => !string.IsNullOrWhiteSpace(_errorMessage) ? _errorMessage : Exception.Message; }

        public Exception Exception { get; }
    }

    internal interface ICommandRunner : IDisposable
    {
        event EventHandler<string> OutputReceived;
        event EventHandler<int> Closed;
        event EventHandler<ErrorOccuredEventArgs> ErrorOccured;

        void Start();

        void Write(string text);
        void WriteLine(string text);
    }

    /// <summary>
    /// Run a single command on Windows. This reads output as ReadLine. Run needs to be called to run the command.
    /// </summary>
    internal class LocalCommandRunner : ICommandRunner
    {
        public static int BUFMAX = 4096;

        private ProcessStartInfo _processStartInfo;
        private System.Diagnostics.Process _process;
        private CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        private System.Threading.Tasks.Task _outputReadLoopTask;
        private System.Threading.Tasks.Task _errorReadLoopTask;
        private bool _hasExited = false;

        private StreamWriter _stdInWriter;
        private StreamReader _stdOutReader;
        private StreamReader _stdErrReader;

        protected object _lock = new object();

        public event EventHandler<string> OutputReceived;
        public event EventHandler<int> Closed;
        public event EventHandler<ErrorOccuredEventArgs> ErrorOccured;

        public LocalCommandRunner(IPipeTransportSettings settings)
            : this(settings.Command, settings.CommandArgs)
        { }

        public LocalCommandRunner(string command, string commandArgs)
        {
            _processStartInfo = new ProcessStartInfo(command, commandArgs);

            _processStartInfo.RedirectStandardError = true;
            _processStartInfo.RedirectStandardInput = true;
            _processStartInfo.RedirectStandardOutput = true;

            _processStartInfo.UseShellExecute = false;
            _processStartInfo.CreateNoWindow = true;
        }

        public void Start()
        {
            ThrowIfDisposed();
            if (_processStartInfo == null)
            {
                throw new InvalidOperationException("Unable to create process. Process start info does not exist");
            }

            lock (_lock)
            {
                _process = new System.Diagnostics.Process();
                _process.StartInfo = _processStartInfo;

                _process.Exited += OnProcessExited;
                _process.EnableRaisingEvents = true;

                _process.Start();

                _stdInWriter = new StreamWriter(_process.StandardInput.BaseStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), BUFMAX);
                _stdOutReader = new StreamReader(_process.StandardOutput.BaseStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), false, BUFMAX);
                _stdErrReader = new StreamReader(_process.StandardError.BaseStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), false, BUFMAX);

                _outputReadLoopTask = System.Threading.Tasks.Task.Run(() => ReadLoop(_stdOutReader, _cancellationSource.Token, true, OnOutputReceived));
                _errorReadLoopTask = System.Threading.Tasks.Task.Run(() => ReadLoop(_stdErrReader, _cancellationSource.Token, false, OnErrorReceived));
            }
        }

        public void Write(string text)
        {
            ThrowIfDisposed();
            lock (_lock)
            {
                EnsureRunning();
                _stdInWriter.Write(text);
                _stdInWriter.Flush();

            }
        }

        public void WriteLine(string text)
        {
            ThrowIfDisposed();
            lock (_lock)
            {
                EnsureRunning();
                _stdInWriter.WriteLine(text);
                _stdInWriter.Flush();
            }
        }

        protected void ReportException(Exception ex)
        {
            ErrorOccured?.Invoke(this, new ErrorOccuredEventArgs(ex));
        }

        protected virtual void OnProcessExited(object sender, EventArgs args)
        {
            lock (_lock)
            {
                // Make sure that all output has been written before exiting.
                if (!_hasExited && _outputReadLoopTask.IsCompleted)
                {
                    _hasExited = true;
                    Closed?.Invoke(this, _process.ExitCode);
                }
            }
        }

        protected virtual void OnErrorReceived(string error)
        {
            ErrorOccured?.Invoke(this, new ErrorOccuredEventArgs(error));
        }

        protected virtual void OnOutputReceived(string line)
        {
            OutputReceived?.Invoke(this, line);
        }

        protected virtual void ReadLoop(StreamReader reader, CancellationToken token, bool checkForExitedProcess, Action<string> action)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    Task<string> task = reader.ReadLineAsync();
                    task.Wait(token);

                    if (task.Result == null || token.IsCancellationRequested)
                    {
                        if (checkForExitedProcess)
                        {
                            VerifyProcessExitedAndNotify();
                        }
                        return; // end of stream
                    }

                    action(task.Result);
                }
            }
            catch (Exception e)
            {
                ReportException(e);
                Dispose();
            }
        }

        protected void VerifyProcessExitedAndNotify()
        {
            bool shouldClose = false;
            lock (_lock)
            {
                if (!_hasExited && _process.HasExited)
                {
                    _hasExited = true;
                    shouldClose = true;
                }
            }

            if (shouldClose)
            {
                Closed?.Invoke(this, _process.ExitCode);
            }

        }

        protected void EnsureRunning()
        {
            if (_process == null || _process.HasExited)
            {
                throw new InvalidOperationException(StringResources.Error_ShellNotRunning);
            }
        }

        protected void CleanUpProcess()
        {
            if (_process != null)
            {
                lock (_lock)
                {
                    if (_process != null)
                    {
                        if (!_process.HasExited)
                        {
                            _process.Kill();
                        }

                        // clean up event handlers.
                        _process.Exited -= OnProcessExited;
                        _process = null;

                        _cancellationSource.Cancel();

                        _outputReadLoopTask = null;
                        _errorReadLoopTask = null;

                        _stdInWriter.Close();
                        _stdInWriter = null;

                        _stdOutReader.Close();
                        _stdOutReader = null;

                        _stdErrReader.Close();
                        _stdErrReader = null;
                    }
                }
            }
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    CleanUpProcess();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        protected void ThrowIfDisposed()
        {
            if (_disposedValue)
            {
                throw new ObjectDisposedException("LocalCommandRunner");
            }
        }
        #endregion
    }

    /// <summary>
    /// Launches a local command where the output is read in a buffer.
    /// </summary>
    internal class RawLocalCommandRunner : LocalCommandRunner
    {
        public RawLocalCommandRunner(string command, string args) : base(command, args) { }

        public RawLocalCommandRunner(IPipeTransportSettings settings) : base(settings) { }

        protected override void ReadLoop(StreamReader reader, CancellationToken token, bool checkForExitedProcess, Action<string> action)
        {
            try
            {
                string result = string.Empty;
                while (!token.IsCancellationRequested)
                {
                    char[] buffer = new char[BUFMAX];
                    Task<int> task = reader.ReadAsync(buffer, 0, buffer.Length);
                    task.Wait(token);

                    if (task.Result > 0)
                    {
                        result += new string(buffer, 0, task.Result);
                        if (task.Result < BUFMAX)
                        {
                            action(result);
                            result = string.Empty;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(result))
                        {
                            action(result);
                            result = string.Empty;
                        }
                        if (checkForExitedProcess)
                        {
                            VerifyProcessExitedAndNotify();
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                ReportException(ex);
                Dispose();
            }
        }
    }

    /// <summary>
    ///  Shell that uses a remote Connection to send commands and receive input/output
    /// </summary>
    internal class RemoteCommandRunner : ICommandRunner, IDebugUnixShellCommandCallback
    {
        private IDebugUnixShellAsyncCommand _asyncCommand;
        private bool _isRunning;
        private string _commandText;
        private Connection _remoteConnection;

        public RemoteCommandRunner(IPipeTransportSettings settings, Connection remoteConnection)
            : this(settings.Command, settings.CommandArgs, remoteConnection)
        { }

        public RemoteCommandRunner(string command, string arguments, Connection remoteConnection)
        {
            _remoteConnection = remoteConnection;
            _commandText = string.Concat(command, " ", arguments);
        }

        public void Start()
        {
            _remoteConnection.BeginExecuteAsyncCommand(_commandText, runInShell: false, callback: this, asyncCommand: out _asyncCommand);
            _isRunning = true;
        }

        public event EventHandler<string> OutputReceived;
        public event EventHandler<int> Closed;
        public event EventHandler<ErrorOccuredEventArgs> ErrorOccured;

        public void Dispose()
        {
            if (_isRunning)
            {
                _asyncCommand.Abort();
                _isRunning = false;
            }
        }

        public void Write(string text)
        {
            EnsureRunning();
            _asyncCommand.Write(text);
        }

        public void WriteLine(string text)
        {
            EnsureRunning();
            _asyncCommand.WriteLine(text);
        }

        void IDebugUnixShellCommandCallback.OnOutputLine(string line)
        {
            OutputReceived?.Invoke(this, line);
        }

        void IDebugUnixShellCommandCallback.OnExit(string exitCode)
        {
            _isRunning = false;
            int code;
            if (!Int32.TryParse(exitCode, out code))
            {
                ErrorOccured?.Invoke(this, new ErrorOccuredEventArgs(StringResources.Error_ExitCodeNotParseable));
                code = -1;
            }
            Closed?.Invoke(this, code);
        }

        private void EnsureRunning()
        {
            if (!_isRunning)
                throw new InvalidOperationException(StringResources.Error_ShellNotRunning);
        }
    }
}
