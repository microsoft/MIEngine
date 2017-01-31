// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Xml;
using System.Globalization;
using System.Net.Security;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.Diagnostics;
using Microsoft.DebugEngineHost;
using MICore.Xml.LaunchOptions;
using System.Reflection;
using Microsoft.VisualStudio.Debugger.Interop;

namespace MICore
{
    public enum TargetArchitecture
    {
        Unknown,
        ARM,
        ARM64,
        X86,
        X64,
        Mips
    };

    public enum TargetEngine
    {
        Unknown,
        Native,
        Java,
    }

    public enum LaunchCompleteCommand
    {
        /// <summary>
        /// Execute the 'exec-run' MI command which will spawn a new process and begin executing it.
        /// This is the default value.
        /// </summary>
        ExecRun,

        /// <summary>
        /// Execute the 'exec-continue' MI command which will resume from stopped state. This is useful if
        /// the result of setting up the debugger is that the debuggee is in break state.
        /// </summary>
        ExecContinue,

        /// <summary>
        /// No command should be executed. This is useful if the target is already ready to go.
        /// </summary>
        None,
    };

    /// <summary>
    /// Launch options when connecting to an instance of an MI Debugger running on a remote device through a shell
    /// </summary>
    public sealed class PipeLaunchOptions : LaunchOptions
    {
        /// <summary>
        /// Creates an instance of PipeLaunchOptions
        /// </summary>
        /// <param name="pipePath">Path of the pipe program</param>
        /// <param name="pipeArguments">Argument to the pipe program</param>
        /// <param name="pipeCommandArguments">Command to be invoked on the pipe program</param>
        /// <param name="pipeCwd">Current working directory of pipe program. If empty directory of the pipePath is set as the cwd.</param>
        /// <param name="pipeEnvironment">Environment variables set before invoking the pipe program</param>
        public PipeLaunchOptions(string pipePath, string pipeArguments, string pipeCommandArguments, string pipeCwd, MICore.Xml.LaunchOptions.EnvironmentEntry[] pipeEnvironment)
        {
            if (string.IsNullOrEmpty(pipePath))
                throw new ArgumentNullException("PipePath");

            this.PipePath = pipePath;
            this.PipeArguments = pipeArguments;
            this.PipeCommandArguments = pipeCommandArguments;
            this.PipeCwd = pipeCwd;

            this.PipeEnvironment = (pipeEnvironment != null) ? pipeEnvironment.Select(e => new EnvironmentEntry(e)).ToArray() : new EnvironmentEntry[] { };
        }

        static internal PipeLaunchOptions CreateFromXml(Xml.LaunchOptions.PipeLaunchOptions source)
        {
            var options = new PipeLaunchOptions(RequireAttribute(source.PipePath, "PipePath"), source.PipeArguments, source.PipeCommandArguments, source.PipeCwd, source.PipeEnvironment);
            options.InitializeCommonOptions(source);

            return options;
        }

        /// <summary>
        /// [Required] Path to the pipe executable.
        /// </summary>
        public string PipePath { get; private set; }

        /// <summary>
        /// [Optional] Arguments to pass to the pipe executable.
        /// </summary>
        /// 
        public string PipeArguments { get; private set; }

        /// <summary>
        /// [Optional] Arguments to pass to the PipePath program that include a format specifier ('{0}') for a custom command.
        /// </summary>
        public string PipeCommandArguments { get; private set; }

        /// <summary>
        /// [Optional] Current working directory when the pipe program is invoked.
        /// </summary>
        public string PipeCwd { get; private set; }

        /// <summary>
        /// [Optional] Enviroment variables for the pipe program.
        /// </summary>
        public IReadOnlyCollection<EnvironmentEntry> PipeEnvironment { get; private set; }
    }

    public sealed class TcpLaunchOptions : LaunchOptions
    {
        public TcpLaunchOptions(string hostname, int port, bool secure)
        {
            if (string.IsNullOrEmpty(hostname))
            {
                throw new ArgumentException("hostname");
            }
            if (port <= 0)
            {
                throw new ArgumentException("port");
            }

            this.Hostname = hostname;
            this.Port = port;
            this.Secure = secure;
            this.ServerCertificateValidationCallback = null;
        }

        static internal TcpLaunchOptions CreateFromXml(Xml.LaunchOptions.TcpLaunchOptions source)
        {
            var options = new TcpLaunchOptions(RequireAttribute(source.Hostname, "Hostname"), LaunchOptions.RequirePortAttribute(source.Port, "Port"), source.Secure);
            options.InitializeCommonOptions(source);

            return options;
        }

