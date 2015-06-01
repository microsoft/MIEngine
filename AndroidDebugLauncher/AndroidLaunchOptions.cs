// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Globalization;

namespace AndroidDebugLauncher
{
    internal class AndroidLaunchOptions
    {
        private AndroidLaunchOptions(XmlReader reader)
        {
            this.Package = LaunchOptions.GetRequiredAttribute(reader, "Package");
            this.IsAttach = GetIsAttachAttribute(reader);
            if (!IsAttach)
            {
                // LaunchActivity is only required when we're launching
                this.LaunchActivity = LaunchOptions.GetRequiredAttribute(reader, "LaunchActivity");
            }
            this.SDKRoot = GetOptionalDirectoryAttribute(reader, "SDKRoot");
            this.NDKRoot = GetOptionalDirectoryAttribute(reader, "NDKRoot");
            this.TargetArchitecture = LaunchOptions.GetTargetArchitectureAttribute(reader);
            this.IntermediateDirectory = GetRequiredDirectoryAttribute(reader, "IntermediateDirectory");
            this.AdditionalSOLibSearchPath = reader.GetAttribute("AdditionalSOLibSearchPath");
            this.DeviceId = LaunchOptions.GetRequiredAttribute(reader, "DeviceId");
            this.LogcatServiceId = GetLogcatServiceIdAttribute(reader);

            CheckTargetArchitectureSupported();
        }

        public static AndroidLaunchOptions CreateFromXml(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentNullException("content");

            var settings = new XmlReaderSettings();
            settings.CloseInput = false;
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;

            using (StringReader stringReader = new StringReader(content))
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                // Read to the top level element
                while (reader.NodeType != XmlNodeType.Element)
                    reader.Read();

                // Allow either no namespace, or the correct namespace
                if (reader.LocalName != "AndroidLaunchOptions")
                {
                    throw new ArgumentOutOfRangeException("content");
                }

                return new AndroidLaunchOptions(reader);
            }
        }

        private string GetOptionalDirectoryAttribute(XmlReader reader, string attributeName)
        {
            string value = reader.GetAttribute(attributeName);
            if (value == null)
                return null;

            EnsureValidDirectory(value, attributeName);
            return value;
        }

        private string GetRequiredDirectoryAttribute(XmlReader reader, string attributeName)
        {
            string value = LaunchOptions.GetRequiredAttribute(reader, attributeName);

            EnsureValidDirectory(value, attributeName);

            return value;
        }

        private void EnsureValidDirectory(string value, string attributeName)
        {
            if (value.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ||
                !Path.IsPathRooted(value) ||
                !Directory.Exists(value))
            {
                throw new LauncherException(Telemetry.LaunchFailureCode.NoReport, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_InvalidDirectoryAttribute, attributeName, value));
            }
        }

        private Guid GetLogcatServiceIdAttribute(XmlReader reader)
        {
            const string attributeName = "LogcatServiceId";

            string attributeValue = reader.GetAttribute(attributeName);
            if (!string.IsNullOrEmpty(attributeValue))
            {
                Guid value;
                if (!Guid.TryParse(attributeValue, out value))
                {
                    throw new LauncherException(Telemetry.LaunchFailureCode.NoReport, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_InvalidAttribute, attributeName));
                }

                return value;
            }
            else
            {
                return Guid.Empty;
            }
        }

        private bool GetIsAttachAttribute(XmlReader reader)
        {
            const string attributeName = "Attach";

            string attributeValue = reader.GetAttribute(attributeName);
            if (string.IsNullOrWhiteSpace(attributeValue))
            {
                // LaunchOptions.xsd specifies false as default
                return false;
            }

            bool isAttach;
            if (!Boolean.TryParse(attributeValue, out isAttach))
            {
                throw new LauncherException(Telemetry.LaunchFailureCode.NoReport, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_InvalidAttribute, attributeName));
            }

            return isAttach;
        }

        /// <summary>
        /// [Required] Package name to spawn
        /// </summary>
        public string Package { get; private set; }

        /// <summary>
        /// [Otprional] Activity name to spawn
        /// 
        /// This is required for a launch
        /// This is not required for an attach
        /// </summary>
        public string LaunchActivity { get; private set; }

        /// <summary>
        /// [Optional] Root of the Android SDK
        /// </summary>
        public string SDKRoot { get; private set; }

        /// <summary>
        /// [Optional] Root of the Android NDK
        /// </summary>
        public string NDKRoot { get; private set; }

        /// <summary>
        /// [Required] Target architecture of the application
        /// </summary>
        public TargetArchitecture TargetArchitecture { get; private set; }

        private void CheckTargetArchitectureSupported()
        {
            switch (this.TargetArchitecture)
            {
                case MICore.TargetArchitecture.X86:
                case MICore.TargetArchitecture.ARM:
                    return;

                default:
                    throw new LauncherException(Telemetry.LaunchFailureCode.NoReport, string.Format(CultureInfo.CurrentCulture, LauncherResources.UnsupportedTargetArchitecture, this.TargetArchitecture));
            }
        }

        /// <summary>
        /// [Required] Directory where files from the device/emulator will be downloaded to.
        /// </summary>
        public string IntermediateDirectory { get; private set; }

        /// <summary>
        /// [Optional] Additional directories to add to the search path
        /// </summary>
        public string AdditionalSOLibSearchPath { get; private set; }

        /// <summary>
        /// [Required] ADB device ID of the device/emulator to target
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// [Optional] The VS Service id of the logcat service used by the launching project system
        /// </summary>
        public Guid LogcatServiceId { get; private set; }

        /// <summary>
        /// [Optional] Set to true if we are performing an attach instead of a launch. Default is false.
        /// </summary>
        public bool IsAttach { get; private set; }
    }
}
