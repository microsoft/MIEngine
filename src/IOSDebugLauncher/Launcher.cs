// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using MICore;
using Microsoft.DebugEngineHost;
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

        void IPlatformAppLauncher.Initialize(HostConfigurationStore configStore, IDeviceAppLauncherEventCallback eventCallback)
        {
            _callback = eventCallback;
        }

        void IPlatformAppLauncher.SetLaunchOptions(string exePath, string args, string dir, object launcherXmlOptions, TargetEngine targetEngine)
        {
            if (launcherXmlOptions == null)
                throw new ArgumentNullException(nameof(launcherXmlOptions));

            if (targetEngine != TargetEngine.Native)
                throw new LauncherException(String.Format(CultureInfo.CurrentCulture, LauncherResources.Error_BadTargetEngine, targetEngine.ToString()));

            var iosXmlOptions = (MICore.Xml.LaunchOptions.IOSLaunchOptions)launcherXmlOptions;

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

            _launchOptions = new IOSLaunchOptions(exePath, iosXmlOptions);
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

            if (_client.ServerCertificateValidationCallback != null)
            {
                (debuggerLaunchOptions as TcpLaunchOptions).ServerCertificateValidationCallback = (object sender, object/*X509Certificate*/ certificate, object/*X509Chain*/ chain, SslPolicyErrors sslPolicyErrors) =>
                {
                    return _client.ServerCertificateValidationCallback(sender, (X509Certificate)certificate, (X509Chain)chain, sslPolicyErrors);
                };
            }

            debuggerLaunchOptions.TargetArchitecture = _launchOptions.TargetArchitecture;
            debuggerLaunchOptions.AdditionalSOLibSearchPath = _launchOptions.AdditionalSOLibSearchPath;
            debuggerLaunchOptions.DebuggerMIMode = MIMode.Lldb;
            debuggerLaunchOptions.CustomLaunchSetupCommands = GetCustomLaunchSetupCommands();
            debuggerLaunchOptions.LaunchCompleteCommand = GetLaunchCompleteCommand();
        }

        private ReadOnlyCollection<LaunchCommand> GetCustomLaunchSetupCommands()
        {
            var commands = new List<LaunchCommand>();

            string fileCommand = String.Empty;

            if (_launchOptions.IOSDebugTarget == IOSDebugTarget.Device)
            {
                fileCommand = string.Format(CultureInfo.InvariantCulture, "-file-exec-and-symbols \"{0}\" -p remote-ios -r \"{1}\"", _launchOptions.ExePath, _appRemotePath);
            }
            else
            {
                fileCommand = string.Format(CultureInfo.InvariantCulture, "-file-exec-and-symbols \"{0}\" -p ios-simulator", _launchOptions.ExePath);
            }

            string targetCommand = string.Format(CultureInfo.InvariantCulture, "-target-select remote localhost:{0}", _remotePorts.IDeviceDebugServerProxyPort.ToString(CultureInfo.InvariantCulture));
            string breakInMainCommand = string.Format(CultureInfo.InvariantCulture, "-break-insert main");

            commands.Add(new LaunchCommand(fileCommand, LauncherResources.DefinePlatform));
            commands.Add(new LaunchCommand(targetCommand, LauncherResources.Connecting));
            commands.Add(new LaunchCommand(breakInMainCommand, LauncherResources.SettingBreakpoint));

            if (_launchOptions.IOSDebugTarget == IOSDebugTarget.Simulator)
            {
                string file = Path.GetFileName(_launchOptions.ExePath);
                if (!String.IsNullOrWhiteSpace(file) && file.EndsWith(".app", StringComparison.Ordinal))
                {
                    file = file.Substring(0, file.Length - 4);
                }
                if (!String.IsNullOrWhiteSpace(file))
                {
                    string targetAttachCommand = string.Format(CultureInfo.InvariantCulture, "-target-attach -n {0}  --waitfor", file);
                    string launchMessage = string.Format(CultureInfo.CurrentCulture, LauncherResources.WaitingForApp, file);
                    commands.Add(new LaunchCommand(targetAttachCommand, launchMessage));
                }
                else
                {
                    throw new Exception(String.Format(CultureInfo.InvariantCulture, LauncherResources.BadNameFormat, _launchOptions.ExePath));
                }
            }
            return commands.AsReadOnly();
        }

        private LaunchCompleteCommand GetLaunchCompleteCommand()
        {
            if (_launchOptions.IOSDebugTarget == IOSDebugTarget.Simulator)
            {
                return LaunchCompleteCommand.ExecContinue;
            }
            else
            {
                return LaunchCompleteCommand.ExecRun;
            }
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
