// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using MICore;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace IOSDebugLauncher
{
    public enum IOSDebugTarget
    {
        Device,
        Simulator,
    }

    internal class IOSLaunchOptions
    {
        public IOSLaunchOptions(MICore.Xml.LaunchOptions.IOSLaunchOptions xmlOptions)
        {
            this.RemoteMachineName = LaunchOptions.RequireAttribute(xmlOptions.RemoteMachineName, "RemoteMachineName");
            this.PackageId = LaunchOptions.RequireAttribute(xmlOptions.PackageId, "PackageId");
            this.VcRemotePort = LaunchOptions.RequirePortAttribute(xmlOptions.vcremotePort, "vcremotePort");
            Debug.Assert((uint)IOSDebugTarget.Device == (uint)MICore.Xml.LaunchOptions.IOSLaunchOptionsIOSDebugTarget.device);
            Debug.Assert((uint)IOSDebugTarget.Simulator == (uint)MICore.Xml.LaunchOptions.IOSLaunchOptionsIOSDebugTarget.simulator);
            this.IOSDebugTarget = (IOSDebugTarget)xmlOptions.IOSDebugTarget;
            this.TargetArchitecture = LaunchOptions.ConvertTargetArchitectureAttribute(xmlOptions.TargetArchitecture);
            this.AdditionalSOLibSearchPath = xmlOptions.AdditionalSOLibSearchPath;
            this.Secure = xmlOptions.Secure;
        }

        public string RemoteMachineName { get; private set; }
        public string PackageId { get; private set; }
        public int VcRemotePort { get; private set; }
        public IOSDebugTarget IOSDebugTarget { get; private set; }
        public TargetArchitecture TargetArchitecture { get; private set; }
        public string AdditionalSOLibSearchPath { get; private set; }
        public bool Secure { get; private set; }
    }
}
