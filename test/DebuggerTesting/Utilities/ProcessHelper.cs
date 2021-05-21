// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DebuggerTesting.Utilities.Windows;
using System.IO;
using System.ComponentModel;

namespace DebuggerTesting.Utilities
{
    public static class ProcessHelper
    {
        #region Methods

        public static void AddToPath(this Process process, string value)
        {
            string existingPath = PlatformUtilities.GetEnvironmentVariable(process.StartInfo, "Path");
            string newPath = string.IsNullOrWhiteSpace(existingPath) ?
                                value :
                                existingPath + ";" + value;
            PlatformUtilities.SetEnvironmentVariable(process.StartInfo, "Path", newPath);
        }

        /// <summary>
        /// Creates a ProcessStartInfo object from a file name and arguments
        /// </summary>
        public static ProcessStartInfo CreateProcessStartInfo(string fileName, string arguments)
        {
            Parameter.ThrowIfNull(fileName, nameof(fileName));

            ProcessStartInfo startInfo = new ProcessStartInfo();
#if !CORECLR
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
#endif
            startInfo.FileName = fileName;
            startInfo.Arguments = arguments;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = Path.GetDirectoryName(fileName);

            return startInfo;
        }

        /// <summary>
        /// Creates a Process object initialized with a ProcessStartInfo object
        /// </summary>
        public static Process CreateProcess(ProcessStartInfo startInfo)
        {
            Parameter.ThrowIfNull(startInfo, nameof(startInfo));

            Process process = new Process();
            process.StartInfo = startInfo;
            return process;
        }

        /// <summary>
        /// Creates a Process object from a file name and arguments
        /// </summary>
        public static Process CreateProcess(string fileName, string arguments)
        {
            return ProcessHelper.CreateProcess(ProcessHelper.CreateProcessStartInfo(fileName, arguments));
        }

        /// <summary>
        /// Provides a way to assure a process will is closed or killed when dispose is called
        /// </summary>
        public static IDisposable ProcessCleanup(ILoggingComponent logger, Process p)
        {
            return new ProcessCleanupHelper(logger, p);
        }

        #region ProcessCleanupHelper Class

        private sealed class ProcessCleanupHelper : DisposableObject
        {
            ILoggingComponent logger;
            Process process;

            public ProcessCleanupHelper(ILoggingComponent logger, Process process)
            {
                this.logger = logger;
                this.process = process;
            }

            protected override void Dispose(bool isDisposing)
            {
                if (isDisposing)
                {
                    if (this.process != null)
                    {
                        int killCount = ProcessHelper.KillProcess(process, recurse: true);
                        if (killCount > 0)
                        {
                            this.logger.WriteLine("Killed {0} debuggee process(es).", killCount);
                        }
                        this.process.Dispose();
                        this.process = null;
                    }
                }

                base.Dispose(isDisposing);
            }
        }

        #endregion

        /// <summary>
        /// Kills a process and child processes if requested
        /// </summary>
        /// <param name="process">The process to kill</param>
        /// <param name="recurse">True to kill child processes</param>
        /// <returns>The count of processes killed</returns>
        public static int KillProcess(Process process, bool recurse = false)
        {
            return KillProcess(process.Id, recurse);
        }

        /// <summary>
        /// Kills a process and child processes if requested
        /// </summary>
        /// <param name="processId">The process to kill</param>
        /// <param name="recurse">True to kill child processes</param>
        /// <returns>The count of processes killed</returns>
        public static int KillProcess(int processId, bool recurse = false)
        {
            if (recurse)
            {
                int killCount = 0;
                int[] childProcessIds = ProcessHelper.FindChildProcesses(processId);
                killCount += ProcessHelper.KillProcess(processId, recurse: false);
                foreach (int childProcessId in childProcessIds)
                {
                    killCount += ProcessHelper.KillProcess(childProcessId, recurse: true);
                }
                return killCount;
            }
            else
            {
                if (!IsProcessRunning(processId))
                {
                    return 0;
                }

                // This can fail, but don't want to raise an exception in the dispose that hides the root error.
                try
                {
                    Kill(processId);
                    return 1;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public static bool IsProcessRunning(int processId)
        {
            if (PlatformUtilities.IsLinux || PlatformUtilities.IsOSX)
            {
                return UnixNativeMethods.GetPGid(processId) >= 0;
            }
            else if (PlatformUtilities.IsWindows)
            {
                try
                {
                    return !TryGetProcessById(processId)?.HasExited ?? false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static void Kill(int processId)
        {
            if (PlatformUtilities.IsLinux || PlatformUtilities.IsOSX)
            {
                const int sigkill = 9;
                UnixNativeMethods.Kill(processId, sigkill);
            }
            else if (PlatformUtilities.IsWindows)
            {
                try
                {
                    TryGetProcessById(processId)?.Kill();
                }
                catch (Exception) { }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Trys to get a Process from a process id. Returns null if the process doesn't exist.
        /// </summary>
        private static Process TryGetProcessById(int processId)
        {
            try
            {
                return Process.GetProcessById(processId);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Try to get child processes by using 'pgrep -P ##'
        /// NOTE: This command is not on OSX 10.7 and earlier
        /// </summary>
        private static int[] FindChildProcesses(int parentProcessId)
        {
            try
            {
                return ProcessHelper.FindChildProcessIds(parentProcessId)
                    .Select((id) => id)
                    .ToArray();
            }
            catch (Exception)
            {
                return new int[] { };
            }
        }

        private static IEnumerable<int> FindChildProcessIds(int id)
        {
            if (PlatformUtilities.IsLinux || PlatformUtilities.IsOSX)
            {
                List<int> childProcessIds = new List<int>(1);
                string pgrepArgs = "-P {0}".FormatInvariantWithArgs(id);

                using (Process pgrepProcess = ProcessHelper.CreateProcess("pgrep", pgrepArgs))
                {
                    pgrepProcess.Start();
                    pgrepProcess.WaitForExit();

                    string childLine;
                    while ((childLine = pgrepProcess.StandardOutput.ReadLine()) != null)
                    {
                        int childProcessId = childLine.ToInt() ?? -1;
                        if (childProcessId > 0)
                        {
                            childProcessIds.Add(childProcessId);
                        }
                    }
                    return childProcessIds;
                }
            }
            else if (PlatformUtilities.IsWindows)
            {
                // Find all processes that are parented by the specified id
                return from p in Process.GetProcesses()
                       where WindowsProcessNativeMethods.GetParentProcessId(p.Id) == id
                       select p.Id;
            }
            throw new NotSupportedException();
        }

        public static void InvokeIfAlive(this Process process, Action action)
        {
            if (!process.HasExited)
            {
                lock (process)
                {
                    if (!process.HasExited)
                    {
                        action();
                    }
                }
            }
        }

        #endregion
    }
}
