// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.DebugEngineHost;
using OpenDebugAD7;

namespace OpenDebug
{
    public static class Utilities
    {
        private const string OSASCRIPT = "/usr/bin/osascript";  // osascript is the AppleScript interpreter on OS X
        private const string LINUX_TERM = "/usr/bin/gnome-terminal";    //private const string LINUX_TERM = "/usr/bin/x-terminal-emulator";
        private const string OSX_BIN_DIR = "/usr/local/bin";

        private static readonly Regex s_VARIABLE = new Regex(@"\{(\w+)\}");

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

#if XPLAT
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
#if !XPLAT
            return RuntimePlatform.Windows;
#else
            const PlatformID MonoOldUnix = (PlatformID)128;

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    return RuntimePlatform.Windows;
                case PlatformID.Unix:
                case MonoOldUnix:
                    // Mono returns PlatformID.Unix on OSX for compatibility
                    return Utilities.GetUnixVariant();
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

        /*
         * Is this running on Mono
         */
        public static bool IsMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        // Abstract API call to add an environment variable to a new process
        public static void SetEnvironmentVariable(this ProcessStartInfo processStartInfo, string key, string value)
        {
#if !XPLAT
            processStartInfo.Environment[key] = value;
#else
            // Desktop CLR has the Environment property in 4.6+, but Mono is currently based on 4.5.
            processStartInfo.EnvironmentVariables[key] = value;
#endif
        }

        // Abstract API call to add an environment variable to a new process
        public static string GetEnvironmentVariable(this ProcessStartInfo processStartInfo, string key)
        {
#if !XPLAT
            if (processStartInfo.Environment.ContainsKey(key))
                return processStartInfo.Environment[key];
#else
            // Desktop CLR has the Environment property in 4.6+, but Mono is currenlty based on 4.5.
            if (processStartInfo.EnvironmentVariables.ContainsKey(key))
                return processStartInfo.EnvironmentVariables[key];
#endif
            return null;
        }

        /*
         * On OS X make sure that /usr/local/bin is on the PATH
         */
        public static void FixPathOnOSX()
        {
            if (Utilities.IsOSX())
            {
                var path = System.Environment.GetEnvironmentVariable("PATH");
                if (!path.Split(':').Contains(OSX_BIN_DIR))
                {
                    path += ":" + OSX_BIN_DIR;
                    System.Environment.SetEnvironmentVariable("PATH", path);
                }
            }
        }

