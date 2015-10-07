// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Globalization;
using System.Net.Security;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.Diagnostics;

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
        None
    };

    /// <summary>
    /// Launch options when connecting to an instance of an MI Debugger through a serial port
    /// </summary>
    public sealed class SerialLaunchOptions : LaunchOptions
    {
        public SerialLaunchOptions(string Port)
        {
            if (string.IsNullOrEmpty(Port))
                throw new ArgumentNullException("Port");

            this.Port = Port;
        }

        static internal SerialLaunchOptions CreateFromXml(Xml.LaunchOptions.SerialPortLaunchOptions source)
        {
            var options = new SerialLaunchOptions(RequireAttribute(source.Port, "Port"));
            options.InitializeCommonOptions(source);

            return options;
        }

        /// <summary>
        /// [Required] Serial port to connect to
        /// </summary>
        public string Port { get; private set; }
    }

    /// <summary>
    /// Launch options when connecting to an instance of an MI Debugger running on a remote device through a shell
    /// </summary>
    public sealed class PipeLaunchOptions : LaunchOptions
    {
        public PipeLaunchOptions(string PipePath, string PipeArguments)
        {
            if (string.IsNullOrEmpty(PipePath))
                throw new ArgumentNullException("PipePath");

            this.PipePath = PipePath;
            this.PipeArguments = PipeArguments;
        }

        static internal PipeLaunchOptions CreateFromXml(Xml.LaunchOptions.PipeLaunchOptions source)
        {
            var options = new PipeLaunchOptions(RequireAttribute(source.PipePath, "PipePath"), source.PipeArguments);
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
        public string PipeArguments { get; private set; }
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

    /// <summary>
    /// Launch options class when VS should launch an instance of an MI Debugger to connect to an MI Debugger server
    /// </summary>
    public sealed class LocalLaunchOptions : LaunchOptions
    {
        public LocalLaunchOptions(string MIDebuggerPath, string MIDebuggerServerAddress)
        {
            if (string.IsNullOrEmpty(MIDebuggerPath))
                throw new ArgumentNullException("MIDebuggerPath");

            this.MIDebuggerPath = MIDebuggerPath;
            this.MIDebuggerServerAddress = MIDebuggerServerAddress;
        }

        static internal LocalLaunchOptions CreateFromXml(Xml.LaunchOptions.LocalLaunchOptions source)
        {
            var options = new LocalLaunchOptions(RequireAttribute(source.MIDebuggerPath, "MIDebuggerPath"), source.MIDebuggerServerAddress);
            options.InitializeCommonOptions(source);

            return options;
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
        /// [Required] Path to the executable file. This path must exist on the Visual Studio computer.
        /// </summary>
        public override string ExePath
        {
            get
            {
                return base.ExePath;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value) || value.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || !File.Exists(value) || !Path.IsPathRooted(value))
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_InvalidLocalExePath, value));

                base.ExePath = value;
            }
        }
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
        public JavaLaunchOptions(string jvmHost, int jvmPort, string[] sourceRoots, string processName)
        {
            JVMHost = jvmHost;
            JVMPort = jvmPort;
            SourceRoots = sourceRoots;
            ProcessName = processName;
        }

        public string JVMHost { get; private set; }

        public int JVMPort { get; private set; }

        public string[] SourceRoots { get; private set; }

        public string ProcessName { get; private set; }
    }


    /// <summary>
    /// Base launch options class
    /// </summary>
    public abstract class LaunchOptions
    {
        private const string XmlNamespace = "http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014";

        private bool _initializationComplete;

        /// <summary>
        /// [Optional] Launcher used to start the application on the device
        /// </summary>
        public IPlatformAppLauncher DeviceAppLauncher { get; private set; }

        public MIMode DebuggerMIMode { get; set; }

        private string _exePath;
        /// <summary>
        /// [Required] Path to the executable file. This could be a path on the remote machine (for Pipe/Serial transports)
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
                // For now at least, we will assume that the target system is unix unless we are launching the MI Debugger locally
                return !(this is LocalLaunchOptions);
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

        public static LaunchOptions GetInstance(string registryRoot, string exePath, string args, string dir, string options, IDeviceAppLauncherEventCallback eventCallback, TargetEngine targetEngine)
        {
            if (string.IsNullOrWhiteSpace(exePath))
                throw new ArgumentNullException("exePath");

            if (string.IsNullOrWhiteSpace(options))
                throw new InvalidLaunchOptionsException(MICoreResources.Error_StringIsNullOrEmpty);

            if (string.IsNullOrEmpty(registryRoot))
                throw new ArgumentNullException("registryRoot");

            Logger.WriteTextBlock("LaunchOptions", options);

            LaunchOptions launchOptions = null;
            Guid clsidLauncher = Guid.Empty;
            object launcherXmlOptions = null;

            try
            {
                using (XmlReader reader = OpenXml(options))
                {
                    switch (reader.LocalName)
                    {
                        case "LocalLaunchOptions":
                            {
                                var serializer = new Microsoft.Xml.Serialization.GeneratedAssembly.LocalLaunchOptionsSerializer();
                                var xmlLaunchOptions = (Xml.LaunchOptions.LocalLaunchOptions)Deserialize(serializer, reader);
                                launchOptions = LocalLaunchOptions.CreateFromXml(xmlLaunchOptions);
                            }
                            break;

                        case "SerialPortLaunchOptions":
                            {
                                var serializer = new Microsoft.Xml.Serialization.GeneratedAssembly.SerialPortLaunchOptionsSerializer();
                                var xmlLaunchOptions = (Xml.LaunchOptions.SerialPortLaunchOptions)Deserialize(serializer, reader);
                                launchOptions = SerialLaunchOptions.CreateFromXml(xmlLaunchOptions);
                            }
                            break;

                        case "PipeLaunchOptions":
                            {
                                var serializer = new Microsoft.Xml.Serialization.GeneratedAssembly.PipeLaunchOptionsSerializer();
                                var xmlLaunchOptions = (Xml.LaunchOptions.PipeLaunchOptions)Deserialize(serializer, reader);
                                launchOptions = PipeLaunchOptions.CreateFromXml(xmlLaunchOptions);
                            }
                            break;

                        case "TcpLaunchOptions":
                            {
                                var serializer = new Microsoft.Xml.Serialization.GeneratedAssembly.TcpLaunchOptionsSerializer();
                                var xmlLaunchOptions = (Xml.LaunchOptions.TcpLaunchOptions)Deserialize(serializer, reader);
                                launchOptions = TcpLaunchOptions.CreateFromXml(xmlLaunchOptions);
                            }
                            break;

                        case "IOSLaunchOptions":
                            {
                                var serializer = new Microsoft.Xml.Serialization.GeneratedAssembly.IOSLaunchOptionsSerializer();
                                launcherXmlOptions = Deserialize(serializer, reader);
                                clsidLauncher = new Guid("316783D1-1824-4847-B3D3-FB048960EDCF");
                            }
                            break;

                        case "AndroidLaunchOptions":
                            {
                                var serializer = new Microsoft.Xml.Serialization.GeneratedAssembly.AndroidLaunchOptionsSerializer();
                                launcherXmlOptions = Deserialize(serializer, reader);
                                clsidLauncher = new Guid("C9A403DA-D3AA-4632-A572-E81FF6301E9B");
                            }
                            break;

                        default:
                            {
                                throw new XmlException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_UnknownXmlElement, reader.LocalName));
                            }
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
                launchOptions = ExecuteLauncher(registryRoot, clsidLauncher, exePath, args, dir, launcherXmlOptions, eventCallback, targetEngine);
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

            if (launchOptions._setupCommands == null)
                launchOptions._setupCommands = new List<LaunchCommand>(capacity: 0).AsReadOnly();

            launchOptions._initializationComplete = true;
            return launchOptions;
        }

        public static XmlReader OpenXml(string content)
        {
            var settings = new XmlReaderSettings();
            settings.CloseInput = true;
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;
            settings.NameTable = new NameTable();
            settings.XmlResolver = null;

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
                        reader.Close();
                    }
                    else if (stringReader != null)
                    {
                        // NOTE: the reader will close the input, so we only want to do this
                        // if we failed to create the reader.
                        stringReader.Close();
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

            if (this.TargetArchitecture == TargetArchitecture.Unknown)
            {
                this.TargetArchitecture = ConvertTargetArchitectureAttribute(source.TargetArchitecture);
            }

            Debug.Assert((uint)MIMode.Gdb == (uint)Xml.LaunchOptions.MIMode.gdb, "Enum values don't line up!");
            Debug.Assert((uint)MIMode.Lldb == (uint)Xml.LaunchOptions.MIMode.lldb, "Enum values don't line up!");
            Debug.Assert((uint)MIMode.Clrdbg == (uint)Xml.LaunchOptions.MIMode.clrdbg, "Enum values don't line up!");
            this.DebuggerMIMode = (MIMode)source.MIMode;

            if (string.IsNullOrEmpty(this.ExeArguments))
                this.ExeArguments = source.ExeArguments;

            if (string.IsNullOrEmpty(this.WorkingDirectory))
                this.WorkingDirectory = source.WorkingDirectory;

            if (string.IsNullOrEmpty(this.VisualizerFile))
                this.VisualizerFile = source.VisualizerFile;

            this.SetupCommands = LaunchCommand.CreateCollectionFromXml(source.SetupCommands);

            if (source.CustomLaunchSetupCommands != null)
            {
                this.CustomLaunchSetupCommands = LaunchCommand.CreateCollectionFromXml(source.CustomLaunchSetupCommands);
            }

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

        private static LaunchOptions ExecuteLauncher(string registryRoot, Guid clsidLauncher, string exePath, string args, string dir, object launcherXmlOptions, IDeviceAppLauncherEventCallback eventCallback, TargetEngine targetEngine)
        {
            var deviceAppLauncher = (IPlatformAppLauncher)VSLoader.VsCoCreateManagedObject(registryRoot, clsidLauncher);
            if (deviceAppLauncher == null)
            {
                throw new ApplicationException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_LauncherNotFound, clsidLauncher.ToString("B")));
            }

            bool success = false;

            try
            {
                try
                {
                    deviceAppLauncher.Initialize(registryRoot, eventCallback);
                    deviceAppLauncher.SetLaunchOptions(exePath, args, dir, launcherXmlOptions, targetEngine);
                }
                catch (Exception e) when (!(e is InvalidLaunchOptionsException))
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
        /// <param name="registryRoot">Current VS registry root</param>
        /// <param name="eventCallback">[Required] Callback object used to send events to the rest of Visual Studio</param>
        void Initialize(string registryRoot, IDeviceAppLauncherEventCallback eventCallback);

        /// <summary>
        /// Initializes the launcher from the launch settings
        /// </summary>
        /// <param name="exePath">[Required] Path to the executable provided in the VsDebugTargetInfo by the project system. Some launchers may ignore this.</param>
        /// <param name="args">[Optional] Arguments to the executable provided in the VsDebugTargetInfo by the project system. Some launchers may ignore this.</param>
        /// <param name="dir">[Optional] Working directory of the executable provided in the VsDebugTargetInfo by the project system. Some launchers may ignore this.</param>
        /// <param name="launcherXmlOptions">[Required] Deserialized XML options structure</param>
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
    }
}
