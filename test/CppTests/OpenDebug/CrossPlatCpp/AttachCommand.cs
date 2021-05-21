// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.Utilities;
using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.CrossPlatCpp
{
    public class AttachCommand : AttachCommand<CppLaunchCommandArgs>
    {

        public AttachCommand(IDebuggerSettings settings, Process process)
        {
            this.Timeout = TimeSpan.FromSeconds(15);

            this.Args.processId = process.Id;
            this.Args.name = CreateName(settings);
            this.Args.program = process.StartInfo.FileName;
            this.Args.args = new string[] { };
            this.Args.request = "attach";
            this.Args.environment = new EnvironmentEntry[] { };
            this.Args.launchOptionType = "Local";
            this.Args.sourceFileMap = new Dictionary<string, string>();

            if (settings.DebuggerType == SupportedDebugger.VsDbg)
            {
                this.Args.type = "cppvsdbg";
            }
            else
            {
                this.Args.stopAtEntry = false;
                this.Args.cwd = Path.GetDirectoryName(process.StartInfo.FileName);
                this.Args.type = "cppdbg";
                this.Args.miDebuggerPath = settings.DebuggerPath;
                this.Args.targetArchitecture = settings.DebuggeeArchitecture.ToArchitectureString();
                this.Args.noDebug = false;
                this.Args.coreDumpPath = null;
                this.Args.MIMode = settings.MIMode;
            }
        }

        private string CreateName(IDebuggerSettings settings)
        {
            string debuggerName = Enum.GetName(typeof(SupportedDebugger), settings.DebuggerType);
            return "C++ ({0})".FormatInvariantWithArgs(debuggerName);
        }

        public override string ToString()
        {
            return "{0} ({1})".FormatInvariantWithArgs(base.ToString(), this.Args.program);
        }
    }
}
