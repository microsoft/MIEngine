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
            proc.StartInfo.WorkingDirectory = miDebuggerDir;

            //GDB locally requires that the directory be on the PATH, being the working directory isn't good enough
            if (proc.StartInfo.EnvironmentVariables.ContainsKey("PATH"))
            {
                proc.StartInfo.EnvironmentVariables["PATH"] = proc.StartInfo.EnvironmentVariables["PATH"] + ";" + miDebuggerDir;
            }

            InitProcess(proc, out reader, out writer);
        }

        protected override string GetThreadName()
        {
            return "MI.LocalTransport";
        }
    }
}
