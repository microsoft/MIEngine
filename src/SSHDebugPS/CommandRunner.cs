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
        protected const int BUFMAX = 4096;

        protected ProcessStartInfo ProcessStartInfo { get; }
        private System.Diagnostics.Process _process;

        private byte _readerCompleteCount = 0;
        private bool _processExited = false;

        private StreamWriter _stdInWriter;
        private StreamReader _stdOutReader;
        private StreamReader _stdErrReader;

        private readonly object _lock = new object();

        public event EventHandler<string> OutputReceived;
        public event EventHandler<int> Closed;
        public event EventHandler<ErrorOccuredEventArgs> ErrorOccured;

        protected bool IsDisposeStarted => _process == null;

        public LocalCommandRunner(IPipeTransportSettings settings)
            : this(settings.Command, settings.CommandArgs)
        { }

        public LocalCommandRunner(string command, string commandArgs)
        {
            ProcessStartInfo = new ProcessStartInfo(command, commandArgs);

            ProcessStartInfo.RedirectStandardError = true;
            ProcessStartInfo.RedirectStandardInput = true;
            ProcessStartInfo.RedirectStandardOutput = true;

            ProcessStartInfo.UseShellExecute = false;
            ProcessStartInfo.CreateNoWindow = true;
        }

        public static LocalCommandRunner CreateInstance(bool handleRawOutput, IPipeTransportSettings settings)
        {
            return CreateInstance(handleRawOutput, settings.Command, settings.CommandArgs);
        }

        public static LocalCommandRunner CreateInstance(bool handleRawOutput, string command, string args)
        {
            if (handleRawOutput)
            {
                return new RawLocalCommandRunner(command, args);
            }
            else
            {
                return new LocalCommandRunner(command, args);
            }
        }

        public void Start()
        {
            ThrowIfDisposed();

            lock (_lock)
            {
                _process = new System.Diagnostics.Process();
                _process.StartInfo = this.ProcessStartInfo;

                _process.Exited += OnProcessExited;
                _process.EnableRaisingEvents = true;

                _process.Start();

                _stdInWriter = new StreamWriter(_process.StandardInput.BaseStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), BUFMAX);
                _stdOutReader = new StreamReader(_process.StandardOutput.BaseStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), false, BUFMAX);
                _stdErrReader = new StreamReader(_process.StandardError.BaseStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), false, BUFMAX);

                _ = ReadStdOutAsync(_stdOutReader);
                _ = ReadLoopAsync(_stdErrReader, OnErrorReceived);
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

        private void OnProcessExited(object sender, EventArgs args)
        {
            bool shouldClose = false;

            lock (_lock)
            {
                Debug.Assert(_processExited == false, "ProcessExit shouldn't be set more than once");
                _processExited = true;
                shouldClose = _readerCompleteCount == 2;
            }

            if (shouldClose)
            {
                Closed?.Invoke(this, _process.ExitCode);
            }
        }

        protected virtual void OnErrorReceived(string error)
        {
            ErrorOccured?.Invoke(this, new ErrorOccuredEventArgs(error));
        }

        protected virtual Task ReadStdOutAsync(StreamReader stdOutReader)
        {
            return ReadLoopAsync(stdOutReader, OnOutputReceived);
        }

        protected void OnOutputReceived(string line)
        {
            OutputReceived?.Invoke(this, line);
        }

        private async Task ReadLoopAsync(StreamReader reader, Action<string> action)
        {
            try
            {
                while (!this.IsDisposeStarted)
                {
                    string line = await reader.ReadLineAsync().ConfigureAwait(false);

                    if (this.IsDisposeStarted)
                    {
                        // Dispose was called
                        return;
                    }

                    if (line == null)
                    {
                        OnReaderComplete();
                        return; // end of stream
                    }

                    action(line);
                }
            }
            catch (Exception e)
            {
                if (!this.IsDisposeStarted) // ignore exceptions if we are disposing
                {
                    ReportException(e);
                    Dispose();
                }
            }
        }

        protected void OnReaderComplete()
        {
            bool shouldClose = false;

            lock (_lock)
            {
                Debug.Assert(_readerCompleteCount < 2, "OnReaderComplete should not be called more than twice");
                _readerCompleteCount++;
                shouldClose = _readerCompleteCount == 2 && _processExited;
            }

            if (shouldClose)
            {
                Closed?.Invoke(this, _process.ExitCode);
            }
        }

        private void EnsureRunning()
        {
            if (_process == null || _process.HasExited)
            {
                throw new InvalidOperationException(StringResources.Error_ShellNotRunning);
            }
        }

        private void CleanUpProcess()
        {
            if (_process != null)
            {
                lock (_lock)
                {
                    System.Diagnostics.Process process = _process;
                    if (process != null)
                    {
                        _process = null;

                        if (!process.HasExited)
                        {
                            try
                            {
                                process.Kill();
                            }
                            catch (Exception)
                            {
                                // Process may already be dead
                            }
                        }

                        // clean up event handlers.
                        process.Exited -= OnProcessExited;

                        _stdInWriter.Close();
                        _stdInWriter = null;

                        _stdOutReader.Close();
                        _stdOutReader = null;

                        _stdErrReader.Close();
                        _stdErrReader = null;

                        process.Close();
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
    /// Launches a local command where the OutputReceived event is fired with the raw line ending characters
    /// </summary>
    internal class RawLocalCommandRunner : LocalCommandRunner
    {
        public RawLocalCommandRunner(string command, string args) : base(command, args) { }

        protected override Task ReadStdOutAsync(StreamReader stdOutReader)
        {
            // Async reads against the stream aren't completing even though data is available, possibly because PineZorro
            // is using `.Result` on an I/O object. So use a dedicated thread instead.
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            // We shouldn't need a full stack for this thread, so reduce the stack size to 256KB
            const int stackSize = 256 * 1024;
            var thread = new Thread(() =>
                {
                    SyncReadStdOut(stdOutReader);
                    tcs.SetResult(null);
                },
                stackSize);

            thread.Name = string.Concat("SSHDebugPS: ", Path.GetFileNameWithoutExtension(this.ProcessStartInfo.FileName), " stdout reader");
            thread.Start();

            return tcs.Task;
        }

        private void SyncReadStdOut(StreamReader stdOutReader)
        {
            try
            {
                char[] buffer = new char[BUFMAX];
                while (!this.IsDisposeStarted)
                {
                    int bytesRead = stdOutReader.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string result = new string(buffer, 0, bytesRead);
                        OnOutputReceived(result);
                    }
                    else
                    {
                        OnReaderComplete();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!this.IsDisposeStarted)
                {
                    ReportException(ex);
                    Dispose();
                }
            }
        }

        protected override void OnErrorReceived(string error)
        {
            // stderr doesn't need to be read using raw I/O, but the call back does expect new line characters, so add them
            base.OnErrorReceived(error + Environment.NewLine);
        }
    }

    /// <summary>
    ///  Shell that uses a remote Connection to send commands and receive input/output
    /// </summary>
    internal class RemoteCommandRunner : ICommandRunner, IDebugUnixShellCommandCallback
    {
        private readonly string _commandText;
        private readonly Connection _remoteConnection;
        private IDebugUnixShellAsyncCommand _asyncCommand;
        private bool _isRunning;
        private readonly bool _handleRawOutput;

        public RemoteCommandRunner(IPipeTransportSettings settings, Connection remoteConnection, bool handleRawOutput)
            : this(settings.Command, settings.CommandArgs, remoteConnection, handleRawOutput)
        { }

        public RemoteCommandRunner(string command, string arguments, Connection remoteConnection, bool handleRawOutput)
        {
            _remoteConnection = remoteConnection;
            _commandText = string.Concat(command, " ", arguments);
            _handleRawOutput = handleRawOutput;
        }

        public void Start()
        {
            _remoteConnection.BeginExecuteAsyncCommand(_commandText, runInShell: _handleRawOutput == false, callback: this, asyncCommand: out _asyncCommand);
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
