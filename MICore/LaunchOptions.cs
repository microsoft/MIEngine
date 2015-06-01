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

namespace MICore
{
    public enum TargetArchitecture
    {
        Unknown,
        ARM,
        X86,
        X64,
        Mips
    };

    /// <summary>
    /// Launch options when connecting to an instance of an MI Debugger through a serial port
    /// </summary>
    public sealed class SerialLaunchOptions : LaunchOptions
    {
        public SerialLaunchOptions(string Port, IEnumerable<string> Commands)
        {
            if (string.IsNullOrEmpty(Port))
                throw new ArgumentNullException("Port");
            if (Commands == null)
                throw new ArgumentNullException("Commands");

            this.Port = Port;
            this.Commands = Commands;
        }

        static internal SerialLaunchOptions CreateFromXml(XmlReader reader)
        {
            string port = GetRequiredAttribute(reader, "Port");

            var options = new SerialLaunchOptions(port, Enumerable.Empty<string>());
            options.ReadCommonAttributes(reader);

            // NOTE: This needs to happen after all attributes are read
            options.Commands = GetChildCommandElements(reader);

            return options;
        }

        /// <summary>
        /// Initial MI commands to execute in Pipe or Serial mode
        /// </summary>
        public IEnumerable<string> Commands { get; private set; }

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
        public PipeLaunchOptions(string PipePath, string PipeArguments, IEnumerable<string> Commands)
        {
            if (string.IsNullOrEmpty(PipePath))
                throw new ArgumentNullException("PipePath");
            if (Commands == null)
                throw new ArgumentNullException("Commands");

            this.PipePath = PipePath;
            this.PipeArguments = PipeArguments;
            this.Commands = Commands;
        }

        static internal PipeLaunchOptions CreateFromXml(XmlReader reader)
        {
            string pipePath = GetRequiredAttribute(reader, "PipePath");
            string pipeArguments = reader.GetAttribute("PipeArguments");

            var options = new PipeLaunchOptions(pipePath, pipeArguments, Enumerable.Empty<string>());
            options.ReadCommonAttributes(reader);

            // NOTE: This needs to happen after all attributes are read
            options.Commands = GetChildCommandElements(reader);

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

        /// <summary>
        /// Initial MI commands to execute in Pipe or Serial mode
        /// </summary>
        public IEnumerable<string> Commands { get; private set; }
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

        static internal TcpLaunchOptions CreateFromXml(XmlReader reader)
        {
            string hostname = GetRequiredAttribute(reader, "Hostname");
            int port = int.Parse(GetRequiredAttribute(reader, "Port"), CultureInfo.InvariantCulture);
            bool secure = false;
            string secureString = reader.GetAttribute("Secure");
            if (!string.IsNullOrEmpty(secureString))
            {
                bool.TryParse(secureString, out secure);
            }

            var options = new TcpLaunchOptions(hostname, port, secure);
            options.ReadCommonAttributes(reader);

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

        static internal LocalLaunchOptions CreateFromXml(XmlReader reader)
        {
            string miDebuggerPath = GetRequiredAttribute(reader, "MIDebuggerPath");
            string miDebuggerServerAddress = reader.GetAttribute("MIDebuggerServerAddress");

            var options = new LocalLaunchOptions(miDebuggerPath, miDebuggerServerAddress);
            options.ReadCommonAttributes(reader);

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


    /// <summary>
    /// Base launch options class
    /// </summary>
    public abstract class LaunchOptions
    {
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

        public static LaunchOptions GetInstance(string registryRoot, string exePath, string args, string dir, string options, IDeviceAppLauncherEventCallback eventCallback)
        {
            if (string.IsNullOrWhiteSpace(exePath))
                throw new ArgumentNullException("exePath");

            if (string.IsNullOrWhiteSpace(options))
                throw GetLaunchOptionsException(MICoreResources.Error_StringIsNullOrEmpty);

            if (string.IsNullOrEmpty(registryRoot))
                throw new ArgumentNullException("registryRoot");

            Logger.WriteTextBlock("LaunchOptions", options);

            LaunchOptions launchOptions = null;
            Guid clsidLauncher = Guid.Empty;

            var settings = new XmlReaderSettings();
            settings.CloseInput = false;
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;

            try
            {
                using (StringReader stringReader = new StringReader(options))
                using (XmlReader reader = XmlReader.Create(stringReader, settings))
                {
                    // Read to the top level element
                    while (reader.NodeType != XmlNodeType.Element)
                        reader.Read();

                    // Allow either no namespace, or the correct namespace
                    if (!string.IsNullOrEmpty(reader.NamespaceURI) && reader.NamespaceURI != "http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014")
                    {
                        throw new XmlException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_UnknownXmlElement, reader.Name));
                    }

                    switch (reader.LocalName)
                    {
                        case "LocalLaunchOptions":
                            launchOptions = LocalLaunchOptions.CreateFromXml(reader);
                            break;

                        case "SerialPortLaunchOptions":
                            launchOptions = SerialLaunchOptions.CreateFromXml(reader);
                            break;

                        case "PipeLaunchOptions":
                            launchOptions = PipeLaunchOptions.CreateFromXml(reader);
                            break;

                        case "TcpLaunchOptions":
                            launchOptions = TcpLaunchOptions.CreateFromXml(reader);
                            break;

                        case "IOSLaunchOptions":
                            clsidLauncher = new Guid("316783D1-1824-4847-B3D3-FB048960EDCF");
                            break;

                        case "AndroidLaunchOptions":
                            clsidLauncher = new Guid("C9A403DA-D3AA-4632-A572-E81FF6301E9B");
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
                throw GetLaunchOptionsException(e.Message);
            }

            if (clsidLauncher != Guid.Empty)
            {
                launchOptions = ExecuteLauncher(registryRoot, clsidLauncher, options, eventCallback);
            }

            if (launchOptions.ExePath == null)
                launchOptions.ExePath = exePath;

            if (string.IsNullOrEmpty(launchOptions.ExeArguments))
                launchOptions.ExeArguments = args;

            if (string.IsNullOrEmpty(launchOptions.WorkingDirectory))
                launchOptions.WorkingDirectory = dir;

            launchOptions._initializationComplete = true;
            return launchOptions;
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

        protected void ReadCommonAttributes(XmlReader reader)
        {
            if (this.ExePath == null)
            {
                string exePath = reader.GetAttribute("ExePath");
                if (!string.IsNullOrWhiteSpace(exePath))
                {
                    this.ExePath = exePath;
                }
            }

            if (this.TargetArchitecture == TargetArchitecture.Unknown)
            {
                this.TargetArchitecture = GetTargetArchitectureAttribute(reader);
            }

            string debuggerType = reader.GetAttribute("MIMode");
            this.DebuggerMIMode = MIMode.Gdb;
            if (String.Equals(debuggerType, "lldb", StringComparison.OrdinalIgnoreCase))
            {
                this.DebuggerMIMode = MIMode.Lldb;
            }
            else if (String.Equals(debuggerType, "clrdbg", StringComparison.OrdinalIgnoreCase))
            {
                this.DebuggerMIMode = MIMode.Clrdbg;
            }

            if (string.IsNullOrEmpty(this.ExeArguments))
                this.ExeArguments = reader.GetAttribute("ExeArguments");

            if (string.IsNullOrEmpty(this.WorkingDirectory))
                this.WorkingDirectory = reader.GetAttribute("WorkingDirectory");

            if (string.IsNullOrEmpty(this.VisualizerFile))
                this.VisualizerFile = reader.GetAttribute("VisualizerFile");

            string additionalSOLibSearchPath = reader.GetAttribute("AdditionalSOLibSearchPath");
            if (!string.IsNullOrEmpty(additionalSOLibSearchPath))
            {
                if (string.IsNullOrEmpty(this.AdditionalSOLibSearchPath))
                    this.AdditionalSOLibSearchPath = additionalSOLibSearchPath;
                else
                    this.AdditionalSOLibSearchPath = string.Concat(this.AdditionalSOLibSearchPath, ";", additionalSOLibSearchPath);
            }
        }

        protected static Exception GetLaunchOptionsException(string message)
        {
            return new ArgumentException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_InvalidLaunchOptions, message));
        }

        public static string GetRequiredAttribute(XmlReader reader, string attributeName)
        {
            string value = reader.GetAttribute(attributeName);
            if (string.IsNullOrWhiteSpace(value))
                throw GetLaunchOptionsException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_MissingAttribute, attributeName));
            return value;
        }

        protected static IEnumerable<string> GetChildCommandElements(XmlReader reader)
        {
            // Read past the main node to the children
            if (!reader.ReadToDescendant("Command"))
            {
                return Enumerable.Empty<string>();
            }

            List<string> commands = new List<string>();

            do
            {
                reader.Read();

                while (reader.NodeType == XmlNodeType.Attribute)
                    reader.Read();

                bool hasValue;
                if (reader.NodeType != XmlNodeType.Text)
                    hasValue = false;
                else if (string.IsNullOrWhiteSpace(reader.Value))
                    hasValue = false;
                else
                    hasValue = true;

                if (!hasValue)
                {
                    throw GetLaunchOptionsException(MICoreResources.Error_ExpectedCommandBody);
                }

                commands.Add(reader.Value);
                reader.Read();

                if (reader.NodeType != XmlNodeType.EndElement)
                {
                    throw GetLaunchOptionsException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_UnknownXmlElement, reader.LocalName));
                }
            }
            while (reader.ReadToNextSibling("Command"));

