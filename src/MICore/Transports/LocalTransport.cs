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
using System.Runtime.InteropServices;

namespace MICore
{
    public class LocalTransport : PipeTransport
    {
        public LocalTransport()
        {
        }

        private bool IsValidMiDebuggerPath(string debuggerPath)
        {
            if (!File.Exists(debuggerPath))
            {
                return false;
            }
            else
            {
                // Verify the target is a file and not a directory
                FileAttributes attr = File.GetAttributes(debuggerPath);
                if ((attr & FileAttributes.Directory) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        public override void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer)
        {
            LocalLaunchOptions localOptions = (LocalLaunchOptions)options;

            if (!this.IsValidMiDebuggerPath(localOptions.MIDebuggerPath))
            {
                throw new Exception(MICoreResources.Error_InvalidMiDebuggerPath);
            }

            string miDebuggerDir = System.IO.Path.GetDirectoryName(localOptions.MIDebuggerPath);

            Process proc = new Process();
            proc.StartInfo.FileName = localOptions.MIDebuggerPath;
            proc.StartInfo.Arguments = "--interpreter=mi";
            proc.StartInfo.WorkingDirectory = miDebuggerDir;

            // On Windows, GDB locally requires that the directory be on the PATH, being the working directory isn't good enough
            if (PlatformUtilities.IsWindows() &&
                options.DebuggerMIMode == MIMode.Gdb)
            {
                string path = proc.StartInfo.GetEnvironmentVariable("PATH");
                path = (string.IsNullOrEmpty(path) ? miDebuggerDir : path + ";" + miDebuggerDir);
                proc.StartInfo.SetEnvironmentVariable("PATH", path);
            }

            foreach (EnvironmentEntry entry in localOptions.Environment)
            {
                proc.StartInfo.SetEnvironmentVariable(entry.Name, entry.Value);
            }

            InitProcess(proc, out reader, out writer);
        }

        protected override string GetThreadName()
        {
            return "MI.LocalTransport";
        }
    }
}
