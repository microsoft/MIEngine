// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.OpenDebug.CrossPlatCpp;
using DebuggerTesting.Utilities;
using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.CrossPlatCpp
{
    #region LaunchCommandArgs

    public class CppLaunchCommandArgs : LaunchCommandArgs
    {
        public string launchOptionType;
        public string miDebuggerPath;
        public string targetArchitecture;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string symbolSearchPath;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string coreDumpPath;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? processId;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? externalConsole;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MIMode;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object VisualizerFile;


        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ShowDisplayString;
    }

    #endregion

    public class LaunchCommand : LaunchCommand<CppLaunchCommandArgs>
    {
        /// <summary>
        /// Launches a new process and attaches to it
        /// </summary>
        /// <param name="program">The full path to the program to launch</param>
        /// <param name="architecture">The architecture of the program</param>
        /// <param name="args">[OPTIONAL] Args to pass to the program</param>
        public LaunchCommand(IDebuggerSettings settings, string program, object visualizerFile = null, bool isAttach = false, params string[] args)
        {
            if (!(visualizerFile == null || visualizerFile is string || visualizerFile is List<string>))
            {
                throw new ArgumentOutOfRangeException(nameof(visualizerFile));
            }

            this.Timeout = TimeSpan.FromSeconds(15);

            this.Args.name = CreateName(settings);
            this.Args.program = program;
            this.Args.args = args ?? new string[] { };
            this.Args.request = "launch";
            this.Args.cwd = Path.GetDirectoryName(program);
            this.Args.environment = new EnvironmentEntry[] { };
            this.Args.launchOptionType = "Local";
            this.Args.symbolSearchPath = String.Empty;
            this.Args.sourceFileMap = new Dictionary<string, string>();

            if (settings.DebuggerType == SupportedDebugger.VsDbg)
            {
                this.Args.type = "cppvsdbg";
            }
            else
            {
                this.Args.type = "cppdbg";
                this.Args.miDebuggerPath = settings.DebuggerPath;
                this.Args.targetArchitecture = settings.DebuggeeArchitecture.ToArchitectureString();
                this.Args.MIMode = settings.MIMode;
                this.Args.VisualizerFile = visualizerFile;
                this.Args.ShowDisplayString = visualizerFile != null;
            }
        }

        /// <summary>
        /// Launch a core dump
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="program"></param>
        /// <param name="coreDumpPath"></param>
        public LaunchCommand(IDebuggerSettings settings, string program, string coreDumpPath)
            : this(settings, program)
        {
            this.Args.coreDumpPath = coreDumpPath;
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