            return commands;
        }

        private static LaunchOptions ExecuteLauncher(string registryRoot, Guid clsidLauncher, string launchOptions, IDeviceAppLauncherEventCallback eventCallback)
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
                    deviceAppLauncher.ParseLaunchOptions(launchOptions);
                }
                catch (Exception e)
                {
                    throw GetLaunchOptionsException(e.Message);
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

        public static TargetArchitecture GetTargetArchitectureAttribute(XmlReader reader)
        {
            string targetArchitecture = GetRequiredAttribute(reader, "TargetArchitecture");
            switch (targetArchitecture.ToLowerInvariant())
            {
                case "x86":
                    return TargetArchitecture.X86;

                case "arm":
                    return MICore.TargetArchitecture.ARM;

                case "mips":
                    return MICore.TargetArchitecture.Mips;

                //case "arm64":
                //    return MICore.TargetArchitecture.ARM64;

                case "x64":
                case "amd64":
                case "x86_64":
                    return TargetArchitecture.X64;

                default:
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_UnknownTargetArchitecture, targetArchitecture));
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
        /// <param name="launchOptions">Launch options string</param>
        void ParseLaunchOptions(string launchOptions);

        /// <summary>
        /// Does whatever steps are necessary to setup for debugging. On Android this will include launching
        /// the app and launching GDB server.
        /// </summary>
        /// <param name="debuggerLaunchOptions">[Required] settings to use when launching the debugger</param>
        void SetupForDebugging(out LaunchOptions debuggerLaunchOptions);

        void InitializeDebuggedProcess(LaunchOptions launchOptions, out IEnumerable<Tuple<string, MICore.ResultClass, string>> intializationCommands);
        void ResumeDebuggedProcess(LaunchOptions launchOptions, out IEnumerable<Tuple<string, MICore.ResultClass>> intializationCommands);

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
