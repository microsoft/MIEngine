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
        public AndroidLaunchOptions(MICore.Xml.LaunchOptions.AndroidLaunchOptions xmlOptions, TargetEngine targetEngine)
        {
            if (xmlOptions == null)
            {
                throw new ArgumentNullException("xmlOptions");
            }

            this.Package = LaunchOptions.RequireAttribute(xmlOptions.Package, "Package");
            this.IsAttach = xmlOptions.Attach;
            if (!IsAttach)
            {
                // LaunchActivity is only required when we're launching
                this.LaunchActivity = LaunchOptions.RequireAttribute(xmlOptions.LaunchActivity, "LaunchActivity");
            }
            this.SDKRoot = GetOptionalDirectoryAttribute(xmlOptions.SDKRoot, "SDKRoot");
            this.NDKRoot = GetOptionalDirectoryAttribute(xmlOptions.NDKRoot, "NDKRoot");
            this.TargetArchitecture = LaunchOptions.ConvertTargetArchitectureAttribute(xmlOptions.TargetArchitecture);

            if (targetEngine == TargetEngine.Native)
                this.IntermediateDirectory = RequireValidDirectoryAttribute(xmlOptions.IntermediateDirectory, "IntermediateDirectory");
            else
                this.IntermediateDirectory = GetOptionalDirectoryAttribute(xmlOptions.IntermediateDirectory, "IntermediateDirectory");

            if (targetEngine == TargetEngine.Java)
            {
                this.JVMHost = LaunchOptions.RequireAttribute(xmlOptions.JVMHost, "JVMHost");
                this.JVMPort = xmlOptions.JVMPort;

                this.SourceRoots = GetSourceRoots(xmlOptions.SourceRoots);

                foreach (SourceRoot root in this.SourceRoots)
                {
                    EnsureValidDirectory(root.Path, "SourceRoots");
                }
            }

            this.AdditionalSOLibSearchPath = xmlOptions.AdditionalSOLibSearchPath;
            this.AbsolutePrefixSOLibSearchPath = xmlOptions.AbsolutePrefixSOLibSearchPath ?? "\"\"";
            this.DeviceId = LaunchOptions.RequireAttribute(xmlOptions.DeviceId, "DeviceId");
            this.LogcatServiceId = GetLogcatServiceIdAttribute(xmlOptions.LogcatServiceId);
            this.WaitDynamicLibLoad = xmlOptions.WaitDynamicLibLoad;

            CheckTargetArchitectureSupported();
        }

        public static SourceRoot[] GetSourceRoots(string pathList)
        {
            List<SourceRoot> sourceRoots = new List<MICore.SourceRoot>();

            if (!string.IsNullOrWhiteSpace(pathList))
            {
                string format = "{0}**";
                string wildcardEnding = string.Format(CultureInfo.InvariantCulture, format, Path.DirectorySeparatorChar);
                string altWildcardEnding = string.Format(CultureInfo.InvariantCulture, format, Path.AltDirectorySeparatorChar);

                foreach (string path in pathList.Split(new char[] { ';' }))
                {
                    string trimmedPath = path.Trim();
                    if (trimmedPath.EndsWith(wildcardEnding, StringComparison.OrdinalIgnoreCase) || trimmedPath.EndsWith(altWildcardEnding, StringComparison.OrdinalIgnoreCase))
                    {
                        string rootedPath = trimmedPath.Substring(0, trimmedPath.Length - 2);
                        sourceRoots.Add(new MICore.SourceRoot(rootedPath, true));
                    }
                    else
                    {
                        sourceRoots.Add(new MICore.SourceRoot(trimmedPath, false));
                    }
                }
            }

            return sourceRoots.ToArray();
        }

        private string GetOptionalDirectoryAttribute(string value, string attributeName)
        {
            if (value == null)
                return null;

            EnsureValidDirectory(value, attributeName);
            return value;
        }

        private string RequireValidDirectoryAttribute(string value, string attributeName)
        {
            LaunchOptions.RequireAttribute(value, attributeName);

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

        private Guid GetLogcatServiceIdAttribute(string attributeValue)
        {
            const string attributeName = "LogcatServiceId";

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
                case MICore.TargetArchitecture.X64:
                case MICore.TargetArchitecture.ARM:
                case MICore.TargetArchitecture.ARM64:
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
        /// [Optional] Absolute prefix for directories to search for shared library symbols
        /// </summary>
        public string AbsolutePrefixSOLibSearchPath { get; private set; }

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

        /// <summary>
        /// [Optional] If true, wait for dynamic library load to finish.
        /// </summary>
        public bool WaitDynamicLibLoad { get; private set; }

        public string JVMHost { get; private set; }

        public int JVMPort { get; private set; }

        public SourceRoot[] SourceRoots { get; private set; }
    }
}
