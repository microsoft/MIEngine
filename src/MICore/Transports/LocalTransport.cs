// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;

namespace MICore
{
    public class LocalTransport : PipeTransport
    {
        public LocalTransport()
        {
        }

        public override void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer)
        {
            LocalLaunchOptions localOptions = (LocalLaunchOptions)options;
            string miDebuggerDir = System.IO.Path.GetDirectoryName(localOptions.MIDebuggerPath);

            Process proc = new Process();
            proc.StartInfo.FileName = localOptions.MIDebuggerPath;
            proc.StartInfo.Arguments = "--interpreter=mi";

            // LLDB has the -environment-cd mi command that is used to set the working dir for gdb/clrdbg, but it doesn't work.
            // So, set lldb's working dir to the user's requested folder before launch.
            proc.StartInfo.WorkingDirectory = options.DebuggerMIMode == MIMode.Lldb ? options.WorkingDirectory : miDebuggerDir;

            // On Windows, GDB locally requires that the directory be on the PATH, being the working directory isn't good enough
            if (PlatformUtilities.IsWindows() &&
                options.DebuggerMIMode == MIMode.Gdb)
            {
                string path = proc.StartInfo.GetEnvironmentVariable("PATH");
                path = (string.IsNullOrEmpty(path) ? miDebuggerDir : path + ";" + miDebuggerDir);
                proc.StartInfo.SetEnvironmentVariable("PATH", path);
            }

            // Only pass the environment to launch clrdbg. For other modes, there are commands that set the environment variables
            // directly for the debuggee.
            if (options.DebuggerMIMode == MIMode.Clrdbg)
            {
                foreach (EnvironmentEntry entry in localOptions.Environment)
                {
                    proc.StartInfo.SetEnvironmentVariable(entry.Name, entry.Value);
                }
            }

            InitProcess(proc, out reader, out writer);
        }

        protected override string GetThreadName()
        {
            return "MI.LocalTransport";
        }
    }
}
