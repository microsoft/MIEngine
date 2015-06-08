// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MICore;
using System.Runtime.InteropServices;
using System.Net;
using System.Globalization;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Threading;
using System.Runtime.ExceptionServices;
using Newtonsoft.Json;

namespace IOSDebugLauncher
{
    [ComVisible(true)]
    [Guid("316783D1-1824-4847-B3D3-FB048960EDCF")]
    internal class Launcher : IPlatformAppLauncher
    {
        private IDeviceAppLauncherEventCallback _callback;
        private IOSLaunchOptions _launchOptions;
        private VcRemoteClient _client;
        private string _appRemotePath;

        private bool _onResumeCalled = false;

        internal class RemotePorts
        {
            [JsonProperty(PropertyName = "idevicedebugserverproxyport")]
            public int IDeviceDebugServerProxyPort { get; set; }
            [JsonProperty(PropertyName = "debugListenerPort")]
            public int DebugListenerPort { get; set; }
        }
        private RemotePorts _remotePorts;

        void IPlatformAppLauncher.Initialize(string registryRoot, IDeviceAppLauncherEventCallback eventCallback)
        {
            _callback = eventCallback;
        }

        public void ParseLaunchOptions(string launchOptions)
        {
            if (string.IsNullOrEmpty(launchOptions))
                throw new ArgumentNullException("launchOptions");

            if (_callback == null)
            {
                Debug.Fail("Why is ParseLaunchOptions called before Initialize?");
                throw new InvalidOperationException();
            }

            if (_launchOptions != null)
            {
                Debug.Fail("Why is ParseLaunchOptions being called more than once?");
                throw new InvalidOperationException();
            }

            _launchOptions = IOSLaunchOptions.CreateFromXml(launchOptions);
        }

        void IPlatformAppLauncher.SetupForDebugging(out LaunchOptions debuggerLaunchOptions)
        {
            if (_launchOptions == null)
            {
                Debug.Fail("Why is SetupForDebugging being called before ParseLaunchOptions?");
                throw new InvalidOperationException();
            }

            _client = VcRemoteClient.GetInstance(_launchOptions);

            _remotePorts = _client.StartDebugListener();

            if (_launchOptions.IOSDebugTarget == IOSDebugTarget.Device)
            {
                _appRemotePath = _client.GetRemoteAppPath();
            }

            debuggerLaunchOptions = new TcpLaunchOptions(_launchOptions.RemoteMachineName, _remotePorts.DebugListenerPort, _launchOptions.Secure);
            (debuggerLaunchOptions as TcpLaunchOptions).ServerCertificateValidationCallback = _client.ServerCertificateValidationCallback;
            debuggerLaunchOptions.TargetArchitecture = _launchOptions.TargetArchitecture;
            debuggerLaunchOptions.AdditionalSOLibSearchPath = _launchOptions.AdditionalSOLibSearchPath;
            debuggerLaunchOptions.DebuggerMIMode = MIMode.Lldb;
        }


        void IPlatformAppLauncher.InitializeDebuggedProcess(LaunchOptions launchOptions, out IEnumerable<Tuple<string, MICore.ResultClass, string>> initializationCommands)
        {
            var commands = new List<Tuple<string, MICore.ResultClass, string>>();

            string fileCommand = String.Empty;

            if (_launchOptions.IOSDebugTarget == IOSDebugTarget.Device)
            {
                fileCommand = string.Format(CultureInfo.InvariantCulture, "-file-exec-and-symbols \"{0}\" -p remote-ios -r \"{1}\"", launchOptions.ExePath, _appRemotePath);
            }
            else
            {
                fileCommand = string.Format(CultureInfo.InvariantCulture, "-file-exec-and-symbols \"{0}\" -p ios-simulator", launchOptions.ExePath);
            }

            string targetCommand = string.Format(CultureInfo.InvariantCulture, "-target-select remote localhost:{0}", _remotePorts.IDeviceDebugServerProxyPort.ToString(CultureInfo.InvariantCulture));
            string breakInMainCommand = string.Format(CultureInfo.InvariantCulture, "-break-insert main");

            commands.Add(new Tuple<string, ResultClass, string>(fileCommand, ResultClass.done, LauncherResources.DefinePlatform));
            commands.Add(new Tuple<string, ResultClass, string>(targetCommand, ResultClass.connected, LauncherResources.Connecting));
            commands.Add(new Tuple<string, ResultClass, string>(breakInMainCommand, ResultClass.done, LauncherResources.SettingBreakpoint));

            if (_launchOptions.IOSDebugTarget == IOSDebugTarget.Simulator)
            {
                string file = Path.GetFileName(launchOptions.ExePath);
                if (!String.IsNullOrWhiteSpace(file) && file.EndsWith(".app", StringComparison.Ordinal))
                {
                    file = file.Substring(0, file.Length - 4);
                }
                if (!String.IsNullOrWhiteSpace(file))
                {
                    string targetAttachCommand = string.Format(CultureInfo.InvariantCulture, "-target-attach -n {0}  --waitfor", file);
                    string launchMessage = string.Format(CultureInfo.InstalledUICulture, LauncherResources.WaitingForApp, file);
                    commands.Add(new Tuple<string, ResultClass, string>(targetAttachCommand, ResultClass.done, launchMessage));
                }
                else
                {
                    throw new Exception(String.Format(CultureInfo.InvariantCulture, LauncherResources.BadNameFormat, launchOptions.ExePath));
                }
            }
            initializationCommands = commands;
        }

        void IPlatformAppLauncher.ResumeDebuggedProcess(LaunchOptions launchOptions, out IEnumerable<Tuple<string, MICore.ResultClass>> initializationCommands)
        {
            var commands = new List<Tuple<string, MICore.ResultClass>>();
            if (_launchOptions.IOSDebugTarget == IOSDebugTarget.Simulator)
            {
                commands.Add(new Tuple<string, ResultClass>("-exec-continue", ResultClass.running));
            }
            else
            {
                commands.Add(new Tuple<string, ResultClass>("-exec-run", ResultClass.running));
            }
            initializationCommands = commands;
        }

        void IPlatformAppLauncher.OnResume()
        {
            _onResumeCalled = true;
            Telemetry.SendLaunchError(Telemetry.LaunchFailureCode.LaunchSuccess.ToString(), _launchOptions.IOSDebugTarget);
            //Nothing to do for this.
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
            }
        }

        public void Terminate()
        {
            if (!_onResumeCalled)
            {
                Telemetry.SendLaunchError(Telemetry.LaunchFailureCode.LaunchFailure.ToString(), _launchOptions.IOSDebugTarget);
            }
            //Nothing to do for this.
        }
    }
}
