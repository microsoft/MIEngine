// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

#if !CORECLR
        [DllImport("libc")]
        static extern int uname(IntPtr buf);

        private static RuntimePlatform GetUnixVariant()
        {
            IntPtr buf = Marshal.AllocHGlobal(8192);
            try
            {
                if (uname(buf) == 0)
                {
                    string os = Marshal.PtrToStringAnsi(buf);
                    if (String.Equals(os, "Darwin", StringComparison.Ordinal))
                    {
                        return RuntimePlatform.MacOSX;
                    }
                }
            }
            finally
            {
                if (buf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buf);
                }
            }

            return RuntimePlatform.Unix;
        }
#endif

        private static RuntimePlatform CalculateRuntimePlatform()
        {
#if CORECLR
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
#else
            const PlatformID MonoOldUnix = (PlatformID)128;

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    return RuntimePlatform.Windows;
                case PlatformID.Unix:
                case MonoOldUnix:
                    // Mono returns PlatformID.Unix on OSX for compatibility
                    return PlatformUtilities.GetUnixVariant();
                case PlatformID.MacOSX:
                    return RuntimePlatform.MacOSX;
                default:
                    return RuntimePlatform.Unknown;
            }
#endif
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
#if CORECLR
            processStartInfo.Environment[key] = value;
#else
            // Desktop CLR has the Environment property in 4.6+, but Mono is currently based on 4.5.
            processStartInfo.EnvironmentVariables[key] = value;
#endif
        }

        // Abstract API call to add an environment variable to a new process
        public static string GetEnvironmentVariable(this ProcessStartInfo processStartInfo, string key)
        {
#if CORECLR
            if (processStartInfo.Environment.ContainsKey(key))
                return processStartInfo.Environment[key];
#else
            // Desktop CLR has the Environment property in 4.6+, but Mono is currenlty based on 4.5.
            if (processStartInfo.EnvironmentVariables.ContainsKey(key))
                return processStartInfo.EnvironmentVariables[key];
#endif
            return null;
        }
    }
}

