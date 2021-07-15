// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DebuggerTesting.Utilities
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

        private static RuntimePlatform _runtimePlatform;

        private static RuntimePlatform GetRuntimePlatform()
        {
            if (_runtimePlatform == RuntimePlatform.Unset)
            {
                _runtimePlatform = CalculateRuntimePlatform();
            }
            return _runtimePlatform;
        }

        private static RuntimePlatform CalculateRuntimePlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RuntimePlatform.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return RuntimePlatform.MacOSX;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return RuntimePlatform.Unix;
            }
            return RuntimePlatform.Unknown;
        }

        /*
         * Is this Windows?
         */
        public static bool IsWindows
        {
            get
            {
                return GetRuntimePlatform() == RuntimePlatform.Windows;
            }
        }

        /*
         * Is this OS X?
         */
        public static bool IsOSX
        {
            get
            {
                return GetRuntimePlatform() == RuntimePlatform.MacOSX;
            }
        }

        /*
         * Is this Linux?
         */
        public static bool IsLinux
        {
            get
            {
                return GetRuntimePlatform() == RuntimePlatform.Unix;
            }
        }

        public static bool Is64Bit
        {
            get
            {
                return true;
            }
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

        // Abstract API call to Marshal.SizeOf
        public static int MarshalSizeOf<T>()
        {
            return Marshal.SizeOf<T>();
        }
    }
}

