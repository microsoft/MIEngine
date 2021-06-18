// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using MICore;

namespace Microsoft.MIDebugEngine
{
    internal class CygwinFilePathMapper
    {
        private DebuggedProcess _debuggedProcess;
        private Dictionary<string, string> _cygwinToWindows;

        public CygwinFilePathMapper(DebuggedProcess debuggedProcess)
        {
            _debuggedProcess = debuggedProcess;
            _cygwinToWindows = new Dictionary<string, string>();
        }

        /// <summary>
        /// Maps cygwin paths (/usr/bin) to Windows Paths (C:\User\bin)
        ///
        /// We cache these paths because most of the time it comes from callstacks that we need to provide
        /// source file maps.
        /// </summary>
        /// <param name="origCygwinPath">A string representing the cygwin path.</param>
        /// <returns>A string as a full Windows path</returns>
        public string MapCygwinToWindows(string origCygwinPath)
        {
            if (!(_debuggedProcess.LaunchOptions is LocalLaunchOptions))
            {
                return origCygwinPath;
            }

            LocalLaunchOptions localLaunchOptions = (LocalLaunchOptions)_debuggedProcess.LaunchOptions;

            string cygwinPath = PlatformUtilities.WindowsPathToUnixPath(origCygwinPath);

            string windowsPath = cygwinPath;

            lock (_cygwinToWindows)
            {
                if (!_cygwinToWindows.TryGetValue(cygwinPath, out windowsPath))
                {
                    if (!LaunchCygPathAndReadResult(cygwinPath, localLaunchOptions.MIDebuggerPath, convertToWindowsPath: true, out windowsPath))
                    {
                        return origCygwinPath;
                    }

                    _cygwinToWindows.Add(cygwinPath, windowsPath);
                }
            }

            return windowsPath;
        }

        /// <summary>
        /// Maps Windows paths (C:\User\bin) to Cygwin Paths (/usr/bin)
        ///
        /// Not cached since we only do this for setting the program, symbol, and working dir.
        /// </summary>
        /// <param name="origWindowsPath">A string representing the Windows path.</param>
        /// <returns>A string as a full unix path</returns>
        public string MapWindowsToCygwin(string origWindowsPath)
        {
            if (!(_debuggedProcess.LaunchOptions is LocalLaunchOptions))
            {
                return origWindowsPath;
            }

            LocalLaunchOptions localLaunchOptions = (LocalLaunchOptions)_debuggedProcess.LaunchOptions;

            string windowsPath = PlatformUtilities.UnixPathToWindowsPath(origWindowsPath);

            if (!LaunchCygPathAndReadResult(windowsPath, localLaunchOptions.MIDebuggerPath, convertToWindowsPath: false, out string cygwinPath))
            {
                return origWindowsPath;
            }

            return cygwinPath;
        }

