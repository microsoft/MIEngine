// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MICore
{
    public static class PlatformUtilities
    {
        private enum RuntimePlatform
        {
            Unset = 0,
            Unknown,
            Windows,
            MacOSX,
            Unix,
        }

        private static RuntimePlatform s_runtimePlatform;

        private static RuntimePlatform GetRuntimePlatform()
        {
            if (s_runtimePlatform == RuntimePlatform.Unset)
            {
                s_runtimePlatform = CalculateRuntimePlatform();
            }
            return s_runtimePlatform;
        }

        private static RuntimePlatform CalculateRuntimePlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    return RuntimePlatform.Windows;
                case PlatformID.Unix:
                    return RuntimePlatform.Unix;
                case PlatformID.MacOSX:
                    return RuntimePlatform.MacOSX;
                default:
                    return RuntimePlatform.Unknown;
            }
        }


        /*
         * Is this Windows?
         */
        public static bool IsWindows()
        {
            return GetRuntimePlatform() == RuntimePlatform.Windows;
        }

        /*
         * Is this OS X?
         */
        public static bool IsOSX()
        {
            return GetRuntimePlatform() == RuntimePlatform.MacOSX;
        }

        /*
         * Is this Linux?
         */
        public static bool IsLinux()
        {
            return GetRuntimePlatform() == RuntimePlatform.Unix;
        }

        // Abstract API call to add an environment variable to a new process
        public static void SetEnvironmentVariable(this ProcessStartInfo processStartInfo, string key, string value)
        {
            processStartInfo.Environment[key] = value;
        }

        // Abstract API call to add an environment variable to a new process
        public static string GetEnvironmentVariable(this ProcessStartInfo processStartInfo, string key)
        {
            if (processStartInfo.Environment.ContainsKey(key))
                return processStartInfo.Environment[key];

            return null;
        }

        public static string UnixPathToWindowsPath(string unixPath)
        {
            return unixPath.Replace('/', '\\');
        }

        public static string WindowsPathToUnixPath(string windowsPath)
        {
            return windowsPath.Replace('\\', '/');
        }

        public static string PathToHostOSPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            if (IsWindows())
            {
                return UnixPathToWindowsPath(path);
            }
            else
            {
                return WindowsPathToUnixPath(path);
            }
        }
    }
}

