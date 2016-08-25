// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DebugEngineHost;
using MICore.Xml.LaunchOptions;
using System.Diagnostics;

namespace Microsoft.SSHDebugPS
{
    /// <summary>
    /// Implementation of the MIEngine's IPlatformAppLauncher interface used to connect the SSH port supplier to the MIEngine
    /// for launch scenarios.
    /// 
    /// Note that in these scenarios the SSH port supplier doesn't actually operate as a true port supplier -- we are still
    /// using the default port supplier as far as AD7 is concerned, and the SSH port supplier is just acting as a transport.
    /// </summary>
    [ComVisible(true)]
    [Guid("7E3052B2-FB42-4E38-B22C-1FD281BD4413")]
    sealed public class MIEngineLauncher : IPlatformAppLauncher
    {
        private SSHLaunchOptions _launchOptions;

        void IDisposable.Dispose()
        {
        }

        void IPlatformAppLauncher.Initialize(HostConfigurationStore configStore, IDeviceAppLauncherEventCallback eventCallback)
        {
        }

        void IPlatformAppLauncher.OnResume()
        {
        }

        void IPlatformAppLauncher.SetLaunchOptions(string exePath, string args, string dir, object launcherXmlOptions, TargetEngine targetEngine)
        {
            // NOTE: exePath/args/dir can all be ignored, as LaunchOptions.GetInstance will use those values if they aren't specified in the XML.

            _launchOptions = (SSHLaunchOptions)launcherXmlOptions;
        }

        void IPlatformAppLauncher.SetupForDebugging(out LaunchOptions debuggerLaunchOptions)
        {
            if (_launchOptions == null)
            {
                Debug.Fail("Why is SetupForDebugging being called before ParseLaunchOptions?");
                throw new InvalidOperationException();
            }

            string targetMachineName = LaunchOptions.RequireAttribute(_launchOptions.TargetMachine, "TargetMachine");

            var port = new AD7Port(new AD7PortSupplier(), targetMachineName, isInAddPort: false);

            // NOTE: this may put up a dialog and/or throw an AD7ConnectCanceledException
            port.EnsureConnected();

            debuggerLaunchOptions = new UnixShellPortLaunchOptions(_launchOptions.StartRemoteDebuggerCommand,
                                                                    port,
                                                                    LaunchOptions.ConvertMIModeAttribute(_launchOptions.MIMode),
                                                                    _launchOptions);
        }

        void IPlatformAppLauncher.Terminate()
        {
        }
    }
}