        // There is an issue launching Cygwin apps that if a process is launched using a bitness mismatched console,
        // process launch will fail. To avoid that, launch cygpath with its own console. This requires calling CreateProcess 
        // directly because the creation flags are not exposed in System.Diagnostics.Process. 
        //
        // Return true if successful. False otherwise.
        private bool LaunchCygPathAndReadResult(string inputPath, string miDebuggerPath, bool convertToWindowsPath, out string outputPath)
        {
            outputPath = "";

            if (String.IsNullOrEmpty(miDebuggerPath))
            {
                return false;
            }

            string cygpathPath = Path.Combine(Path.GetDirectoryName(miDebuggerPath), "cygpath.exe");
            if (!File.Exists(cygpathPath))
            {
                return false;
            }

            List<IDisposable> disposableHandles = new List<IDisposable>();

            try
            {
                // Create the anonymous pipe that will act as stdout for the cygpath process
                SECURITY_ATTRIBUTES pPipeSec = new SECURITY_ATTRIBUTES();
                pPipeSec.bInheritHandle = 1;
                SafeFileHandle stdoutRead;
                SafeFileHandle stdoutWrite;
                if (!CreatePipe(out stdoutRead, out stdoutWrite, ref pPipeSec, 4096))
                {
                    Debug.Fail("Unexpected failure CreatePipe in LaunchCygPathAndReadResult");
                    return false;
                }
                SetHandleInformation(stdoutRead, HANDLE_FLAGS.INHERIT, 0);
                disposableHandles.Add(stdoutRead);
                disposableHandles.Add(stdoutWrite);


                const int STARTF_USESTDHANDLES = 0x00000100;
                const int STARTF_USESHOWWINDOW = 0x00000001;
                const int SW_HIDE = 0;
                STARTUPINFO startupInfo = new STARTUPINFO();
                startupInfo.dwFlags = STARTF_USESTDHANDLES | STARTF_USESHOWWINDOW;
                startupInfo.hStdOutput = stdoutWrite;
                startupInfo.wShowWindow = SW_HIDE;
                startupInfo.cb = Marshal.SizeOf(startupInfo);

                PROCESS_INFORMATION processInfo = new PROCESS_INFORMATION();
                SECURITY_ATTRIBUTES processSecurityAttributes = new SECURITY_ATTRIBUTES();
                SECURITY_ATTRIBUTES threadSecurityAttributes = new SECURITY_ATTRIBUTES();

                processSecurityAttributes.nLength = Marshal.SizeOf(processSecurityAttributes);
                threadSecurityAttributes.nLength = Marshal.SizeOf(threadSecurityAttributes);

                const uint DETACHED_PROCESS = 0x00000008;
                uint flags = DETACHED_PROCESS;
                string command = string.Empty;
                if (convertToWindowsPath)
                {
                    //  -w, --windows         print Windows form of NAMEs (C:\WINNT)
                    // ex: "C:\\cygwin64\\bin\\cygpath.exe -w " + inputPath,
                    command = String.Concat(cygpathPath, " -w ", inputPath);
                }
                else
                {
                    // -u, --unix            (default) print Unix form of NAMEs (/cygdrive/c/winnt)
                    // ex: "C:\\cygwin64\\bin\\cygpath.exe -u " + inputPath,
                    command = String.Concat(cygpathPath, " -u ", inputPath);
                }
                if (!CreateProcess(
                        null,
                        command,
                        ref processSecurityAttributes,
                        ref threadSecurityAttributes,
                        true,
                        flags,
                        IntPtr.Zero,
                        null,
                        ref startupInfo,
                        out processInfo
                        ))
                {
                    Debug.Fail("Launching cygpath for source mapping failed");
                    return false;
                }
                SafeFileHandle processSH = new SafeFileHandle(processInfo.hProcess, true);
                SafeFileHandle threadSH = new SafeFileHandle(processInfo.hThread, true);
                disposableHandles.Add(processSH);
                disposableHandles.Add(threadSH);

                const int timeout = 5000;
                if (WaitForSingleObject(processInfo.hProcess, timeout) != 0)
                {
                    Debug.Fail("cygpath failed to map source file.");
                    return false;
                }

                uint exitCode = 0;
                if (!GetExitCodeProcess(processInfo.hProcess, out exitCode))
                {
                    Debug.Fail("cygpath failed to get exit code from cygpath.");
                    return false;
                }

                if (exitCode != 0)
                {
                    Debug.Fail("cygpath returned error exit code.");
                    return false;
                }

                FileStream fs = new FileStream(stdoutRead, FileAccess.Read);
                StreamReader sr = new StreamReader(fs);
                outputPath = sr.ReadLine();
            }
            finally
            {
                foreach (IDisposable h in disposableHandles)
                {
                    h.Dispose();
                }
            }

            return true;
        }

        #region pinvoke definitions
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public SafeFileHandle hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }


        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
            );

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            private IntPtr _lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        private static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        private static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, ref SECURITY_ATTRIBUTES lpPipeAttributes, uint nSize);

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DuplicateHandle(SafeFileHandle hSourceProcessHandle, SafeFileHandle hSourceHandle, SafeFileHandle hTargetProcessHandle, out SafeFileHandle lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [Flags]
        private enum HANDLE_FLAGS : uint
        {
            None = 0,
            INHERIT = 1,
            PROTECT_FROM_CLOSE = 2
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetHandleInformation(SafeFileHandle hObject, HANDLE_FLAGS dwMask, uint flags);
        #endregion
    }
}


