// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.MIDebugEngine
{
    public static class MIDebugCommandDispatcher
    {
        private readonly static List<DebuggedProcess> s_processes = new List<DebuggedProcess>();

        public static Task<string> ExecuteCommand(string command)
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
            return ExecuteCommand(command, lastProcess);
        }

        internal static Task<string> ExecuteCommand(string command, DebuggedProcess process, bool ignoreFailures=false)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentNullException("command");

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
                return process.ConsoleCmdAsync(command, ignoreFailures);
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

            lock (s_processes)
            {
                s_processes.Add(process);
            }
        }

        private static void process_DebuggerExitEvent(object sender, EventArgs e)
        {
            DebuggedProcess debuggedProcess = sender as DebuggedProcess;
            if (debuggedProcess != null)
            {
                debuggedProcess.DebuggerExitEvent -= process_DebuggerExitEvent;
                lock (s_processes)
                {
                    s_processes.Remove(debuggedProcess);
                }
            }
        }
    }
}
