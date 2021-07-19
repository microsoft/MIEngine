// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SysProcess = System.Diagnostics.Process;

namespace Microsoft.SSHDebugPS
{
    readonly struct ProcessResult
    {
        public int ExitCode { get; }
        public IList<string> StdOut { get; }
        public IList<string> StdErr { get; }

        public ProcessResult(int exitCode, IList<string> stdOut, IList<string> stdErr)
        {
            this.ExitCode = exitCode;
            this.StdOut = stdOut;
            this.StdErr = stdErr;
        }
    };

    internal static class LocalProcessAsyncRunner
    {
        public static Task<ProcessResult> ExecuteProcessAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
        {
            bool isComplete = false;
            var taskCompletionSource = new TaskCompletionSource<ProcessResult>();
            CancellationTokenRegistration cancellationTokenRegistration = new CancellationTokenRegistration();
            List<string> stdOut = new List<string>();
            List<string> stdError = new List<string>();
            SysProcess process = null;

            void Process_Exited(object sender, EventArgs e)
            {
                // Briefly take this lock to make sure that we had a chance to finish initialization, and to guard 'isComplete'
                lock (taskCompletionSource)
                {
                    if (isComplete)
                    {
                        return; // already canceled
                    }
                    isComplete = true;
                }

                cancellationTokenRegistration.Dispose();
                process.Exited -= Process_Exited;

                // Wait for any output handlers to flush
                process.WaitForExit();

                int exitCode = process.ExitCode;
                process.Close();

                taskCompletionSource.SetResult(new ProcessResult(exitCode, stdOut, stdError));
            }

            void OnCancel()
            {
                // Briefly take this lock to make sure that we had a chance to finish initialization, and to guard 'isComplete'
                lock (taskCompletionSource)
                {
                    if (isComplete)
                    {
                        return; // process already exited
                    }
                    isComplete = true;
                }

                taskCompletionSource.SetCanceled();
                process.Exited -= Process_Exited;

                try
                {
                    process.Kill();
                }
                catch (Exception)
                { }

                process.Close();
            }
            
            lock (taskCompletionSource)
            {
                cancellationTokenRegistration = cancellationToken.Register(OnCancel);
                process = new SysProcess();
                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;
                process.Exited += Process_Exited;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        return; // ignore end-of-stream
                    }

                    stdOut.Add(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        return; // ignore end-of-stream
                    }

                    stdError.Add(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            return taskCompletionSource.Task;
        }
    }
}
