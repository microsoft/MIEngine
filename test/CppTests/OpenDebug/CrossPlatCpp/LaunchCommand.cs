// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.Utilities;
using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.CrossPlatCpp
{
    #region LaunchCommandArgs

    public sealed class CppLaunchCommandArgs : LaunchCommandArgs
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
        [JsonConverter(typeof(VisualizerFileConverter))]
        public List<string> VisualizerFile { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ShowDisplayString;
    }

    internal class VisualizerFileConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            List<string> visualizerFile = new List<string>();
            if (reader.TokenType == JsonToken.StartArray)
            {
                visualizerFile = serializer.Deserialize<List<string>>(reader);
            }
            else
            {
                visualizerFile.Add(reader.Value.ToString());
            }
            return visualizerFile;
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            // throw new NotImplementedException();
            serializer.Serialize(writer, value);
        }
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
        public LaunchCommand(IDebuggerSettings settings, string program, string visualizerFile = null, bool isAttach = false, params string[] args)
        {
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
                this.Args.VisualizerFile = new List<string>();
                this.Args.VisualizerFile.Add(visualizerFile);
                this.Args.ShowDisplayString = !string.IsNullOrEmpty(visualizerFile);
            }
        }

        /// <summary>
        /// Launches a new process and attaches to it - handles multiple visualizerFiles
        /// </summary>
        /// <param name="program">The full path to the program to launch</param>
        /// <param name="architecture">The architecture of the program</param>
        /// <param name="args">[OPTIONAL] Args to pass to the program</param>
        public LaunchCommand(IDebuggerSettings settings, string program, List<string> visualizerFile = null, bool isAttach = false, params string[] args)
        {
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
                // this.Args.ShowDisplayString = !string.IsNullOrEmpty(visualizerFile);
                this.Args.ShowDisplayString = visualizerFile != null && visualizerFile.Count > 0;
            }
        }

        /// <summary>
        /// Launch a core dump
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="program"></param>
        /// <param name="coreDumpPath"></param>
        public LaunchCommand(IDebuggerSettings settings, string program, string coreDumpPath)
            : this(settings, program, string.Empty, false, null)
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
