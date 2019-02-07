// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using liblinux;
using liblinux.Shell;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS
{
    public interface IRawShell : IDisposable
    {
        event EventHandler<string> OutputReceived;
        event EventHandler<int> Closed;
        event EventHandler ErrorOccured;

        void WriteCommandStart(string startCommand);
        void Write(string text);
        void WriteLine(string text);
    }

    internal class RawLocalShell : IRawShell
    {
        private System.Diagnostics.Process _localProcess;
        private StreamWriter _stdoutWriter;

        public RawLocalShell(string command, string arguments)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(command, arguments);
            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;

            _localProcess = System.Diagnostics.Process.Start(processStartInfo);
            _stdoutWriter = new StreamWriter(_localProcess.StandardInput.BaseStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 4096, leaveOpen: true);
            _localProcess.OutputDataReceived += OnProcessOutput;
            _localProcess.ErrorDataReceived += OnErrorOutput;
            _localProcess.Exited += OnProcessExited;

            _localProcess.BeginOutputReadLine();
            _localProcess.BeginErrorReadLine();
            _localProcess.EnableRaisingEvents = true;
        }

        public event EventHandler<string> OutputReceived;
        public event EventHandler<int> Closed;
        public event EventHandler ErrorOccured;

        public void Dispose()
        {
            if (_localProcess != null)
            {
                _localProcess.Close();
            }

            if (_stdoutWriter != null)
            {
                _stdoutWriter.Close();
                _stdoutWriter = null;
            }
        }

        public void Write(string text)
        {
            _stdoutWriter.Write(text);
            _stdoutWriter.Flush();
        }

        public void WriteCommandStart(string startCommand)
        {
            WriteLine(startCommand);
        }

        public void WriteLine(string text)
        {
            _stdoutWriter.WriteLine(text);
            _stdoutWriter.Flush();
        }

        private void OnErrorOutput(object sender, DataReceivedEventArgs e)
        {
            ErrorOccured?.Invoke(sender, e);
        }

        private void OnProcessOutput(object sender, DataReceivedEventArgs e)
        {
            OutputReceived?.Invoke(this, e?.Data + '\n');
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            Closed?.Invoke(this, _localProcess.ExitCode);
        }
    }

    /// <summary>
    /// Launches a raw command that doesn't have shell-like input/output
    /// </summary>
    internal class CommandShell : IRawShell
    {
        private System.Diagnostics.Process _localProcess;
        private StreamWriter _stdoutWriter;
        private StreamReader _processReader;
        private StreamReader _processError;
        private CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        private bool _isClosed = false;

        public CommandShell(string command, string arguments)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(command, arguments);
            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.CreateNoWindow = true;

            _localProcess = System.Diagnostics.Process.Start(processStartInfo);
            _localProcess.Exited += OnProcessExited;
            _localProcess.EnableRaisingEvents = true;

            _stdoutWriter = new StreamWriter(_localProcess.StandardInput.BaseStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 4096, leaveOpen: true);
            _processReader = _localProcess.StandardOutput;
            _processError = _localProcess.StandardError;

            Thread outputThread = new Thread(() => ReadLoop(_processReader, _cancellationSource.Token, (msg) => { OutputReceived?.Invoke(this, msg); }));
            Thread errorThread = new Thread(() => ReadLoop(_processError, _cancellationSource.Token, (msg) => { ErrorOccured?.Invoke(this, null); }));

            outputThread.Start();
            errorThread.Start();
        }

        public event EventHandler<string> OutputReceived;
        public event EventHandler<int> Closed;
        public event EventHandler ErrorOccured;

        private static int BUFMAX = 4096;
        private void ReadLoop(StreamReader reader, CancellationToken token, Action<string> action)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    char[] buffer = new char[BUFMAX];
                    Task<int> task = reader.ReadAsync(buffer, 0, buffer.Length);
                    task.Wait(token);

                    if (task.Result > 0)
                        action(new string(buffer, 0, task.Result));
                }
            }
            catch (Exception)
            {
                ErrorOccured?.Invoke(this, null);
                Close();
                // close correctly?
            }
        }

        private void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_isClosed)
            {
                _isClosed = true;
                _cancellationSource.Cancel();
                if (_localProcess != null)
                    _localProcess.Exited -= OnProcessExited;
                _localProcess?.Close();

                _stdoutWriter?.Close();
                _processReader?.Close();
                _processError?.Close();

                _localProcess = null;
                _stdoutWriter = null;
                _processReader = null;
                _processError = null;
            }
        }

        public void Write(string text)
        {
            _stdoutWriter.Write(text);
            _stdoutWriter.Flush();
        }

        public void WriteCommandStart(string startCommand)
        {
            WriteLine(startCommand);
        }

        public void WriteLine(string text)
        {
            _stdoutWriter.WriteLine(text);
            _stdoutWriter.Flush();
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            Closed?.Invoke(this, _localProcess.ExitCode);
        }
    }

    /// <summary>
    ///  Shell that uses a remote Connection to send commands and receive input/output
    /// </summary>
    internal class RawRemoteShell : IRawShell, IDebugUnixShellCommandCallback
    {
        private IDebugUnixShellAsyncCommand _asyncCommand;
        private bool _isRunning;

        public RawRemoteShell(string command, string arguments, Connection remoteConnection)
        {
            string commandText = string.Concat(command, " ", arguments);
            remoteConnection.BeginExecuteAsyncCommand(commandText, runInShell: false, callback: this, asyncCommand: out _asyncCommand);
            _isRunning = true;
        }

        public event EventHandler<string> OutputReceived;
        public event EventHandler<int> Closed;
        public event EventHandler ErrorOccured;

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
            if (_isRunning)
            {
                _asyncCommand.Write(text);
            }
            else
                throw new InvalidOperationException("Is not running");
        }

        public void WriteCommandStart(string startCommand)
        {
            if (_isRunning)
            {
                _asyncCommand.WriteLine(startCommand);
            }
            else
                throw new InvalidOperationException("Is not running");
        }

        public void WriteLine(string text)
        {
            if (_isRunning)
            {
                _asyncCommand.WriteLine(text);
            }
            else
                throw new InvalidOperationException("Is not running");
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
                code = -1;
            }
            Closed?.Invoke(this, code);
        }
    }
}
