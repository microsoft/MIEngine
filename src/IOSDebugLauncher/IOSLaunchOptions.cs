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

namespace IOSDebugLauncher
{
    public enum IOSDebugTarget
    {
        Device,
        Simulator,
    }

    internal class IOSLaunchOptions
    {
        private IOSLaunchOptions(XmlReader reader)
        {
            this.RemoteMachineName = LaunchOptions.GetRequiredAttribute(reader, "RemoteMachineName");
            this.PackageId = LaunchOptions.GetRequiredAttribute(reader, "PackageId");
            this.VcRemotePort = int.Parse(LaunchOptions.GetRequiredAttribute(reader, "vcremotePort"), CultureInfo.InvariantCulture);
            this.IOSDebugTarget = GetIOSTargetArchitectureAttribute(reader);
            this.TargetArchitecture = LaunchOptions.GetTargetArchitectureAttribute(reader);
            this.AdditionalSOLibSearchPath = reader.GetAttribute("AdditionalSOLibSearchPath");

            this.Secure = false;
            string secureString = reader.GetAttribute("Secure");
            if (!string.IsNullOrEmpty(secureString))
            {
                bool secure;
                if (bool.TryParse(secureString, out secure))
                {
                    this.Secure = secure;
                }
            }
        }

        internal static IOSLaunchOptions CreateFromXml(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("content");

            var settings = new XmlReaderSettings();
            settings.CloseInput = false;
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;

            using (StringReader stringReader = new StringReader(content))
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                while (reader.NodeType != XmlNodeType.Element)
                    reader.Read();

                if (reader.LocalName != "IOSLaunchOptions")
                {
                    throw new ArgumentException("content");
                }

                return new IOSLaunchOptions(reader);
            }
        }

        private IOSDebugTarget GetIOSTargetArchitectureAttribute(XmlReader reader)
        {
            string iosDebugTarget = LaunchOptions.GetRequiredAttribute(reader, "IOSDebugTarget");
            switch (iosDebugTarget.ToLowerInvariant())
            {
                case "simulator":
                    return IOSDebugTarget.Simulator;

                case "device":
                    return IOSDebugTarget.Device;

                default:
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, LauncherResources.Error_UnknownDebugTarget, iosDebugTarget));
            }
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
