// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.MIDebugEngine
{
    public static class MIDebugCommandDispatcher
    {
        private readonly static List<DebuggedProcess> s_processes = new List<DebuggedProcess>();

        private static DebuggedProcess GetLastProcess()
        {
            DebuggedProcess lastProcess;
            lock (s_processes)
            {
                if (s_processes.Count == 0)
                {
                    throw new InvalidOperationException(MICoreResources.Error_NoMIDebuggerProcess);
                }

                lastProcess = s_processes[s_processes.Count - 1];
            }

            if (lastProcess == null)
            {
                throw new InvalidOperationException(MICoreResources.Error_NoMIDebuggerProcess);
            }

            return lastProcess;
        }

        public static MICore.ProcessState GetProcessState()
        {
            return GetLastProcess().ProcessState;
        }

        public static async Task<Results> ExecuteMICommandWithResultsObject(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentNullException(nameof(command));

            command = command.Trim();

            if (command[0] == '-')
            {
                return await GetLastProcess().CmdAsync(command, ResultClass.None);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(command));
            }
        }

        public static Task<string> ExecuteCommand(string command)
        {
            return ExecuteCommand(command, GetLastProcess());
        }

        internal static Task<string> ExecuteCommand(string command, DebuggedProcess process, bool ignoreFailures = false)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentNullException(nameof(command));

            if (process == null)
            {
                throw new InvalidOperationException(MICoreResources.Error_NoMIDebuggerProcess);
            }

            command = command.Trim();

            if (command[0] == '-')
            {
                return ExecuteMiCommand(process, command, ignoreFailures);
            }
            else
            {
                return process.ConsoleCmdAsync(command, allowWhileRunning: false, ignoreFailures: ignoreFailures);
            }
        }

        private static async Task<string> ExecuteMiCommand(DebuggedProcess lastProcess, string command, bool ignoreFailures)
        {
            Results results = await lastProcess.CmdAsync(command, ignoreFailures ? ResultClass.None : ResultClass.done);
            return results.ToString();
        }

        internal static void AddProcess(DebuggedProcess process)
        {
            process.DebuggerExitEvent += process_DebuggerExitEvent;
            process.DebuggerAbortedEvent += process_DebuggerAbortedEvent;

            lock (s_processes)
            {
                s_processes.Add(process);
            }
        }

        private static void process_DebuggerAbortedEvent(object sender, DebuggerAbortedEventArgs args)
        {
            process_DebuggerExitEvent(sender, null);
        }

        private static void process_DebuggerExitEvent(object sender, EventArgs e)
        {
            DebuggedProcess debuggedProcess = sender as DebuggedProcess;
            if (debuggedProcess != null)
            {
                debuggedProcess.DebuggerExitEvent -= process_DebuggerExitEvent;
                debuggedProcess.DebuggerAbortedEvent -= process_DebuggerAbortedEvent;
                lock (s_processes)
                {
                    s_processes.Remove(debuggedProcess);
                }
            }
        }

        public static void EnableLogging(bool output, string logFile)
        {
            if (!string.IsNullOrEmpty(logFile))
            {
                string tempDirectory = Path.GetTempPath();
                if (Path.IsPathRooted(logFile) || (!string.IsNullOrEmpty(tempDirectory) && Directory.Exists(tempDirectory)))
                {
                    string filePath = Path.Combine(tempDirectory, logFile);

                    File.CreateText(filePath).Dispose(); // Test to see if we can create a text file in HostLogChannel. This will allow the error to be shown when enabling the setting.

                    logFile = filePath;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(logFile));
                }
            }

            Logger.CmdLogInfo.logFile = logFile;
            if (output)
                Logger.CmdLogInfo.logToOutput = WriteLogToOutput;
            else
                Logger.CmdLogInfo.logToOutput = null;
            Logger.CmdLogInfo.enabled = true;
            Logger.Reset();
        }

        public static void WriteLogToOutput(string line)
        {
            lock (s_processes)
            {
                if (s_processes.Count > 0)
                {
                    s_processes[0].WriteOutput(line); ;
                }
            }
        }
    }
}