        public string Hostname { get; private set; }
        public int Port { get; private set; }
        public bool Secure { get; private set; }
        public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }
    }

    public sealed class EnvironmentEntry
    {
        public EnvironmentEntry(Xml.LaunchOptions.EnvironmentEntry xmlEntry)
        {
            this.Name = xmlEntry.Name;
            this.Value = xmlEntry.Value;
        }

        /// <summary>
        /// [Required] Name of the environment variable
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// [Required] Value of the environment variable
        /// </summary>
        public string Value { get; private set; }
    }

    public sealed class SourceMapEntry
    {
        public SourceMapEntry() // used by launchers 
        {
        }

        public SourceMapEntry(Xml.LaunchOptions.SourceMapEntry xmlEntry)
        {
            this.EditorPath = xmlEntry.EditorPath;
            this.CompileTimePath = xmlEntry.CompileTimePath;
            this.UseForBreakpoints = xmlEntry.UseForBreakpoints;
        }

        private string _editorPath;
        public string EditorPath
        {
            get
            {
                return _editorPath;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("EditorPath");
                }
                this._editorPath = value;
            }
        }


        private string _compileTimePath;
        public string CompileTimePath
        {
            get
            {
                return _compileTimePath;
            }
            set
            {
                _compileTimePath = value == null ? string.Empty : value;
            }
        }
        public bool UseForBreakpoints { get; set; }

        public static ReadOnlyCollection<SourceMapEntry> CreateCollectionFromXml(Xml.LaunchOptions.SourceMapEntry[] source)
        {
            SourceMapEntry[] pathArray = source?.Select(x => new SourceMapEntry(x)).ToArray();
            if (pathArray == null)
            {
                pathArray = new SourceMapEntry[0];
            }

            return new ReadOnlyCollection<SourceMapEntry>(pathArray);
        }
    }

    /// <summary>
    /// Launch options class when VS should launch an instance of an MI Debugger to connect to an MI Debugger server
    /// </summary>
    public sealed class LocalLaunchOptions : LaunchOptions
    {
        private bool _useExternalConsole;

        private const int DefaultLaunchTimeout = 10 * 1000; // 10 seconds

        public LocalLaunchOptions(string MIDebuggerPath, string MIDebuggerServerAddress, Xml.LaunchOptions.EnvironmentEntry[] environmentEntries)
        {
            if (string.IsNullOrEmpty(MIDebuggerPath))
                throw new ArgumentNullException("MIDebuggerPath");

            this.MIDebuggerPath = MIDebuggerPath;
            this.MIDebuggerServerAddress = MIDebuggerServerAddress;

            List<EnvironmentEntry> environmentList = new List<EnvironmentEntry>();
            if (environmentEntries != null)
            {
                foreach (Xml.LaunchOptions.EnvironmentEntry xmlEntry in environmentEntries)
                {
                    environmentList.Add(new EnvironmentEntry(xmlEntry));
                }
            }

            this.Environment = new ReadOnlyCollection<EnvironmentEntry>(environmentList);
        }

        private void InitializeServerOptions(Xml.LaunchOptions.LocalLaunchOptions source)
        {
            if (!String.IsNullOrWhiteSpace(source.DebugServer))
            {
                DebugServer = source.DebugServer;
                DebugServerArgs = source.DebugServerArgs;
                ServerStarted = source.ServerStarted;
                FilterStderr = source.FilterStderr;
                FilterStdout = source.FilterStdout;
                if (!FilterStderr && !FilterStdout)
                {
                    FilterStdout = true;    // no pattern source specified, use stdout
                }
                ServerLaunchTimeout = source.ServerLaunchTimeoutSpecified ? source.ServerLaunchTimeout : DefaultLaunchTimeout;
            }
        }

        /// <summary>
        /// Checks that the file path is valid, exists, and is rooted.
        /// </summary>
        public static bool CheckFilePath(string path)
        {
            return path.IndexOfAny(Path.GetInvalidPathChars()) < 0 && File.Exists(path) && Path.IsPathRooted(path);
        }

        /// <summary>
        /// Checks that if the directory path is valid, exists and is rooted.
        /// </summary>
        public static bool CheckDirectoryPath(string path)
        {
            return path.IndexOfAny(Path.GetInvalidPathChars()) < 0 && Directory.Exists(path) && Path.IsPathRooted(path);
        }

        public bool ShouldStartServer()
        {
            return !string.IsNullOrWhiteSpace(DebugServer);
        }

        public bool IsValidMiDebuggerPath()
        {
            return File.Exists(MIDebuggerPath);
        }

        static internal LocalLaunchOptions CreateFromXml(Xml.LaunchOptions.LocalLaunchOptions source)
        {
            string miDebuggerPath = source.MIDebuggerPath;

            // If no path to the debugger was specified, look for the proper binary in the user's $PATH
            if (String.IsNullOrEmpty(miDebuggerPath))
            {
                string debuggerBinary = null;
                switch (source.MIMode)
                {
                    case Xml.LaunchOptions.MIMode.gdb:
                        debuggerBinary = "gdb";
                        break;

                    case Xml.LaunchOptions.MIMode.lldb:
                        debuggerBinary = "lldb-mi";
                        break;
                }

                if (!String.IsNullOrEmpty(debuggerBinary))
                {
                    miDebuggerPath = LocalLaunchOptions.ResolveFromPath(debuggerBinary);
                }

                if (String.IsNullOrEmpty(miDebuggerPath))
                {
                    throw new InvalidLaunchOptionsException(MICoreResources.Error_NoMiDebuggerPath);
                }
            }

            var options = new LocalLaunchOptions(
                RequireAttribute(miDebuggerPath, "MIDebuggerPath"),
                source.MIDebuggerServerAddress,
                source.Environment);
            options.InitializeCommonOptions(source);
            options.InitializeServerOptions(source);
            options._useExternalConsole = source.ExternalConsole;

            // when using local options the core dump path must check out
            if (options.IsCoreDump && !LocalLaunchOptions.CheckFilePath(options.CoreDumpPath))
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, MICoreResources.Error_InvalidLocalExePath, options.CoreDumpPath));

            return options;
        }

        private static string ResolveFromPath(string command)
        {
            string pathVar = System.Environment.GetEnvironmentVariable("PATH");

            // Check each portion of the PATH environment variable to see if it contains the requested file
            foreach (string pathPart in pathVar.Split(Path.PathSeparator))
            {
                string candidate = Path.Combine(pathPart, command);

                // If the file exists, use it
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// [Required] Path to the MI Debugger Executable.
        /// </summary>
        public string MIDebuggerPath { get; private set; }

        /// <summary>
        /// [Optional] Server address that MI Debugger server is listening to
        /// </summary>
        public string MIDebuggerServerAddress { get; private set; }

        /// <summary>
        /// [Optional] List of environment variables to add to the launched process
        /// </summary>
        public ReadOnlyCollection<EnvironmentEntry> Environment { get; private set; }

        /// <summary>
        /// [Optional] MI Debugger Server exe, if non-null then the MIEngine will start the debug server before starting the debugger
        /// </summary>
        public string DebugServer { get; private set; }

        /// <summary>
        /// [Optional] Args for MI Debugger Server exe
        /// </summary>
        public string DebugServerArgs { get; private set; }

        /// <summary>
        /// [Optional] Server started pattern (in Regex format)
        /// </summary>
        public string ServerStarted { get; private set; }

        /// <summary>
        /// [Optional] Log strings written to stderr and examine for server started pattern
        /// </summary>
        public bool FilterStderr { get; private set; }

        /// <summary>
        /// [Optional] Log strings written to stdout and examine for server started pattern
        /// </summary>
        public bool FilterStdout { get; private set; }

        /// <summary>
        /// [Optional] Log strings written to stderr and examine for server started pattern
        /// </summary>
        public int ServerLaunchTimeout { get; private set; }

        public bool UseExternalConsole
        {
            get { return _useExternalConsole; }
        }
    }

    public sealed class SourceRoot
    {
        public SourceRoot(string path, bool recursive)
        {
            Path = path;
            RecursiveSearchEnabled = recursive;
        }

        public string Path { get; private set; }

        public bool RecursiveSearchEnabled { get; private set; }
    }

    public sealed class JavaLaunchOptions : LaunchOptions
    {
        /// <summary>
        /// Creates an instance of JavaLaunchOptions. This is used to send information to the Java Debugger.
        /// </summary>
        /// <param name="jvmHost">Java Virtual Machine host.</param>
        /// <param name="jvmPort">Java Virtual Machine port.</param>
        /// <param name="sourceRoots">Source roots.</param>
        /// <param name="processName">Logical name of the process. Usually indicates the name of the activity.</param>
        public JavaLaunchOptions(string jvmHost, int jvmPort, SourceRoot[] sourceRoots, string processName)
        {
            JVMHost = jvmHost;
            JVMPort = jvmPort;
            SourceRoots = new ReadOnlyCollection<SourceRoot>(sourceRoots);
            ProcessName = processName;
        }

        public string JVMHost { get; private set; }

        public int JVMPort { get; private set; }

        public ReadOnlyCollection<SourceRoot> SourceRoots { get; private set; }

        public string ProcessName { get; private set; }
    }

    /// <summary>
    /// Launch options used when launching through IDebugUnixShellPort (SSH, and possible other things in the future).
    /// </summary>
    public sealed class UnixShellPortLaunchOptions : LaunchOptions
    {
        public string StartRemoteDebuggerCommand { get; private set; }
        public Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier.IDebugUnixShellPort UnixPort { get; private set; }

        /// <summary>
        /// After a successful launch of ClrDbg from a VS session, the subsequent debugging in the same VS will not attempt to download ClrDbg on the remote machine.
        /// This keeps track of the ports where the debugger was successfully launched.
        /// </summary>
        private static HashSet<string> s_LaunchSuccessSet = new HashSet<string>();

        /// <summary>
        /// Url to get the GetClrDbg.sh script from.
        /// </summary>
        public string GetClrDbgUrl { get; private set; } = "https://aka.ms/getclrdbgsh";

        /// <summary>
        /// Default location of the debugger on the remote machine.
        /// </summary>
        public string DebuggerInstallationDirectory { get; private set; } = ".vs-debugger";

        /// <summary>
        /// Meta version of the clrdbg.
        /// </summary>
        /// TODO: rajkumar42, placeholder. Needs to be fixed in the pkgdef as well.
        public string ClrDbgVersion { get; private set; } = "vs2015u2";

        /// <summary>
        /// Sub directory where the clrdbg should be downloaded relative to <see name="DebuggerInstallationDirectory"/>
        /// </summary>
        public string ClrDbgInstallationSubDirectory { get; private set; } = "vs2015u2";

        /// <summary>
        /// Shell command invoked after a successful launch of clrdbg. 
        /// Launches the existing clrdbg.
        /// </summary>
        /// /// <remarks>
        /// {0} - Base directory of debugger
        /// {1} - clrdbg version.
        /// {2} - Subdirectory where clrdbg should be installed.
        /// </remarks>
        private const string ClrdbgFirstLaunchCommand = "cd {0} && chmod +x ./GetClrDbg.sh && ./GetClrDbg.sh -v {1} -l {0}/{2} -d";

        /// <summary>
        /// Shell command invoked after a successful launch of clrdbg. 
        /// Launches the existing clrdbg.
        /// </summary>
        /// /// <remarks>
        /// {0} - Base directory of debugger
        /// {1} - clrdbg version.
        /// {2} - Subdirectory where clrdbg should be installed.
        /// </remarks>
        private const string ClrdbgSubsequentLaunchCommand = "cd {0} && ./GetClrDbg.sh -v {1} -l {0}/{2} -d -s";

        public UnixShellPortLaunchOptions(string startRemoteDebuggerCommand,
                Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier.IDebugUnixShellPort unixPort,
                MIMode miMode,
                BaseLaunchOptions baseLaunchOptions,
                string getClrDbgUrl = null,
                string remoteDebuggerInstallationDirectory = null,
                string remoteDebuggerInstallationSubDirectory = null,
                string clrdbgVersion = null)
        {
            this.UnixPort = unixPort;
            this.DebuggerMIMode = miMode;

            if (!string.IsNullOrWhiteSpace(getClrDbgUrl))
            {
                GetClrDbgUrl = getClrDbgUrl;
            }

            if (!string.IsNullOrWhiteSpace(remoteDebuggerInstallationDirectory))
            {
                DebuggerInstallationDirectory = remoteDebuggerInstallationDirectory;
            }

            if (!string.IsNullOrWhiteSpace(remoteDebuggerInstallationSubDirectory))
            {
                ClrDbgInstallationSubDirectory = remoteDebuggerInstallationSubDirectory;
            }

            if (!string.IsNullOrWhiteSpace(clrdbgVersion))
            {
                ClrDbgVersion = clrdbgVersion;
            }

            if (string.IsNullOrEmpty(startRemoteDebuggerCommand))
            {
                switch (miMode)
                {
                    case MIMode.Gdb:
                        startRemoteDebuggerCommand = "gdb --interpreter=mi";
                        break;
                    case MIMode.Lldb:
                        // TODO: Someday we should likely use a download script here too
                        startRemoteDebuggerCommand = "lldb-mi --interpreter=mi";
                        break;
                    case MIMode.Clrdbg:
                        string debuggerHomeDirectory;
                        if (DebuggerInstallationDirectory.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                        {
                            debuggerHomeDirectory = DebuggerInstallationDirectory;
                        }
                        else
                        {
                            string userHomeDirectory = UnixPort.GetUserHomeDirectory();
                            debuggerHomeDirectory = string.Format(CultureInfo.InvariantCulture, "{0}/{1}", userHomeDirectory, DebuggerInstallationDirectory);
                        }

                        if (!HasSuccessfulPreviousLaunch(this))
                        {
                            startRemoteDebuggerCommand = string.Format(CultureInfo.InvariantCulture, ClrdbgFirstLaunchCommand, debuggerHomeDirectory, ClrDbgVersion, ClrDbgInstallationSubDirectory);
                        }
                        else
                        {
                            startRemoteDebuggerCommand = string.Format(CultureInfo.InvariantCulture, ClrdbgSubsequentLaunchCommand, debuggerHomeDirectory, ClrDbgVersion, ClrDbgInstallationSubDirectory);
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("miMode");
                }
            }

            this.StartRemoteDebuggerCommand = startRemoteDebuggerCommand;

            if (baseLaunchOptions != null)
            {
                this.InitializeCommonOptions(baseLaunchOptions);
                this.BaseOptions = baseLaunchOptions;
            }
        }

        public static UnixShellPortLaunchOptions CreateForAttachRequest(Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier.IDebugUnixShellPort unixPort,
            int processId,
            MIMode miMode,
            string getClrDbgUrl,
            string remoteDebuggingDirectory,
            string remoteDebuggingSubDirectory,
            string debuggerVersion)
        {
            var @this = new UnixShellPortLaunchOptions(startRemoteDebuggerCommand: null,
                                                       unixPort: unixPort,
                                                       miMode: miMode,
                                                       baseLaunchOptions: null,
                                                       getClrDbgUrl: getClrDbgUrl,
                                                       remoteDebuggerInstallationDirectory: remoteDebuggingDirectory,
                                                       remoteDebuggerInstallationSubDirectory: remoteDebuggingSubDirectory,
                                                       clrdbgVersion: debuggerVersion);

            @this.ProcessId = processId;
            @this.SetupCommands = new ReadOnlyCollection<LaunchCommand>(new LaunchCommand[] { });
            @this.LoadSupplementalOptions(null);
            @this.SetInitializationComplete();

            return @this;
        }

        /// <summary>
        /// Records for a specific portname, the remote launch was successful.
        /// </summary>
        /// <param name="launchOptions">launch options</param>
        public static void SetSuccessfulLaunch(UnixShellPortLaunchOptions launchOptions)
        {
            IDebugPort2 debugPort = launchOptions.UnixPort as IDebugPort2;
            if (debugPort != null)
            {
                string portName = null;
                debugPort.GetPortName(out portName);
                if (!string.IsNullOrWhiteSpace(portName))
                {
                    lock (s_LaunchSuccessSet)
                    {
                        // If it is successful once, we expect the clrdbg launch to be successful atleast till the end of the current VS session. 
                        // The portname will not be removed from the list.
                        s_LaunchSuccessSet.Add(portName);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the previous launch was ever successful on the same session false otherwise.
        /// </summary>
        /// <param name="launchOptions">launch options</param>
        public static bool HasSuccessfulPreviousLaunch(UnixShellPortLaunchOptions launchOptions)
        {
            IDebugPort2 debugPort = launchOptions.UnixPort as IDebugPort2;
            if (debugPort != null)
            {
                string portName = null;
                debugPort.GetPortName(out portName);
                if (!string.IsNullOrWhiteSpace(portName))
                {
                    lock (s_LaunchSuccessSet)
                    {
                        // If it is successful once, we expect the clrdbg launch to be successful atleast till the end of the current VS session. 
                        // The portname will not be removed from the list.
                        return s_LaunchSuccessSet.Contains(portName);
                    }
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Base launch options class
    /// </summary>
    public abstract class LaunchOptions
    {
        private const string XmlNamespace = "http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014";
        private static Lazy<Assembly> s_serializationAssembly = new Lazy<Assembly>(LoadSerializationAssembly, LazyThreadSafetyMode.ExecutionAndPublication);
        private bool _initializationComplete;
        private MIMode _miMode;

        /// <summary>
        /// [Optional] Launcher used to start the application on the device
        /// </summary>
        public IPlatformAppLauncher DeviceAppLauncher { get; private set; }

        public MIMode DebuggerMIMode
        {
            get { return _miMode; }
            set
            {
                VerifyCanModifyProperty("DebuggerMIMode");
                _miMode = value;
            }
        }

        public bool NoDebug { get; private set; } = false;

        private Xml.LaunchOptions.BaseLaunchOptions _baseOptions;
        /// <summary>
        /// Hold on to options in serializable form to support child process debugging
        /// </summary>
        public Xml.LaunchOptions.BaseLaunchOptions BaseOptions
        {
            get { return _baseOptions; }
            protected set
            {
                if (value == null)
                    throw new ArgumentNullException("BaseOptions");
                VerifyCanModifyProperty("BaseOptions");

                _baseOptions = value;
            }
        }

        private string _exePath;

        /// <summary>
        /// [Required] Path to the executable file. This could be a path on the remote machine (for Pipe transport)
        /// or the local machine (Local transport).
        /// </summary>
        public virtual string ExePath
        {
            get { return _exePath; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentOutOfRangeException("ExePath");
                VerifyCanModifyProperty("ExePath");

                _exePath = value;
            }
        }

        private string _exeArguments;
        /// <summary>
        /// [Optional] Additional arguments to specify when launching the process
        /// </summary>
        public string ExeArguments
        {
            get { return _exeArguments; }
            set
            {
                VerifyCanModifyProperty("ExeArguments");
                _exeArguments = value;
            }
        }

        private int _processId;

        /// <summary>
        /// [Optional] If supplied, the debugger will attach to the process rather than launching a new one. Note that some operating systems will require admin rights to do this.
        /// </summary>
        public int ProcessId
        {
            get { return _processId; }
            protected set
            {
                VerifyCanModifyProperty("ProcessId");
                _processId = value;
            }
        }

        private string _coreDumpPath;
        /// <summary>
        /// [Optional] Path to a core dump file for the specified executable.
        /// </summary>
        public string CoreDumpPath
        {
            get
            {
                return _coreDumpPath;
            }
            protected set
            {
                VerifyCanModifyProperty("CoreDumpPath");

                // CoreDumpPath is allowed to be null/empty
                _coreDumpPath = value;
            }
        }
        public bool IsCoreDump
        {
            get { return !String.IsNullOrEmpty(this.CoreDumpPath); }
        }

        private string _workingDirectory;
        /// <summary>
        /// [Optional] Working directory to use for the MI Debugger when launching the process
        /// </summary>
        public string WorkingDirectory
        {
            get { return _workingDirectory; }
            set
            {
                VerifyCanModifyProperty("WorkingDirectory");
                _workingDirectory = value;
            }
        }

        private string _absolutePrefixSoLibSearchPath;
        /// <summary>
        /// [Optional] Absolute prefix for directories to search for shared library symbols
        /// </summary>
        public string AbsolutePrefixSOLibSearchPath
        {
            get { return _absolutePrefixSoLibSearchPath; }
            set
            {
                VerifyCanModifyProperty("AbsolutePrefixSOLibSearchPath");
                _absolutePrefixSoLibSearchPath = value;
            }
        }

        private string _additionalSOLibSearchPath;
        /// <summary>
        /// [Optional] Additional directories to search for shared library symbols
        /// </summary>
        public string AdditionalSOLibSearchPath
        {
            get { return _additionalSOLibSearchPath; }
            set
            {
                VerifyCanModifyProperty("AdditionalSOLibSearchPath");
                _additionalSOLibSearchPath = value;
            }
        }

        private string _visualizerFile;
        /// <summary>
        /// [Optional] Natvis file name - from install location
        /// </summary>
        public string VisualizerFile
        {
            get { return _visualizerFile; }
            set
            {
                VerifyCanModifyProperty("VisualizerFile");
                _visualizerFile = value;
            }
        }

        private bool _waitDynamicLibLoad = true;
        /// <summary>
        /// If true, wait for dynamic library load to finish.
        /// </summary>
        public bool WaitDynamicLibLoad
        {
            get { return _waitDynamicLibLoad; }
            set
            {
                VerifyCanModifyProperty("WaitDynamicLibLoad");
                _waitDynamicLibLoad = value;
            }
        }

        /// <summary>
        /// If true, instead of showing Natvis-DisplayString value as a child of a dummy element, it is shown immediately.
        /// Should only be enabled if debugger is fast enough providing the value.
        /// </summary>
        public bool ShowDisplayString { get; set; }

        private TargetArchitecture _targetArchitecture;
        public TargetArchitecture TargetArchitecture
        {
            get { return _targetArchitecture; }
            set
            {
                VerifyCanModifyProperty("TargetArchitecture");
                _targetArchitecture = value;
            }
        }

        /// <summary>
        /// True if we assume that various symbol paths are going to be processed on a Unix machine
        /// </summary>
        public bool UseUnixSymbolPaths
        {
            get
            {
                if (this is LocalLaunchOptions)
                {
                    return !PlatformUtilities.IsWindows();
                }
                else
                {
                    // For now lets assume the debugger is on Unix if we are using Pipe/Tcp launch options
                    return true;
                }
            }
        }

        private ReadOnlyCollection<LaunchCommand> _setupCommands;

        /// <summary>
        /// [Required] Additional commands used to setup debugging. May be an empty collection
        /// </summary>
        public ReadOnlyCollection<LaunchCommand> SetupCommands
        {
            get { return _setupCommands; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("SetupCommands");

                VerifyCanModifyProperty("SetupCommands");
                _setupCommands = value;
            }
        }


        private ReadOnlyCollection<LaunchCommand> _customLaunchSetupCommands;

        /// <summary>
        /// [Optional] If provided, this replaces the default commands used to launch a target with some other commands. For example,
        /// this can be '-target-attach' in order to attach to a target process.An empty command list replaces the launch commands with nothing,
        /// which can be useful if the debugger is being provided launch options as command line options.
        /// </summary>
        public ReadOnlyCollection<LaunchCommand> CustomLaunchSetupCommands
        {
            get { return _customLaunchSetupCommands; }
            set
            {
                VerifyCanModifyProperty("CustomLaunchSetupCommands");
                _customLaunchSetupCommands = value;
            }
        }

        private LaunchCompleteCommand _launchCompleteCommand;

        public LaunchCompleteCommand LaunchCompleteCommand
        {
            get { return _launchCompleteCommand; }
            set
            {
                VerifyCanModifyProperty("LaunchCompleteCommand");
                _launchCompleteCommand = value;
            }
        }

        private bool _debugChildProcesses;

        public bool DebugChildProcesses
        {
            get { return _debugChildProcesses; }
            protected set
            {
                VerifyCanModifyProperty("DebugChildProcesses");
                _debugChildProcesses = value;
            }
        }

        private ReadOnlyCollection<SourceMapEntry> _sourceMap;

        public ReadOnlyCollection<SourceMapEntry> SourceMap
        {
            get { return _sourceMap; }
            set
            {
                VerifyCanModifyProperty("SourceMap");
                _sourceMap = value;
            }
        }

        public string GetOptionsString()
        {
            try
            {
                var strWriter = new StringWriter(CultureInfo.InvariantCulture);
                XmlSerializer serializer;
                using (XmlWriter writer = XmlWriter.Create(strWriter))
                {
                    if (BaseOptions is Xml.LaunchOptions.LocalLaunchOptions)
                    {
                        serializer = new XmlSerializer(typeof(Xml.LaunchOptions.LocalLaunchOptions));
                        Serialize(serializer, writer, BaseOptions);
                    }
                    else if (BaseOptions is Xml.LaunchOptions.PipeLaunchOptions)
                    {
                        serializer = new XmlSerializer(typeof(Xml.LaunchOptions.PipeLaunchOptions));
                        Serialize(serializer, writer, BaseOptions);
                    }
                    else if (BaseOptions is Xml.LaunchOptions.TcpLaunchOptions)
                    {
                        serializer = new XmlSerializer(typeof(Xml.LaunchOptions.TcpLaunchOptions));
                        Serialize(serializer, writer, BaseOptions);
                    }
                    else
                    {
                        throw new XmlException(MICoreResources.Error_UnknownLaunchOptions);
                    }
                }
                return strWriter.ToString();
            }
            catch (Exception e)
            {
                throw new InvalidLaunchOptionsException(e.Message);
            }
        }

        public static LaunchOptions GetInstance(HostConfigurationStore configStore, string exePath, string args, string dir, string options, bool noDebug, IDeviceAppLauncherEventCallback eventCallback, TargetEngine targetEngine, Logger logger)
        {
            if (string.IsNullOrWhiteSpace(exePath))
                throw new ArgumentNullException("exePath");

            if (string.IsNullOrWhiteSpace(options))
                throw new InvalidLaunchOptionsException(MICoreResources.Error_StringIsNullOrEmpty);

            logger?.WriteTextBlock("LaunchOptions", options);

            LaunchOptions launchOptions = null;
            Guid clsidLauncher = Guid.Empty;
            object launcher = null;
            object launcherXmlOptions = null;

            try
            {
                XmlSerializer serializer;
                using (XmlReader reader = OpenXml(options))
                {
                    switch (reader.LocalName)
                    {
                        case "LocalLaunchOptions":
                            {
                                serializer = GetXmlSerializer(typeof(Xml.LaunchOptions.LocalLaunchOptions));
                                var xmlLaunchOptions = (Xml.LaunchOptions.LocalLaunchOptions)Deserialize(serializer, reader);
                                launchOptions = LocalLaunchOptions.CreateFromXml(xmlLaunchOptions);
                                launchOptions.BaseOptions = xmlLaunchOptions;
                            }
                            break;

                        case "PipeLaunchOptions":
                            {
                                serializer = GetXmlSerializer(typeof(Xml.LaunchOptions.PipeLaunchOptions));
                                var xmlLaunchOptions = (Xml.LaunchOptions.PipeLaunchOptions)Deserialize(serializer, reader);
                                launchOptions = PipeLaunchOptions.CreateFromXml(xmlLaunchOptions);
                                launchOptions.BaseOptions = xmlLaunchOptions;
                            }
                            break;

                        case "TcpLaunchOptions":
                            {
                                serializer = GetXmlSerializer(typeof(Xml.LaunchOptions.TcpLaunchOptions));
                                var xmlLaunchOptions = (Xml.LaunchOptions.TcpLaunchOptions)Deserialize(serializer, reader);
                                launchOptions = TcpLaunchOptions.CreateFromXml(xmlLaunchOptions);
                                launchOptions.BaseOptions = xmlLaunchOptions;
                            }
                            break;

                        case "IOSLaunchOptions":
                            {
                                serializer = GetXmlSerializer(typeof(IOSLaunchOptions));
                                launcherXmlOptions = Deserialize(serializer, reader);
                                clsidLauncher = new Guid("316783D1-1824-4847-B3D3-FB048960EDCF");
                            }
                            break;

                        case "AndroidLaunchOptions":
                            {
                                serializer = GetXmlSerializer(typeof(AndroidLaunchOptions));
                                launcherXmlOptions = Deserialize(serializer, reader);
                                clsidLauncher = new Guid("C9A403DA-D3AA-4632-A572-E81FF6301E9B");
                            }
                            break;

                        case "SSHLaunchOptions":
                            {
                                serializer = GetXmlSerializer(typeof(SSHLaunchOptions));
                                launcherXmlOptions = Deserialize(serializer, reader);
                                clsidLauncher = new Guid("7E3052B2-FB42-4E38-B22C-1FD281BD4413");
                            }
                            break;

                        default:
                            {
                                launcher = configStore?.GetCustomLauncher(reader.LocalName);
                                if (launcher == null)
                                {
                                    throw new XmlException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_UnknownXmlElement, reader.LocalName));
                                }
                                if (launcher as IPlatformAppLauncher == null)
                                {
                                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_LauncherNotFound, reader.LocalName));
                                }
                                var deviceAppLauncher = (IPlatformAppLauncherSerializer)launcher;
                                if (deviceAppLauncher == null)
                                {
                                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_LauncherSerializerNotFound, clsidLauncher.ToString("B")));
                                }
                                serializer = deviceAppLauncher.GetXmlSerializer(reader.LocalName);
                                launcherXmlOptions = Deserialize(serializer, reader);
                            }
                            break;
                    }

                    // Read any remaining bits of XML to catch other errors
                    while (reader.NodeType != XmlNodeType.None)
                        reader.Read();
                }
            }
            catch (XmlException e)
            {
                throw new InvalidLaunchOptionsException(e.Message);
            }

            if (clsidLauncher != Guid.Empty)
            {
                launchOptions = ExecuteLauncher(configStore, clsidLauncher, exePath, args, dir, launcherXmlOptions, eventCallback, targetEngine, logger);
            }
            else if (launcher != null)
            {
                launchOptions = ExecuteLauncher(configStore, (IPlatformAppLauncher)launcher, exePath, args, dir, launcherXmlOptions, eventCallback, targetEngine, logger);
            }

            if (targetEngine == TargetEngine.Native)
            {
                if (launchOptions.ExePath == null)
                    launchOptions.ExePath = exePath;
            }

            if (string.IsNullOrEmpty(launchOptions.ExeArguments))
                launchOptions.ExeArguments = args;

            if (string.IsNullOrEmpty(launchOptions.WorkingDirectory))
                launchOptions.WorkingDirectory = dir;

            launchOptions.NoDebug = noDebug;

            if (launchOptions._setupCommands == null)
                launchOptions._setupCommands = new List<LaunchCommand>(capacity: 0).AsReadOnly();

            // load supplemental options 
            launchOptions.LoadSupplementalOptions(logger);

            launchOptions.SetInitializationComplete();
            return launchOptions;
        }

        internal void LoadSupplementalOptions(Logger logger)
        {
            if (SourceMap == null)
            {
                SourceMap = new ReadOnlyCollection<SourceMapEntry>(new List<SourceMapEntry>());
            }
            // load supplemental options from the solution root
            string slnRoot = HostNatvisProject.FindSolutionRoot();
            if (!string.IsNullOrEmpty(slnRoot))
            {
                string optFile = Path.Combine(slnRoot, "Microsoft.MIEngine.Options.xml");
                if (File.Exists(optFile))
                {
                    var reader = File.OpenText(optFile);
                    string suppOptions = reader.ReadToEnd();
                    if (!string.IsNullOrEmpty(suppOptions))
                    {
                        try
                        {
                            logger?.WriteTextBlock("SupplementalLaunchOptions", suppOptions);
                            XmlReader xmlRrd = OpenXml(suppOptions);
                            XmlSerializer serializer = GetXmlSerializer(typeof(Xml.LaunchOptions.SupplementalLaunchOptions));
                            var xmlSuppOptions = (Xml.LaunchOptions.SupplementalLaunchOptions)Deserialize(serializer, xmlRrd);
                            Merge(xmlSuppOptions);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_ProcessingFile, "Microsoft.MIEngine.Options.xml", e.Message), e);
                        }
                    }
                }
            }
        }

        private void Merge(SupplementalLaunchOptions suppOptions)
        {
            // merge the source mapping lists
            List<SourceMapEntry> map = new List<SourceMapEntry>();
            if (suppOptions.SourceMap != null)
            {
                foreach (var e in suppOptions.SourceMap)    // add new entries from the supplemental options
                {
                    map.Add(new SourceMapEntry(e));
                }
            }
            if (SourceMap != null)
            {
                foreach (var e in SourceMap)    // append project system entries
                {
                    map.Add(e);
                }
            }
            SourceMap = new ReadOnlyCollection<SourceMapEntry>(map);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security.Xml", "CA3053: UseSecureXmlResolver.",
            Justification = "Usage is secure -- XmlResolver property is set to 'null' in desktop CLR, and is always null in CoreCLR. But CodeAnalysis cannot understand the invocation since it happens through reflection.")]
        public static XmlReader OpenXml(string content)
        {
            var settings = new XmlReaderSettings();
            settings.CloseInput = true;
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;
            settings.NameTable = new NameTable();

            // set XmlResolver via reflection, if it exists. This is required for desktop CLR, as otherwise the XML reader may
            // attempt to hit untrusted external resources.
            var xmlResolverProperty = settings.GetType().GetProperty("XmlResolver", BindingFlags.Public | BindingFlags.Instance);
            xmlResolverProperty?.SetValue(settings, null);

            // Create our own namespace manager so that we can set the default namespace
            // We need this because the XML serializer requires correct namespaces,
            // but project systems may not provide it.
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(settings.NameTable);
            namespaceManager.AddNamespace(string.Empty, XmlNamespace);
            XmlParserContext context = new XmlParserContext(settings.NameTable, namespaceManager, string.Empty, XmlSpace.None);

            StringReader stringReader = null;
            XmlReader reader = null;
            bool success = false;

            try
            {
                stringReader = new StringReader(content);
                reader = XmlReader.Create(stringReader, settings, context);

                // Read to the top level element
                while (reader.NodeType != XmlNodeType.Element)
                    reader.Read();

                if (reader.NamespaceURI != XmlNamespace)
                {
                    throw new XmlException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_UnknownXmlElement, reader.Name));
                }

                success = true;
                return reader;
            }
            finally
            {
                if (!success)
                {
                    if (reader != null)
                    {
                        reader.Dispose();
                    }
                    else if (stringReader != null)
                    {
                        // NOTE: the reader will close the input, so we only want to do this
                        // if we failed to create the reader.
                        stringReader.Dispose();
                    }
                }
            }
        }

        public static object Deserialize(XmlSerializer serializer, XmlReader reader)
        {
            try
            {
                return serializer.Deserialize(reader);
            }
            catch (InvalidOperationException outerException)
            {
                // In all the cases I have seen thus far, the InvalidOperationException has a fairly useless message
                // and the inner exception message is better.
                Exception e = outerException.InnerException ?? outerException;

                throw new InvalidLaunchOptionsException(e.Message);
            }
        }

        private static void Serialize(XmlSerializer serializer, XmlWriter writer, object o)
        {
            try
            {
                serializer.Serialize(writer, o);
            }
            catch (InvalidOperationException outerException)
            {
                // In all the cases I have seen thus far, the InvalidOperationException has a fairly useless message
                // and the inner exception message is better.
                Exception e = outerException.InnerException ?? outerException;

                throw new InvalidLaunchOptionsException(e.Message);
            }
        }

        public IEnumerable<string> GetSOLibSearchPath()
        {
            IEqualityComparer<string> comparer = this.UseUnixSymbolPaths ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            return GetSOLibSearchPathCandidates().Distinct(comparer);
        }

        /// <summary>
        /// Returns the possible paths 
        /// </summary>
        private IEnumerable<string> GetSOLibSearchPathCandidates()
        {
            char[] slashes = { '\\', '/' };

            if (_exePath != null)
            {
                // NOTE: Path.GetDirectoryName doesn't do the right thing for unix paths, so use our own logic

                int lastSlashIndex = _exePath.LastIndexOfAny(slashes);
                if (lastSlashIndex > 0)
                {
                    int exeDirectoryLength = lastSlashIndex;
                    if (exeDirectoryLength == 2 && _exePath[1] == ':')
                        exeDirectoryLength++; // for 'c:\foo.exe' we want to return 'c:\' instead of 'c:'

                    yield return _exePath.Substring(0, exeDirectoryLength);
                }
            }

            if (!string.IsNullOrEmpty(_additionalSOLibSearchPath))
            {
                foreach (string directory in _additionalSOLibSearchPath.Split(';'))
                {
                    if (string.IsNullOrWhiteSpace(directory))
                        continue;

                    // To make sure that all directory names are in a canonical form, if there are any trailing slashes, remove them
                    string directoryWithoutTrailingSlashes = directory.TrimEnd(slashes);

                    if (directoryWithoutTrailingSlashes.Length == 2 && directoryWithoutTrailingSlashes[1] == ':')
                        yield return directoryWithoutTrailingSlashes + '\\'; // add the slash to drive letters though so the path is not relative

                    yield return directoryWithoutTrailingSlashes;
                }
            }
        }

        protected void InitializeCommonOptions(Xml.LaunchOptions.BaseLaunchOptions source)
        {
            if (this.ExePath == null)
            {
                string exePath = source.ExePath;
                if (!string.IsNullOrWhiteSpace(exePath))
                {
                    this.ExePath = exePath;
                }
            }

            if (this.TargetArchitecture == TargetArchitecture.Unknown && source.TargetArchitectureSpecified)
            {
                this.TargetArchitecture = ConvertTargetArchitectureAttribute(source.TargetArchitecture);
            }

            this.DebuggerMIMode = ConvertMIModeAttribute(source.MIMode);

            if (string.IsNullOrEmpty(this.ExeArguments))
                this.ExeArguments = source.ExeArguments;

            if (string.IsNullOrEmpty(this.WorkingDirectory))
                this.WorkingDirectory = source.WorkingDirectory;

            if (string.IsNullOrEmpty(this.VisualizerFile))
                this.VisualizerFile = source.VisualizerFile;

            this.ShowDisplayString = source.ShowDisplayString;
            this.WaitDynamicLibLoad = source.WaitDynamicLibLoad;

            this.SetupCommands = LaunchCommand.CreateCollectionFromXml(source.SetupCommands);

            if (source.CustomLaunchSetupCommands != null)
            {
                this.CustomLaunchSetupCommands = LaunchCommand.CreateCollectionFromXml(source.CustomLaunchSetupCommands);
            }

            this.SourceMap = SourceMapEntry.CreateCollectionFromXml(source.SourceMap);

            Debug.Assert((uint)LaunchCompleteCommand.ExecRun == (uint)Xml.LaunchOptions.BaseLaunchOptionsLaunchCompleteCommand.execrun);
            Debug.Assert((uint)LaunchCompleteCommand.ExecContinue == (uint)Xml.LaunchOptions.BaseLaunchOptionsLaunchCompleteCommand.execcontinue);
            Debug.Assert((uint)LaunchCompleteCommand.None == (uint)Xml.LaunchOptions.BaseLaunchOptionsLaunchCompleteCommand.None);
            this.LaunchCompleteCommand = (LaunchCompleteCommand)source.LaunchCompleteCommand;

            string additionalSOLibSearchPath = source.AdditionalSOLibSearchPath;
            if (!string.IsNullOrEmpty(additionalSOLibSearchPath))
            {
                if (string.IsNullOrEmpty(this.AdditionalSOLibSearchPath))
                    this.AdditionalSOLibSearchPath = additionalSOLibSearchPath;
                else
                    this.AdditionalSOLibSearchPath = string.Concat(this.AdditionalSOLibSearchPath, ";", additionalSOLibSearchPath);
            }
            if (string.IsNullOrEmpty(this.AbsolutePrefixSOLibSearchPath))
                this.AbsolutePrefixSOLibSearchPath = source.AbsolutePrefixSOLibSearchPath;

            if (source.DebugChildProcessesSpecified)
            {
                this.DebugChildProcesses = source.DebugChildProcesses;
            }

            this.ProcessId = source.ProcessId;
            this.CoreDumpPath = source.CoreDumpPath;

            // Ensure that CoreDumpPath and ProcessId are not specified at the same time
            if (!String.IsNullOrEmpty(source.CoreDumpPath) && source.ProcessIdSpecified)
                throw new InvalidLaunchOptionsException(String.Format(CultureInfo.InvariantCulture, MICoreResources.Error_CannotSpecifyBoth, nameof(source.CoreDumpPath), nameof(source.ProcessId)));
        }

        public static string RequireAttribute(string attributeValue, string attributeName)
        {
            if (string.IsNullOrWhiteSpace(attributeValue))
                throw new InvalidLaunchOptionsException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_MissingAttribute, attributeName));

            return attributeValue;
        }

        public static int RequirePortAttribute(int attributeValue, string attributeName)
        {
            if (attributeValue <= 0 || attributeValue >= 0xffff)
            {
                throw new InvalidLaunchOptionsException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_BadRequiredAttribute, "Port"));
            }

            return attributeValue;
        }

        private static LaunchOptions ExecuteLauncher(HostConfigurationStore configStore, Guid clsidLauncher, string exePath, string args, string dir, object launcherXmlOptions, IDeviceAppLauncherEventCallback eventCallback, TargetEngine targetEngine, Logger logger)
        {
            var deviceAppLauncher = (IPlatformAppLauncher)HostLoader.VsCoCreateManagedObject(configStore, clsidLauncher);
            if (deviceAppLauncher == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_LauncherNotFound, clsidLauncher.ToString("B")));
            }
            return ExecuteLauncher(configStore, deviceAppLauncher, exePath, args, dir, launcherXmlOptions, eventCallback, targetEngine, logger);
        }

        private static LaunchOptions ExecuteLauncher(HostConfigurationStore configStore, IPlatformAppLauncher deviceAppLauncher, string exePath, string args, string dir, object launcherXmlOptions, IDeviceAppLauncherEventCallback eventCallback, TargetEngine targetEngine, Logger logger)
        {
            bool success = false;

            try
            {
                try
                {
                    deviceAppLauncher.Initialize(configStore, eventCallback);
                    deviceAppLauncher.SetLaunchOptions(exePath, args, dir, launcherXmlOptions, targetEngine);
                }
                catch (Exception e) when (!(e is InvalidLaunchOptionsException) && ExceptionHelper.BeforeCatch(e, logger, reportOnlyCorrupting: true))
                {
                    throw new InvalidLaunchOptionsException(e.Message);
                }

                LaunchOptions debuggerLaunchOptions;
                deviceAppLauncher.SetupForDebugging(out debuggerLaunchOptions);
                debuggerLaunchOptions.DeviceAppLauncher = deviceAppLauncher;

                success = true;
                return debuggerLaunchOptions;
            }
            finally
            {
                if (!success)
                {
                    deviceAppLauncher.Dispose();
                }
            }
        }

        private static XmlSerializer GetXmlSerializer(Type type)
        {
            Assembly serializationAssembly = s_serializationAssembly.Value;
            if (serializationAssembly == null)
            {
                return new XmlSerializer(type);
            }
            else
            {
                // NOTE: You can look at MIEngine\src\MICore\obj\Debug\sgen\<random-temp-file-name>.cs to see the source code for this assembly.
                Type serializerType = serializationAssembly.GetType("Microsoft.Xml.Serialization.GeneratedAssembly." + type.Name + "Serializer");
                ConstructorInfo constructor = serializerType?.GetConstructor(new Type[0]);
                if (constructor == null)
                {
                    throw new Exception(string.Format(CultureInfo.CurrentUICulture, MICoreResources.Error_UnableToLoadSerializer, type.Name));
                }

                object serializer = constructor.Invoke(new object[0]);
                return (XmlSerializer)serializer;
            }
        }

        private static Assembly LoadSerializationAssembly()
        {
            // This code looks to see if we have sgen-created XmlSerializers assembly next to this dll, which will be true
            // when the MIEngine is running in Visual Studio. If so, it loads it, so that we can get the performance advantages
            // of a static XmlSerializers assembly. Otherwise we return null, and we will use a dynamic deserializer.

            string thisModulePath = typeof(LaunchOptions).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName;
            string thisModuleDir = Path.GetDirectoryName(thisModulePath);
            string thisModuleName = Path.GetFileNameWithoutExtension(thisModulePath);
            string serializerAssemblyPath = Path.Combine(thisModuleDir, thisModuleName + ".XmlSerializers.dll");
            if (!File.Exists(serializerAssemblyPath))
                return null;

            return Assembly.Load(new AssemblyName(thisModuleName + ".XmlSerializers"));
        }

        protected void SetInitializationComplete()
        {
            _initializationComplete = true;
        }

        private void VerifyCanModifyProperty(string propertyName)
        {
            if (_initializationComplete)
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_PropertyCannotBeModifiedAfterInitialization, propertyName));
        }

        public static TargetArchitecture ConvertTargetArchitectureAttribute(Xml.LaunchOptions.TargetArchitecture source)
        {
            switch (source)
            {
                case Xml.LaunchOptions.TargetArchitecture.X86:
                case Xml.LaunchOptions.TargetArchitecture.x86:
                    return TargetArchitecture.X86;

                case Xml.LaunchOptions.TargetArchitecture.arm:
                case Xml.LaunchOptions.TargetArchitecture.ARM:
                    return TargetArchitecture.ARM;

                case Xml.LaunchOptions.TargetArchitecture.mips:
                case Xml.LaunchOptions.TargetArchitecture.MIPS:
                    return TargetArchitecture.Mips;

                case Xml.LaunchOptions.TargetArchitecture.x64:
                case Xml.LaunchOptions.TargetArchitecture.amd64:
                case Xml.LaunchOptions.TargetArchitecture.x86_64:
                case Xml.LaunchOptions.TargetArchitecture.X64:
                case Xml.LaunchOptions.TargetArchitecture.AMD64:
                case Xml.LaunchOptions.TargetArchitecture.X86_64:
                    return TargetArchitecture.X64;

                case Xml.LaunchOptions.TargetArchitecture.arm64:
                case Xml.LaunchOptions.TargetArchitecture.ARM64:
                    return TargetArchitecture.ARM64;

                default:
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_UnknownTargetArchitecture, source.ToString()));
            }
        }

        public static MIMode ConvertMIModeAttribute(Xml.LaunchOptions.MIMode source)
        {
            Debug.Assert((uint)MIMode.Gdb == (uint)Xml.LaunchOptions.MIMode.gdb, "Enum values don't line up!");
            Debug.Assert((uint)MIMode.Lldb == (uint)Xml.LaunchOptions.MIMode.lldb, "Enum values don't line up!");
            Debug.Assert((uint)MIMode.Clrdbg == (uint)Xml.LaunchOptions.MIMode.clrdbg, "Enum values don't line up!");
            return (MIMode)source;
        }
    }

    /// <summary>
    /// Interface implemented by the android launcher. In the future we will truely make use of this as a COM
    /// interface when we are no longer using GDB. For now, we don't actually use this as a COM interface but
    /// rather as a managed interface
    /// </summary>
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("74977D02-627B-4580-BEF7-B79B8D9009EF")]
    public interface IPlatformAppLauncher : IDisposable
    {
        /// <summary>
        /// Initialized the device app launcher
        /// </summary>
        /// <param name="configStore">Current VS registry root</param>
        /// <param name="eventCallback">[Required] Callback object used to send events to the rest of Visual Studio</param>
        void Initialize(HostConfigurationStore configStore, IDeviceAppLauncherEventCallback eventCallback);

        /// <summary>
        /// Initializes the launcher from the launch settings
        /// </summary>
        /// <param name="exePath">[Required] Path to the executable provided in the VsDebugTargetInfo by the project system. Some launchers may ignore this.</param>
        /// <param name="args">[Optional] Arguments to the executable provided in the VsDebugTargetInfo by the project system. Some launchers may ignore this.</param>
        /// <param name="dir">[Optional] Working directory of the executable provided in the VsDebugTargetInfo by the project system. Some launchers may ignore this.</param>
        /// <param name="launcherXmlOptions">[Required] Deserialized XML options structure</param>
        /// <param name="targetEngine">Indicates the type of debugging being done.</param>
        void SetLaunchOptions(string exePath, string args, string dir, object launcherXmlOptions, TargetEngine targetEngine);

        /// <summary>
        /// Does whatever steps are necessary to setup for debugging. On Android this will include launching
        /// the app and launching GDB server.
        /// </summary>
        /// <param name="debuggerLaunchOptions">[Required] settings to use when launching the debugger</param>
        void SetupForDebugging(out LaunchOptions debuggerLaunchOptions);

        /// <summary>
        /// Allows the device app launcher to preform any final tasks after the debugger has connected. On Android
        /// this is when we will connect to the process using JDbg.
        /// </summary>
        void OnResume();

        /// <summary>
        /// Called when terminating the application on stop debugging
        /// </summary>
        void Terminate();
    };

    /// <summary>
    /// Used when implementing a launcher extention. The extention implements this interface for deserializing its custom xml parameters
    /// </summary>
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("040D083A-A799-45F9-A459-B134B49EE629")]
    public interface IPlatformAppLauncherSerializer : IDisposable
    {
        XmlSerializer GetXmlSerializer(string name);
    }

    /// <summary>
    /// Call back implemented by the caller of OnResume to provide a channel for errors
    /// </summary>
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("6FC53A91-CB60-47E7-979B-65B7C894E794")]
    public interface IDeviceAppLauncherEventCallback
    {
        /// <summary>
        /// Call back when an error happens that should be reported to the user
        /// </summary>
        /// <param name="message">[Required] message to send</param>
        void OnWarning(string message);

        /// <summary>
        /// Used to send a custom debug event to a VS IDE service
        /// </summary>
        /// <param name="guidVSService">VS IDE service to send the event to</param>
        /// <param name="sourceId">Guid to uniquely identify the type of message</param>
        /// <param name="messageCode">Identifies the type of custom event being sent. Partners are free to define any
        /// set of values.</param>
        /// <param name="parameter1">[Optional] Specifies additional message-specific information.</param>
        /// <param name="parameter2">[Optional] Specifies additional message-specific information.</param>
        void OnCustomDebugEvent(Guid guidVSService, Guid sourceId, int messageCode, object parameter1, object parameter2);

        /// <summary>
        /// Used to send an output string to the IDE
        /// </summary>
        /// <param name="outputString">message to send</param>
        void OnOutputString(string outputString);
    }
}
