// © Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        /// Network address of the MI Debugger Server to connect to (example: localhost:1234).
        /// </summary>
        [JsonProperty("miDebuggerServerAddress", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MiDebuggerServerAddress { get; set; }

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
        /// Optional source file mappings passed to the debug engine. Example: '{ "/original/source/path":"/current/source/path" }'
        /// </summary>
        [JsonProperty("sourceFileMap", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, string> SourceFileMap { get; protected set; }

        /// <summary>
        /// When present, this tells the debugger to connect to a remote computer using another executable as a pipe that will relay standard input/output between VS Code and the MI-enabled debugger backend executable (such as gdb).
        /// </summary>
        [JsonProperty("pipeTransport", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PipeTransport PipeTransport { get; set; }
    }

    public partial class AttachOptions : BaseOptions
    {
        #region Public Properties for Serialization
        
        [JsonProperty("processId")]
        public Dictionary<string, object> ProcessId { get; private set; }

        #endregion

        #region Constructors

        public AttachOptions()
        {
            this.ProcessId = new Dictionary<string, object>();
            this.SourceFileMap = new Dictionary<string, string>();
        }

        public AttachOptions(string program, Dictionary<string, object> processId, string type = null, string targetArchitecture = null, string visualizerFile = null, bool? showDisplayString = null, string additionalSOLibSearchPath = null, string MIMode = null, string miDebuggerPath = null, string miDebuggerServerAddress = null, bool? filterStdout = null, bool? filterStderr = null, Dictionary<string, string> sourceFileMap = null, PipeTransport pipeTransport = null)
        {
            this.Program = program;
            this.Type = type;
            this.TargetArchitecture = targetArchitecture;
            this.VisualizerFile = visualizerFile;
            this.ShowDisplayString = showDisplayString;
            this.AdditionalSOLibSearchPath = additionalSOLibSearchPath;
            this.MIMode = MIMode;
            this.MiDebuggerPath = miDebuggerPath;
            this.MiDebuggerServerAddress = miDebuggerServerAddress;
            this.ProcessId = processId;
            this.FilterStdout = filterStdout;
            this.FilterStderr = filterStderr;
            this.SourceFileMap = sourceFileMap;
            this.PipeTransport = pipeTransport;
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
        /// One or more GDB/LLDB commands to execute in order to setup the underlying debugger. Example: "setupCommands": [ { "text": "-enable-pretty-printing", "description": "Enable GDB pretty printing", "ignoreFailures": true }].
        /// </summary>
        [JsonProperty("setupCommands", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<SetupCommand> SetupCommands { get; private set; }

        /// <summary>
        /// If provided, this replaces the default commands used to launch a target with some other commands. For example, this can be "-target-attach" in order to attach to a target process. An empty command list replaces the launch commands with nothing, which can be useful if the debugger is being provided launch options as command line options. Example: "customLaunchSetupCommands": [ { "text": "target-run", "description": "run target", "ignoreFailures": false }].
        /// </summary>
        [JsonProperty("customLaunchSetupCommands", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<SetupCommand> CustomLaunchSetupCommands { get; private set; }

        /// <summary>
        /// The command to execute after the debugger is fully setup in order to cause the target process to run. Allowed values are "exec-run", "exec-continue", "None". The default value is "exec-run".
        /// </summary>
        [JsonProperty("launchCompleteCommand", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public LaunchCompleteCommandValue? LaunchCompleteCommand { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum LaunchCompleteCommandValue
        {
            [EnumMember(Value = "exec-run")]
            Exec_run,
            [EnumMember(Value = "exec-continue")]
            Exec_continue,
            [EnumMember(Value = "None")]
            None,
        }

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
        /// Optional full path to a core dump file for the specified program. Defaults to null.
        /// </summary>
        [JsonProperty("coreDumpPath", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CoreDumpPath { get; set; }

        /// <summary>
        /// If true, a console is launched for the debuggee. If false, no console is launched. Note this option is ignored in some cases for technical reasons.
        /// </summary>
        [JsonProperty("externalConsole", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? ExternalConsole { get; set; }

        #endregion

        #region Constructors

        public LaunchOptions()
        {
            this.Args = new List<string>();
            this.SetupCommands = new List<SetupCommand>();
            this.CustomLaunchSetupCommands = new List<SetupCommand>();
            this.Environment = new List<Environment>();
            this.SourceFileMap = new Dictionary<string, string>();
        }

        public LaunchOptions(string program, List<string> args = null, string type = null, string targetArchitecture = null, string cwd = null, List<SetupCommand> setupCommands = null, List<SetupCommand> customLaunchSetupCommands = null, LaunchCompleteCommandValue? launchCompleteCommand = null, string visualizerFile = null, bool? showDisplayString = null, List<Environment> environment = null, string additionalSOLibSearchPath = null, string MIMode = null, string miDebuggerPath = null, string miDebuggerServerAddress = null, bool? stopAtEntry = null, string debugServerPath = null, string debugServerArgs = null, string serverStarted = null, bool? filterStdout = null, bool? filterStderr = null, int? serverLaunchTimeout = null, string coreDumpPath = null, bool? externalConsole = null, Dictionary<string, string> sourceFileMap = null, PipeTransport pipeTransport = null)
        {
            this.Program = program;
            this.Args = args;
            this.Type = type;
            this.TargetArchitecture = targetArchitecture;
            this.Cwd = cwd;
            this.SetupCommands = setupCommands;
            this.CustomLaunchSetupCommands = customLaunchSetupCommands;
            this.LaunchCompleteCommand = launchCompleteCommand;
            this.VisualizerFile = visualizerFile;
            this.ShowDisplayString = showDisplayString;
            this.Environment = environment;
            this.AdditionalSOLibSearchPath = additionalSOLibSearchPath;
            this.MIMode = MIMode;
            this.MiDebuggerPath = miDebuggerPath;
            this.MiDebuggerServerAddress = miDebuggerServerAddress;
            this.StopAtEntry = stopAtEntry;
            this.DebugServerPath = debugServerPath;
            this.DebugServerArgs = debugServerArgs;
            this.ServerStarted = serverStarted;
            this.FilterStdout = filterStdout;
            this.FilterStderr = filterStderr;
            this.ServerLaunchTimeout = serverLaunchTimeout;
            this.CoreDumpPath = coreDumpPath;
            this.ExternalConsole = externalConsole;
            this.SourceFileMap = sourceFileMap;
            this.PipeTransport = pipeTransport;
        }

        #endregion
    }

    public partial class PipeTransport
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
        /// The full path to the debugger on the target machine, for example /usr/bin/gdb.
        /// </summary>
        [JsonProperty("debuggerPath", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DebuggerPath { get; set; }

        /// <summary>
        /// Environment variables passed to the pipe program.
        /// </summary>
        [JsonProperty("pipeEnv", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, object> PipeEnv { get; private set; }

        #endregion

        #region Constructors

        public PipeTransport()
        {
            this.PipeArgs = new List<string>();
            this.PipeEnv = new Dictionary<string, object>();
        }

        public PipeTransport(string pipeCwd = null, string pipeProgram = null, List<string> pipeArgs = null, string debuggerPath = null, Dictionary<string, object> pipeEnv = null)
        {
            this.PipeCwd = pipeCwd;
            this.PipeProgram = pipeProgram;
            this.PipeArgs = pipeArgs;
            this.DebuggerPath = debuggerPath;
            this.PipeEnv = pipeEnv;
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
}
