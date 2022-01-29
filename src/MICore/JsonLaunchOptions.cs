// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MICore.Json.LaunchOptions
{
    public abstract partial class BaseOptions
    {
        /// <summary>
        /// Semicolon separated list of directories to use to search for .so files. Example: "c:\dir1;c:\dir2".
        /// </summary>
        [JsonProperty("additionalSOLibSearchPath", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AdditionalSOLibSearchPath { get; set; }

        /// <summary>
        /// Full path to program executable.
        /// </summary>
        [JsonProperty("program")]
        public string Program { get; set; }

        /// <summary>
        /// The type of the engine. Must be "cppdbg".
        /// </summary>
        [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Type { get; set; }

        /// <summary>
        /// The architecture of the debuggee. This will automatically be detected unless this parameter is set. Allowed values are x86, arm, arm64, mips, x64, amd64, x86_64.
        /// </summary>
        [JsonProperty("targetArchitecture", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TargetArchitecture { get; set; }

        /// <summary>
        /// .natvis file to be used when debugging this process. This option is not compatible with GDB pretty printing. Please also see "showDisplayString" if using this setting.
        /// </summary>
        [JsonProperty("visualizerFile", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string VisualizerFile { get; set; }

        /// <summary>
        /// When a visualizerFile is specified, showDisplayString will enable the display string. Turning this option on can cause slower performance during debugging.
        /// </summary>
        [JsonProperty("showDisplayString", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? ShowDisplayString { get; set; }

        /// <summary>
        /// Indicates the console debugger that the MIDebugEngine will connect to. Allowed values are "gdb" "lldb".
        /// </summary>
        [JsonProperty("MIMode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MIMode { get; set; }

        /// <summary>
        /// The path to the mi debugger (such as gdb). When unspecified, it will search path first for the debugger.
        /// </summary>
        [JsonProperty("miDebuggerPath", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MiDebuggerPath { get; set; }

        /// <summary>
        /// Arguments for the mi debugger.
        /// </summary>
        [JsonProperty("miDebuggerArgs", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MiDebuggerArgs { get; set; }

        /// <summary>
        /// Network address of the MI Debugger Server to connect to (example: localhost:1234).
        /// </summary>
        [JsonProperty("miDebuggerServerAddress", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MiDebuggerServerAddress { get; set; }

        /// <summary>
        /// If true, use gdb extended-remote mode to connect to gdbserver.
        /// </summary>
        [JsonProperty("useExtendedRemote", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? UseExtendedRemote { get; set; }

        /// <summary>
        /// Optional source file mappings passed to the debug engine. Example: '{ "/original/source/path":"/current/source/path" }'
        /// </summary>
        [JsonProperty("sourceFileMap", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, object> SourceFileMap { get; protected set; }

        /// <summary>
        /// When present, this tells the debugger to connect to a remote computer using another executable as a pipe that will relay standard input/output between VS Code and the MI-enabled debugger backend executable (such as gdb).
        /// </summary>
        [JsonProperty("pipeTransport", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PipeTransport PipeTransport { get; set; }

        /// <summary>
        /// Supports explcit control of symbol loading. The processing of Exceptions lists and symserver entries.
        /// </summary>
        [JsonProperty("symbolLoadInfo", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public SymbolLoadInfo SymbolLoadInfo { get; set; }

        /// <summary>
        /// One or more GDB/LLDB commands to execute in order to setup the underlying debugger. Example: "setupCommands": [ { "text": "-enable-pretty-printing", "description": "Enable GDB pretty printing", "ignoreFailures": true }].
        /// </summary>
        [JsonProperty("setupCommands", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<SetupCommand> SetupCommands { get; protected set; }

        /// <summary>
        /// One or more commands to execute in order to setup underlying debugger after debugger has been attached. i.e. flashing and resetting the board
        /// </summary>
        [JsonProperty("postRemoteConnectCommands", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<SetupCommand> PostRemoteConnectCommands { get; protected set; }

        /// <summary>
        /// Explicitly control whether hardware breakpoints are used. If an optional limit is provided, additionally restrict the number of hardware breakpoints for remote targets. Example: "hardwareBreakpoints": { "require": true, "limit": 5 }.
        /// </summary>
        [JsonProperty("hardwareBreakpoints", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public HardwareBreakpointInfo HardwareBreakpointInfo { get; set; }
    }

    public partial class AttachOptions : BaseOptions
    {
        #region Public Properties for Serialization

        [JsonProperty("processId")]
        public int ProcessId { get; private set; }

        #endregion

        #region Constructors

        public AttachOptions()
        {
            this.SourceFileMap = new Dictionary<string, object>();
        }

        public AttachOptions(
            string program,
            int processId,
            string type = null,
            string targetArchitecture = null,
            string visualizerFile = null,
            bool? showDisplayString = null,
            string additionalSOLibSearchPath = null,
            string MIMode = null,
            string miDebuggerPath = null,
            string miDebuggerArgs = null,
            string miDebuggerServerAddress = null,
            bool? useExtendedRemote = null,
            HardwareBreakpointInfo hardwareBreakpointInfo = null,
            Dictionary<string, object> sourceFileMap = null,
            PipeTransport pipeTransport = null,
            SymbolLoadInfo symbolLoadInfo = null)
        {
            this.Program = program;
            this.Type = type;
            this.TargetArchitecture = targetArchitecture;
            this.VisualizerFile = visualizerFile;
            this.ShowDisplayString = showDisplayString;
            this.AdditionalSOLibSearchPath = additionalSOLibSearchPath;
            this.MIMode = MIMode;
            this.MiDebuggerPath = miDebuggerPath;
            this.MiDebuggerArgs = miDebuggerArgs;
            this.MiDebuggerServerAddress = miDebuggerServerAddress;
            this.UseExtendedRemote = useExtendedRemote;
            this.ProcessId = processId;
            this.HardwareBreakpointInfo = hardwareBreakpointInfo;
            this.SourceFileMap = sourceFileMap;
            this.PipeTransport = pipeTransport;
            this.SymbolLoadInfo = symbolLoadInfo;
        }

        #endregion
    }

    public partial class Environment
    {
        #region Public Properties for Serialization

        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("value", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Value { get; set; }

        #endregion

        #region Constructors

        public Environment()
        {
        }

        public Environment(string name = null, string value = null)
        {
            this.Name = name;
            this.Value = value;
        }

        #endregion
    }

    public partial class SymbolLoadInfo
    {
        #region Public Properties for Serialization

        /// <summary>
        /// If true, symbols for all libs will be loaded, otherwise no solib symbols will be loaded. Modified by ExceptionList. Default value is true.
        /// </summary>
        [JsonProperty("loadAll", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? LoadAll { get; set; }

        /// <summary>
        /// List of filenames (wildcards allowed). Modifies behavior of LoadAll. 
        /// If LoadAll is true then don't load symbols for libs that match any name in the list. 
        /// Otherwise only load symbols for libs that match.
        /// </summary>
        [JsonProperty("exceptionList", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ExceptionList { get; set; }

        #endregion

        #region Constructors

        public SymbolLoadInfo()
        {
        }

        public SymbolLoadInfo(bool? loadAll = null, string exceptionList = null)
        {
            this.LoadAll = loadAll;
            this.ExceptionList = exceptionList;
        }

        #endregion
    }

    public partial class HardwareBreakpointInfo
    {
        #region Public Properties for Serialization

        /// <summary>
        /// If true, always use hardware breakpoints. Default value is false.
        /// </summary>
        [JsonProperty("require")]
        public bool Require { get; set; }

        /// <summary>
        /// When <see cref="Require"/> is true, restrict the number of available hardware breakpoints. Default is 0, in which case there is no limit. This setting is only enforced with remote GDB targets.
        /// </summary>
        [JsonProperty("limit", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? Limit { get; set; }

        #endregion

        #region Constructors

        public HardwareBreakpointInfo()
        {
        }

        public HardwareBreakpointInfo(bool require = false, int? limit = null)
        {
            this.Require = require;
            this.Limit = limit;
        }

        #endregion
    }

    public partial class LaunchOptions : BaseOptions
    {
        #region Public Properties for Serialization

        /// <summary>
        /// Command line arguments passed to the program.
        /// </summary>
        [JsonProperty("args", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<string> Args { get; private set; }

        /// <summary>
        /// The working directory of the target
        /// </summary>
        [JsonProperty("cwd", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Cwd { get; set; }

        /// <summary>
        /// If provided, this replaces the default commands used to launch a target with some other commands. For example, this can be "-target-attach" in order to attach to a target process. An empty command list replaces the launch commands with nothing, which can be useful if the debugger is being provided launch options as command line options. Example: "customLaunchSetupCommands": [ { "text": "target-run", "description": "run target", "ignoreFailures": false }].
        /// </summary>
        [JsonProperty("customLaunchSetupCommands", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<SetupCommand> CustomLaunchSetupCommands { get; private set; }

        /// <summary>
        /// The command to execute after the debugger is fully setup in order to cause the target process to run. Allowed values are "exec-run", "exec-continue", "None". The default value is "exec-run".
        /// </summary>
        [JsonProperty("launchCompleteCommand", DefaultValueHandling = DefaultValueHandling.Ignore),
        JsonConverter(typeof(LaunchCompleteCommandConverter))]
        public LaunchCompleteCommand? LaunchCompleteCommand { get; set; }
        
        /// <summary>
        /// Environment variables to add to the environment for the program. Example: [ { "name": "squid", "value": "clam" } ].
        /// </summary>
        [JsonProperty("environment", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<Environment> Environment { get; private set; }

        /// <summary>
        /// Optional parameter. If true, the debugger should stop at the entrypoint of the target. If processId is passed, has no effect.
        /// </summary>
        [JsonProperty("stopAtEntry", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? StopAtEntry { get; set; }

        /// <summary>
        /// Optional full path to debug server to launch. Defaults to null.
        /// </summary>
        [JsonProperty("debugServerPath", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DebugServerPath { get; set; }

        /// <summary>
        /// Optional debug server args. Defaults to null.
        /// </summary>
        [JsonProperty("debugServerArgs", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DebugServerArgs { get; set; }

        /// <summary>
        /// Optional server-started pattern to look for in the debug server output. Defaults to null.
        /// </summary>
        [JsonProperty("serverStarted", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ServerStarted { get; set; }

        /// <summary>
        /// Optional time, in milliseconds, for the debugger to wait for the debugServer to start up. Default is 10000.
        /// </summary>
        [JsonProperty("serverLaunchTimeout", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? ServerLaunchTimeout { get; set; }

        /// <summary>
        /// Search stdout stream for server-started pattern and log stdout to debug output. Defaults to true.
        /// </summary>
        [JsonProperty("filterStdout", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? FilterStdout { get; set; }

        /// <summary>
        /// Search stderr stream for server-started pattern and log stderr to debug output. Defaults to false.
        /// </summary>
        [JsonProperty("filterStderr", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? FilterStderr { get; set; }

        /// <summary>
        /// Optional full path to a core dump file for the specified program. Defaults to null.
        /// </summary>
        [JsonProperty("coreDumpPath", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CoreDumpPath { get; set; }

        /// <summary>
        /// If true, a console is launched for the debuggee. If false, no console is launched. Note this option is ignored in some cases for technical reasons.
        /// </summary>
        [JsonProperty("externalConsole", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? ExternalConsole { get; set; }

        /// <summary>
        /// If true, disables debuggee console redirection that is required for Integrated Terminal support.
        /// </summary>
        [JsonProperty("avoidWindowsConsoleRedirection", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? AvoidWindowsConsoleRedirection { get; set; }

        /// <summary>
        /// Optional parameter. If true, the debugger should stop after connecting to the target.
        /// </summary>
        [JsonProperty("stopAtConnect", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? StopAtConnect { get; set; }

        #endregion

        #region Constructors

        public LaunchOptions()
        {
            this.Args = new List<string>();
            this.SetupCommands = new List<SetupCommand>();
            this.PostRemoteConnectCommands = new List<SetupCommand>();
            this.CustomLaunchSetupCommands = new List<SetupCommand>();
            this.Environment = new List<Environment>();
            this.SourceFileMap = new Dictionary<string, object>();
        }

        public LaunchOptions(
            string program,
            List<string> args = null,
            string type = null,
            string targetArchitecture = null,
            string cwd = null,
            List<SetupCommand> setupCommands = null,
            List<SetupCommand> postRemoteConnectCommands = null,
            List<SetupCommand> customLaunchSetupCommands = null,
            LaunchCompleteCommand? launchCompleteCommand = null,
            string visualizerFile = null,
            bool? showDisplayString = null,
            List<Environment> environment = null,
            string additionalSOLibSearchPath = null,
            string MIMode = null,
            string miDebuggerPath = null,
            string miDebuggerArgs = null,
            string miDebuggerServerAddress = null,
            bool? useExtendedRemote = null,
            bool? stopAtEntry = null,
            string debugServerPath = null,
            string debugServerArgs = null,
            string serverStarted = null,
            bool? filterStdout = null,
            bool? filterStderr = null,
            int? serverLaunchTimeout = null,
            string coreDumpPath = null,
            bool? externalConsole = null,
            HardwareBreakpointInfo hardwareBreakpointInfo = null,
            Dictionary<string, object> sourceFileMap = null,
            PipeTransport pipeTransport = null,
            bool? stopAtConnect = null)
        {
            this.Program = program;
            this.Args = args;
            this.Type = type;
            this.TargetArchitecture = targetArchitecture;
            this.Cwd = cwd;
            this.SetupCommands = setupCommands;
            this.PostRemoteConnectCommands = postRemoteConnectCommands;
            this.CustomLaunchSetupCommands = customLaunchSetupCommands;
            this.LaunchCompleteCommand = launchCompleteCommand;
            this.VisualizerFile = visualizerFile;
            this.ShowDisplayString = showDisplayString;
            this.Environment = environment;
            this.AdditionalSOLibSearchPath = additionalSOLibSearchPath;
            this.MIMode = MIMode;
            this.MiDebuggerPath = miDebuggerPath;
            this.MiDebuggerArgs = miDebuggerArgs;
            this.MiDebuggerServerAddress = miDebuggerServerAddress;
            this.UseExtendedRemote = useExtendedRemote;
            this.StopAtEntry = stopAtEntry;
            this.DebugServerPath = debugServerPath;
            this.DebugServerArgs = debugServerArgs;
            this.ServerStarted = serverStarted;
            this.FilterStdout = filterStdout;
            this.FilterStderr = filterStderr;
            this.ServerLaunchTimeout = serverLaunchTimeout;
            this.CoreDumpPath = coreDumpPath;
            this.ExternalConsole = externalConsole;
            this.HardwareBreakpointInfo = hardwareBreakpointInfo;
            this.SourceFileMap = sourceFileMap;
            this.PipeTransport = pipeTransport;
            this.StopAtConnect = stopAtConnect;
        }

        #endregion

        #region Private class
        /// <summary>
        /// Custom converter to avoid dependency on System.Runtime.Serialization.Primitives.dll
        /// </summary>
        private class LaunchCompleteCommandConverter : JsonConverter
        {
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (objectType == typeof(LaunchCompleteCommand?) && reader.TokenType == JsonToken.String)
                {
                    String value = reader.Value.ToString();
                    if (value.Equals("exec-continue", StringComparison.Ordinal))
                    {
                        return MICore.LaunchCompleteCommand.ExecContinue;
                    }
                    if (value.Equals("exec-run", StringComparison.Ordinal))
                    {
                        return MICore.LaunchCompleteCommand.ExecRun;
                    }
                    if (value.Equals("None", StringComparison.Ordinal))
                    {
                        return MICore.LaunchCompleteCommand.None;
                    }

                    throw new InvalidLaunchOptionsException(String.Format(CultureInfo.CurrentCulture, MICoreResources.Error_InvalidLaunchCompleteCommandValue, reader.Value));
                }

                Debug.Fail(String.Format(CultureInfo.CurrentCulture, "Unexpected objectType '{0}' passed for launchCompleteCommand serialization.", objectType.ToString()));
                return null;
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(LaunchCompleteCommand?);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }

    public partial class PipeTransport : PipeTransportOptions
    {
        #region Public Properties for Serialization

        /// <summary>
        /// When present, this tells the debugger override the PipeTransport's fields if the client's current platform is Windows and the field is defined in this configuration.
        /// </summary>
        [JsonProperty("windows", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PipeTransportOptions Windows { get; private set; }

        /// <summary>
        /// When present, this tells the debugger override the PipeTransport's fields if the client's current platform is OSX and the field is defined in this configuration.
        /// </summary>
        [JsonProperty("osx", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PipeTransportOptions OSX { get; private set; }

        /// <summary>
        /// When present, this tells the debugger override the PipeTransport's fields if the client's current platform is Linux and the field is defined in this configuration.
        /// </summary>
        [JsonProperty("linux", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PipeTransportOptions Linux { get; private set; }

        #endregion

        #region Constructors

        public PipeTransport()
        {

        }

        public PipeTransport(PipeTransportOptions windows = null, PipeTransportOptions osx = null, PipeTransportOptions linux = null)
        {
            this.Windows = windows;
            this.OSX = osx;
            this.Linux = linux;
        }

        #endregion
    }


    public partial class PipeTransportOptions
    {
        #region Public Properties for Serialization

        /// <summary>
        /// The fully qualified path to the working directory for the pipe program.
        /// </summary>
        [JsonProperty("pipeCwd", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PipeCwd { get; set; }

        /// <summary>
        /// The fully qualified pipe command to execute.
        /// </summary>
        [JsonProperty("pipeProgram", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PipeProgram { get; set; }

        /// <summary>
        /// Command line arguments passed to the pipe program to configure the connection.
        /// </summary>
        [JsonProperty("pipeArgs", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<string> PipeArgs { get; private set; }

        /// <summary>
        /// Command line arguments passed to the pipe program to execute a remote command.
        /// </summary>
        [JsonProperty("pipeCmd", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<string> PipeCmd { get; private set; }

        /// <summary>
        /// The full path to the debugger on the target machine, for example /usr/bin/gdb.
        /// </summary>
        [JsonProperty("debuggerPath", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DebuggerPath { get; set; }

        /// <summary>
        /// Environment variables passed to the pipe program.
        /// </summary>
        [JsonProperty("pipeEnv", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, string> PipeEnv { get; private set; }

        /// <summary>
        /// Should arguments that contain characters that need to be quoted (example: spaces) be quoted? Defaults to 'true'. If set to false, the debugger command will no longer be automatically quoted.
        /// </summary>
        [JsonProperty("quoteArgs", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? QuoteArgs { get; private set; }

        #endregion

        #region Constructors

        public PipeTransportOptions()
        {
            this.PipeArgs = new List<string>();
            this.PipeEnv = new Dictionary<string, string>();
        }

        public PipeTransportOptions(string pipeCwd = null, string pipeProgram = null, List<string> pipeArgs = null, string debuggerPath = null, Dictionary<string, string> pipeEnv = null, bool? quoteArgs = null)
        {
            this.PipeCwd = pipeCwd;
            this.PipeProgram = pipeProgram;
            this.PipeArgs = pipeArgs;
            this.DebuggerPath = debuggerPath;
            this.PipeEnv = pipeEnv;
            this.QuoteArgs = quoteArgs;
        }

        #endregion
    }

    public partial class SetupCommand
    {
        #region Public Properties for Serialization

        /// <summary>
        /// The debugger command to execute.
        /// </summary>
        [JsonProperty("text", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Text { get; set; }

        /// <summary>
        /// Optional description for the command.
        /// </summary>
        [JsonProperty("description", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Description { get; set; }

        /// <summary>
        /// If true, failures from the command should be ignored. Default value is false.
        /// </summary>
        [JsonProperty("ignoreFailures", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? IgnoreFailures { get; set; }

        #endregion

        #region Constructors

        public SetupCommand()
        {
        }

        public SetupCommand(string text = null, string description = null, bool? ignoreFailures = null)
        {
            this.Text = text;
            this.Description = description;
            this.IgnoreFailures = ignoreFailures;
        }

        #endregion
    }

    public partial class SourceFileMapOptions
    {
        #region Public Properties for Serialization

        /// <summary>
        /// The editor's path.
        /// </summary>
        [JsonProperty("editorPath", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string EditorPath { get; set; }

        /// <summary>
        /// Use this source mapping for breakpoint binding? Default is true.
        /// </summary>
        [JsonProperty("useForBreakpoints", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? UseForBreakpoints { get; set; }

        #endregion

        #region Constructors

        public SourceFileMapOptions()
        {
        }

        public SourceFileMapOptions(string editorPath = null, bool? useForBreakpoints = null)
        {
            this.EditorPath = editorPath;
            this.UseForBreakpoints = useForBreakpoints;
        }

        #endregion
    }

    public static class LaunchOptionHelpers
    {
        public static BaseOptions GetLaunchOrAttachOptions(JObject parsedJObject)
        {
            BaseOptions baseOptions = null;
            string requestType = parsedJObject["request"]?.Value<string>();
            if (String.IsNullOrWhiteSpace(requestType))
            {
                // If request isn't specified, see if we can determine what it is
                if (!String.IsNullOrWhiteSpace(parsedJObject["processId"]?.Value<string>()))
                {
                    requestType = "attach";
                }
                else if (!String.IsNullOrWhiteSpace(parsedJObject["program"]?.Value<string>()))
                {
                    requestType = "launch";
                }
                else
                {
                    throw new InvalidLaunchOptionsException(String.Format(CultureInfo.CurrentCulture, MICoreResources.Error_BadRequiredAttribute, "program"));
                }
            }

            switch (requestType)
            {
                case "launch":
                    // handle launch case
                    baseOptions = parsedJObject.ToObject<Json.LaunchOptions.LaunchOptions>();
                    break;
                case "attach":
                    // handle attach case
                    baseOptions = parsedJObject.ToObject<Json.LaunchOptions.AttachOptions>();
                    break;
                default:
                    throw new InvalidLaunchOptionsException(String.Format(CultureInfo.CurrentCulture, MICoreResources.Error_BadRequiredAttribute, "request"));
            }

            return baseOptions;
        }
    }
}
