// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsDebugLauncher
{
    internal class DebugLauncher : IDisposable
    {
        private static readonly int BUFFER_SIZE = 1024 * 4;

        internal class LaunchParameters
        {
            public string PipeServer { get; set; }
            public string StdInPipeName { get; set; }
            public string StdOutPipeName { get; set; }
            public string StdErrPipeName { get; set; }
            public string PidPipeName { get; set; }
            public string DbgExe { get; set; }
            public List<string> DbgExeArgs { get; set; }

            public LaunchParameters()
            {
                DbgExeArgs = new List<string>();
            }

            /// <summary>
            /// Ensures all parameters have been set
            /// </summary>
            /// <returns></returns>
            public bool ValidateParameters()
            {
                return !(string.IsNullOrEmpty(PipeServer)
                    || string.IsNullOrEmpty(StdInPipeName)
                    || string.IsNullOrEmpty(StdOutPipeName)
                    || string.IsNullOrEmpty(StdErrPipeName)
                    || string.IsNullOrEmpty(PidPipeName)
                    || string.IsNullOrEmpty(DbgExe));
            }

            public string ParametersAsString()
            {
                StringBuilder argString = new StringBuilder();
                foreach (var arg in DbgExeArgs.ToList())
                {
                    if (arg.Contains(' ', StringComparison.Ordinal))
                    {
                        argString.Append("\"" + arg + "\"");
                    }
                    else
                    {
                        argString.Append(arg);
                    }

                    argString.Append(' ');
                }

                return argString.ToString();
            }
        }

        private LaunchParameters _parameters;

        private bool _isRunning = true;

        private StreamWriter _debuggerCommandStream;
        private StreamReader _debuggerOutputStream;
        private StreamReader _debuggerErrorStream;

        private StreamReader _npCommandStream;
        private StreamWriter _npErrorStream;
        private StreamWriter _npOutputStream;
        private StreamWriter _npPidStream;

        private Process _dbgProcess;

        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public DebugLauncher(LaunchParameters parameters)
        {
            _parameters = parameters;
        }

        public void StartPipeConnection()
        {
            NamedPipeClientStream inputStream = new NamedPipeClientStream(_parameters.PipeServer, _parameters.StdInPipeName, PipeDirection.In, PipeOptions.None, TokenImpersonationLevel.Impersonation);
            NamedPipeClientStream outputStream = new NamedPipeClientStream(_parameters.PipeServer, _parameters.StdOutPipeName, PipeDirection.Out, PipeOptions.None, TokenImpersonationLevel.Impersonation);
            NamedPipeClientStream errorStream = new NamedPipeClientStream(_parameters.PipeServer, _parameters.StdErrPipeName, PipeDirection.Out, PipeOptions.None, TokenImpersonationLevel.Impersonation);
            NamedPipeClientStream pidStream = new NamedPipeClientStream(_parameters.PipeServer, _parameters.PidPipeName, PipeDirection.Out, PipeOptions.None, TokenImpersonationLevel.Impersonation);

            try
            {
                // Connect as soon as possible
                inputStream.Connect();
                outputStream.Connect();
                errorStream.Connect();
                pidStream.Connect();

                Encoding encNoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                _npCommandStream = new StreamReader(inputStream, encNoBom, false, BUFFER_SIZE);
                _npOutputStream = new StreamWriter(outputStream, encNoBom, BUFFER_SIZE) { AutoFlush = true };
                _npErrorStream = new StreamWriter(errorStream, encNoBom, BUFFER_SIZE) { AutoFlush = true };
                _npPidStream = new StreamWriter(pidStream, encNoBom, 5000) { AutoFlush = true };
                
                ProcessStartInfo info = new ProcessStartInfo();

                if (Path.IsPathRooted(_parameters.DbgExe))
                {
                    info.WorkingDirectory = Path.GetDirectoryName(_parameters.DbgExe);
                }

                info.FileName = _parameters.DbgExe;
                info.Arguments = _parameters.ParametersAsString();
                info.UseShellExecute = false;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;

                _dbgProcess = new Process();
                _dbgProcess.StartInfo = info;
                _dbgProcess.EnableRaisingEvents = true;
                _dbgProcess.Exited += OnProcessExited;

                _dbgProcess.Start();
                _debuggerCommandStream = new StreamWriter(_dbgProcess.StandardInput.BaseStream, encNoBom) { AutoFlush = true };
                _debuggerOutputStream = _dbgProcess.StandardOutput;
                _debuggerErrorStream = _dbgProcess.StandardError;

                Thread readThread = new Thread(() => ReadWriteLoop(_npCommandStream, _debuggerCommandStream, _cancellationTokenSource.Token));
                readThread.Name = "MIEngine.DbgInputThread";
                readThread.Start();

                Thread outputThread = new Thread(() => ReadWriteLoop(_debuggerOutputStream, _npOutputStream, _cancellationTokenSource.Token));
                outputThread.Name = "MIEngine.DbgOutputThread";
                outputThread.Start();

                Thread errThread = new Thread(() => ReadWriteLoop(_debuggerErrorStream, _npErrorStream, _cancellationTokenSource.Token));
                errThread.Name = "MIEngine.DbgErrorThread";
                errThread.Start();

                _npPidStream.WriteLine(Process.GetCurrentProcess().Id.ToString(CultureInfo.CurrentCulture));
                _npPidStream.WriteLine(_dbgProcess.Id.ToString(CultureInfo.CurrentCulture));
            }
            catch (Exception e)
            {
                Debug.Fail($"Exception caught in StartPipeConnection. Message: {e.Message}");
                ReportExceptionAndShutdown(e);
            }
        }

        private void OnProcessExited(object c, EventArgs e)
        {
            Shutdown();
        }

        private void Shutdown()
        {
            if (_isRunning)
            {
                _isRunning = false;
                _cancellationTokenSource.Cancel();
                _dbgProcess?.Close();
                _dbgProcess = null;
            }
        }

        private void ReadWriteLoop(StreamReader reader, StreamWriter writer, CancellationToken token)
        {
            try
            {
                while (_isRunning)
                {
                    string line = ReadLine(reader, token);
                    {
                        //Console.Error.WriteLine(symbol + line);
                        if (line != null)
                        {
                            writer.WriteLine(line);
                            writer.Flush();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Fail($"Exception caught in ReadWriteLoop. Message: {e.Message}");
                ReportExceptionAndShutdown(e);
            }
        }

        private void ReportExceptionAndShutdown(Exception e)
        {
            try
            {
                _npErrorStream.WriteLine(FormattableString.Invariant($"Exception while debugging. {e.Message}. Shutting down."));
            }
            catch (Exception) { } // Eat any exceptions
            finally
            {
                Shutdown();
            }
        }

        private string ReadLine(StreamReader reader, CancellationToken token)
        {
            try
            {
                //return reader.ReadLine();
                Task<string> task = reader.ReadLineAsync();
                task.Wait(token);
                return task.Result;
            }
            catch (Exception e)
            {
                // We will get some exceptions we expect. Assert only on the ones we don't expect
                Debug.Assert(
                    e is OperationCanceledException
                    || e is IOException
                    || e is ObjectDisposedException
                    || (e is AggregateException && ((AggregateException)e).InnerException is ArgumentException), // we get this when the host side is closed
                    "Exception throw from ReadLine when we haven't quit yet");
                Shutdown();
                return null;
            }
        }
        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_isRunning)
                {
                    Shutdown();
                }

                try
                {
                    _npCommandStream?.Dispose();
                    _npOutputStream?.Dispose();
                    _npErrorStream?.Dispose();

                    _npPidStream?.Dispose();

                    _cancellationTokenSource?.Dispose();

                    _debuggerCommandStream?.Dispose();
                    _debuggerOutputStream?.Dispose();
                    _debuggerErrorStream?.Dispose();
                }
                // catch all exceptions
                catch
                { }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