        /// <summary>
        /// Normalize path by removing relative pathing if requested, get file casing from the OS
        /// </summary>
        /// <param name="fixCasing">Fix casing by querying the file system. This requires the file be on disk.</param>
        /// <returns>If successful, the correctly cased path and filename. Otherwise, what was passed in.</returns>
        public static string NormalizeFileName(string origPath, bool fixCasing = false)
        {
            try
            {
                // If the path isn't rooted, don't do anything
                if (!IsFilePathRooted(origPath))
                {
                    return origPath;
                }

                Stack<string> origPathParts = new Stack<string>();
                Stack<string> normalizedPathParts = new Stack<string>();

                string pathRoot = Path.GetPathRoot(origPath);

                // push into the stack to reverse it so it can easily remove relativeness in the path such as c:\path1\..\path2. 
                string fileSegment = Path.GetFileName(origPath);
                string dirSegment = Path.GetDirectoryName(origPath);
                while (!String.IsNullOrWhiteSpace(fileSegment))
                {
                    origPathParts.Push(fileSegment);

                    if (String.Equals(dirSegment, pathRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    fileSegment = Path.GetFileName(dirSegment);
                    dirSegment = Path.GetDirectoryName(dirSegment);
                }

                while (origPathParts.Any())
                {
                    string pathPart = origPathParts.Pop();
                    if (String.Equals(pathPart, "..", StringComparison.Ordinal))
                    {
                        // Don't pop if its the top rooted path. This is what Windows does today
                        if (normalizedPathParts.Any())
                        {
                            normalizedPathParts.Pop();
                        }
                    }
                    else if (String.Equals(pathPart, ".", StringComparison.Ordinal))
                    {
                        // no-op
                    }
                    else
                    {
                        if (fixCasing && !String.IsNullOrWhiteSpace(pathRoot))
                        {
                            string casedPath = Path.Combine(pathRoot, Path.Combine(normalizedPathParts.Reverse().ToArray()));
                            string casedPathPart = Directory.EnumerateFileSystemEntries(casedPath, pathPart).Select(p => Path.GetFileName(p)).FirstOrDefault();
                            if (!String.IsNullOrWhiteSpace(casedPathPart))
                            {
                                pathPart = casedPathPart;
                            }
                            else
                            {
                                // some reason it couldn't find it so stop checking
                                fixCasing = false;
                            }
                        }
                        normalizedPathParts.Push(pathPart);
                    }
                }

                // Ensure the drive letter is lower case - GetPathRoot doesn't alter it
                if (pathRoot.Length > 2 && pathRoot[1] == ':')
                {
                    pathRoot = String.Format(CultureInfo.CurrentCulture, "{0}{1}", Char.ToLowerInvariant(pathRoot[0]), pathRoot.Substring(1));
                }

                string path = Path.Combine(pathRoot, Path.Combine(normalizedPathParts.Reverse().ToArray()));
                return path;
            }
            catch
            {
                // Failed, so use original value
                return origPath;
            }
        }

        private static bool IsFilePathRooted(string path)
        {
            // Path.IsPathRooted is a bit more loosy-goosey than what we want (ex: "\foo.txt" and "c:foo.txt" is considered rooted on Windows),
            // so lets check ourself.
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            if (Utilities.IsWindows())
            {
                char firstCharUpper = char.ToUpperInvariant(path[0]);
                if (path.Length >= 3 && firstCharUpper >= 'A' && firstCharUpper <= 'Z' && path[1] == ':' && (path[2] == '\\' || path[2] == '/'))
                {
                    return true;
                }
                else if (path.StartsWith(@"\\", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            else
            {
                if (path[0] == '/')
                {
                    return true;
                }
            }

            return false;
        }

        /*
         * Resolve hostname, dotted-quad notation for IPv4, or colon-hexadecimal notation for IPv6 to IPAddress.
         * Returns null on failure.
         */
        public static IPAddress ResolveIPAddress(string addressString)
        {
            try
            {
                IPAddress ipaddress = null;
                if (IPAddress.TryParse(addressString, out ipaddress))
                {
                    return ipaddress;
                }

                IPHostEntry entry = Dns.GetHostEntryAsync(addressString).Result;
                if (entry != null && entry.AddressList != null && entry.AddressList.Length > 0)
                {
                    if (entry.AddressList.Length == 1)
                    {
                        return entry.AddressList[0];
                    }
                    foreach (IPAddress address in entry.AddressList)
                    {
                        if (address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return address;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // fall through
            }

            return null;
        }

        /*
         * Find a free socket port.
         */
        public static int FindFreePort(int fallback)
        {
            TcpListener l = null;
            try
            {
                l = new TcpListener(IPAddress.Loopback, 0);
                l.Start();
                return ((IPEndPoint)l.LocalEndpoint).Port;
            }
            catch (Exception)
            {
                // ignore
            }
            finally
            {
                l.Stop();
            }
            return fallback;
        }

        internal static string GetExceptionDescription(Exception exception)
        {
            if (!IsCorruptingException(exception))
            {
                return exception.Message;
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_CorruptingException, exception.GetType().FullName, exception.StackTrace);
            }
        }

        public static bool IsCorruptingException(Exception exception)
        {
            if (exception is NullReferenceException)
                return true;
            if (exception is ArgumentNullException)
                return true;
            if (exception is ArithmeticException)
                return true;
            if (exception is ArrayTypeMismatchException)
                return true;
            if (exception is DivideByZeroException)
                return true;
            if (exception is IndexOutOfRangeException)
                return true;
            if (exception is InvalidCastException)
                return true;
            if (exception is System.Runtime.InteropServices.SEHException)
                return true;

            return false;
        }

        public static void ReportException(Exception e)
        {
            try
            {
                HostTelemetry.ReportCurrentException(e, null);
                Logger.WriteLine("EXCEPTION: " + e.ToString());
            }
            catch
            {
                // If anything goes wrong, ignore it. We want to report the original exception, not a telemetry problem
            }
        }

        internal static Exception GetInnerMost(Exception e)
        {
            while (e.InnerException != null)
            {
                e = e.InnerException;
            }

            return e;
        }
    }
}
