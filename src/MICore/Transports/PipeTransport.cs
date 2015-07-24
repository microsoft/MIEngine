// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Collections.Specialized;
using System.Collections;
using System.Threading.Tasks;
using System.Globalization;

namespace MICore
{
    public class PipeTransport : StreamTransport
    {
        private Process _process;
        private StreamReader _stdErrReader;
        private int _remainingReaders;
        private ManualResetEvent _allReadersDone = new ManualResetEvent(false);

        public PipeTransport()
        {
        }

        protected override string GetThreadName()
        {
            return "MI.PipeTransport";
        }

        protected virtual void InitProcess(Process proc, out StreamReader stdout, out StreamWriter stdin)
        {
            _process = proc;
            _process.EnableRaisingEvents = true;
            _process.Exited += OnProcessExit;

            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;

            lock (_process)
            {
                this.Callback.AppendToInitializationLog(string.Format(CultureInfo.InvariantCulture, "Starting: \"{0}\" {1}", _process.StartInfo.FileName, _process.StartInfo.Arguments));
                _process.Start();

                stdout = _process.StandardOutput;
                stdin = _process.StandardInput;
                _stdErrReader = _process.StandardError;
                _remainingReaders = 2;

                AsyncReadFromStdError();
            }
        }

        public override void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer)
        {
            PipeLaunchOptions pipeOptions = (PipeLaunchOptions)options;

            Process proc = new Process();
            proc.StartInfo.FileName = pipeOptions.PipePath;
            proc.StartInfo.Arguments = pipeOptions.PipeArguments;
            proc.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(pipeOptions.PipePath);

            InitProcess(proc, out reader, out writer);
        }

        public override void Close()
        {
            if (_writer != null)
            {
                Echo("logout");
            }

            base.Close();

            if (_stdErrReader != null)
            {
                _stdErrReader.Close();
            }

            _allReadersDone.Set();

            if (_process != null)
            {
                _process.EnableRaisingEvents = false;
                _process.Exited -= OnProcessExit;
                _process.Close();
            }
        }

        protected override void OnReadStreamAborted()
        {
            DecrementReaders();

            try
            {
                if (_process.WaitForExit(50))
                {
                    // If the pipe process has already exited, or is just about to exit, we want to send the abort event from OnProcessExit
                    // instead of from here since that will have access to stderr
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                Debug.Assert(IsClosed);
                return; // already closed
            }

            base.OnReadStreamAborted();
        }

        private async void AsyncReadFromStdError()
        {
            try
            {
                while (true)
                {
                    string line = await _stdErrReader.ReadLineAsync();
                    if (line == null)
                        break;

                    this.Callback.OnStdErrorLine(line);
                }
            }
            catch (Exception)
            {
                // If anything goes wrong, don't crash VS
            }

            DecrementReaders();
        }

        /// <summary>
        /// Called when either the stderr or the stdout reader exits. Used to synchronize between obtaining stderr/stdout content and the target process exiting.
        /// </summary>
        private void DecrementReaders()
        {
            if (Interlocked.Decrement(ref _remainingReaders) == 0)
            {
                _allReadersDone.Set();
            }
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            // Wait until 'Init' gets a chance to set m_Reader/m_Writer before sending up the debugger exit event
            if (_reader == null || _writer == null)
            {
                lock (_process)
                {
                    if (_reader == null || _writer == null)
                    {
                        return; // something went wrong and these are still null, ignore process exit
                    }
                }
            }

            // Wait for a bit to get all the stderr/stdout text. We can't wait too long on this since it is possble that
            // this pipe might still be arround if the the process we started kicked off other processes that still have
            // our pipe.
            _allReadersDone.WaitOne(100);

            if (!this.IsClosed)
            {
                // We are sometimes seeing m_process throw InvalidOperationExceptions by the time we get here. 
                // Attempt to get the real exit code, if we can't, still log the message with unknown exit code.
                string exitCode = null;
                try
                {
                    exitCode = string.Format(CultureInfo.InvariantCulture, "{0} (0x{0:X})", _process.ExitCode);
                }
                catch (InvalidOperationException)
                {
                    exitCode = "Unknown";
                }
                this.Callback.AppendToInitializationLog(string.Format(CultureInfo.InvariantCulture, "\"{0}\" exited with code {1}.", _process.StartInfo.FileName, exitCode));


                try
                {
                    this.Callback.OnDebuggerProcessExit();
                }
                catch
                {
                    // We have no exception back stop here, and we are trying to report failures. But if something goes wrong,
                    // lets not crash VS
                }
            }
        }
    }
}
