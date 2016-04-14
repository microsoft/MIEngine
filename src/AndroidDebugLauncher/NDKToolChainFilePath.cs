// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidDebugLauncher
{
    /// <summary>
    /// Represents a relative path within the ndk_root\toolchains folder which does fuzzy matching for tool chain version.
    /// </summary>
    internal class NDKToolChainFilePath: INDKFilePath
    {
        public string ToolChainName { get; }
        public string PreferredVersion { get; }
        public string PartialFilePath { get; }

        private NDKToolChainFilePath(string toolChainName, string preferredVersion, string partialFilePath)
        {
            this.ToolChainName = toolChainName;
            this.PreferredVersion = preferredVersion;
            this.PartialFilePath = partialFilePath;
        }

        public static NDKToolChainFilePath[] x86_GDBPaths()
        {
            // NOTE: In the r8 (and before) versions of the NDK, it is 'android-linux' instead of 'linux-android'. I don't know
            // if we actually will support these fairly old NDKs, but if so we will want another entry in this array.

            return new NDKToolChainFilePath[] {
                new NDKToolChainFilePath(@"x86", "4.8", @"prebuilt\windows\bin\i686-linux-android-gdb.exe"), // windows-x86 NDK path
                new NDKToolChainFilePath(@"x86", "4.8", @"prebuilt\windows-x86_64\bin\i686-linux-android-gdb.exe")  // windows-x86_x64 NDK path
            };
        }

        public static NDKToolChainFilePath[] x64_GDBPaths()
        {
            return new NDKToolChainFilePath[]
            {
                new NDKToolChainFilePath(@"x86_64", "4.9", @"prebuilt\windows\bin\x86_64-linux-android-gdb.exe"),
                new NDKToolChainFilePath(@"x86_64", "4.9", @"prebuilt\windows-x86_64\bin\x86_64-linux-android-gdb.exe")
            };
        }

        public static NDKToolChainFilePath[] ARM_GDBPaths()
        {
            return new NDKToolChainFilePath[] {
                new NDKToolChainFilePath("arm-linux-androideabi", "4.8", @"prebuilt\windows\bin\arm-linux-androideabi-gdb.exe"),
                // NOTE: The 4.8 GDB from the windows-x86_64 NDK doesn't work (Symbol format `elf32-littlearm' unknown)
                new NDKToolChainFilePath("arm-linux-androideabi", "4.6", @"prebuilt\windows-x86_64\bin\arm-linux-androideabi-gdb.exe")
            };
        }

        public static NDKToolChainFilePath[] ARM64_GDBPaths()
        {
            return new NDKToolChainFilePath[] {
                new NDKToolChainFilePath("aarch64-linux-android", "4.9", @"prebuilt\windows\bin\aarch64-linux-android-gdb.exe"),
                new NDKToolChainFilePath("aarch64-linux-android", "4.9", @"prebuilt\windows-x86_64\bin\aarch64-linux-android-gdb.exe")
            };
        }

        public string TryResolve(string ndkRoot)
        {
            string toolChainsDirectory = GetToolChainsDirectory(ndkRoot);

            // First check for the prefered version
            string preferredPath = GetFullPath(toolChainsDirectory, this.PreferredVersion);
            if (File.Exists(preferredPath))
                return preferredPath;

            // Sort the tool chains by version number to give us deterministic ordering
            IEnumerable<NdkToolVersion> toolChainVersions = GetToolChainVersions(toolChainsDirectory).OrderByDescending((v) => v);

            foreach (NdkToolVersion version in toolChainVersions)
            {
                string path = GetFullPath(toolChainsDirectory, version.ToString());
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        /// <summary>
        /// Returns the path that we will put in the error message if this file cannot be found
        /// </summary>
        /// <param name="ndkRoot">Path to the root of the NDK</param>
        /// <returns>Path name with '*' for the version number</returns>
        public string GetSearchPathDescription(string ndkRoot)
        {
            return GetFullPath(GetToolChainsDirectory(ndkRoot), "*");
        }

        public string GetPartialFilePath()
        {
            return PartialFilePath;
        }

        private static string GetToolChainsDirectory(string ndkRoot)
        {
            return Path.Combine(ndkRoot, "toolchains");
        }

        private string GetFullPath(string toolChainsDirectory, string version)
        {
            return Path.Combine(toolChainsDirectory, this.ToolChainName + "-" + version, this.PartialFilePath);
        }

        private IEnumerable<NdkToolVersion> GetToolChainVersions(string toolChainsDirectory)
        {
            if (!Directory.Exists(toolChainsDirectory))
                yield break;

            foreach (string subDirectoryName in Directory.EnumerateDirectories(toolChainsDirectory, this.ToolChainName + "-*"))
            {
                string versionString = subDirectoryName.Substring(toolChainsDirectory.Length + 1 /* for '\' */ + this.ToolChainName.Length + 1 /* for '-' */);

                NdkToolVersion version;
                if (NdkToolVersion.TryParse(versionString, out version))
                    yield return version;
            }
        }
    }
}
