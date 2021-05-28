// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands
{
    public abstract class LaunchCommandArgs : JsonValue
    {
        public string name;
        public string type;
        public string request;
        public string program;
        public string[] args;
        public bool stopAtEntry;
        public string cwd;
        public EnvironmentEntry[] environment;
        public bool noDebug;
        public IDictionary<string, string> sourceFileMap;
    }

    public abstract class LaunchCommand<T> : Command<T>
        where T : LaunchCommandArgs, new()
    {
        public LaunchCommand() : base("launch")
        {
        }

        public bool StopAtEntry
        {
            get { return this.Args.stopAtEntry; }
            set { this.Args.stopAtEntry = value; }
        }

        public EnvironmentEntry[] Environment
        {
            get { return this.Args.environment; }
            set { this.Args.environment = value; }
        }

        public IDictionary<string, string> SourceFileMap
        {
            get { return this.Args.sourceFileMap; }
            set { this.Args.sourceFileMap = value; }
        }
    }

    public class EnvironmentEntry : JsonValue
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }
}
