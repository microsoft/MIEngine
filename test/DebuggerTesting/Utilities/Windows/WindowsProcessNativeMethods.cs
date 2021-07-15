// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace DebuggerTesting.Utilities.Windows
{
    /// <summary>
    /// P/Invoke APIs to get process information
    /// </summary>
    internal static class WindowsProcessNativeMethods
    {
        #region CreateToolhelp32Snapshot

        [Flags]
        private enum SnapshotFlags : uint
        {
            Inherit = 0x80000000,
            All = (HeapList | Process | Thread | Module | Module32),
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot([In] SnapshotFlags flags, [In] uint processId);

        #endregion

        #region Process32

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessEntry32
        {
            public ProcessEntry32(uint? size) : this()
            {
                this.size = size ?? (uint)PlatformUtilities.MarshalSizeOf<ProcessEntry32>();
            }

            const int MAX_PATH = 260;
            public uint size;
            public uint unused;
            public uint processId;
            public UIntPtr defaultHeapId;
            public uint moduleId;
            public uint cntThreads;
            public uint parentProcessId;
            public int pcPriClassBase;
            public uint flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string exeFile;
        }

        private const int ERROR_NO_MORE_FILES = 18;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Process32First([In] IntPtr snapshot, [In, Out] ref ProcessEntry32 processEntry);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Process32Next([In] IntPtr snapshot, [In, Out] ref ProcessEntry32 processEntry);

        private static ProcessEntry32 Process32First(IntPtr snapshot)
        {
            ProcessEntry32 processEntry = new ProcessEntry32(size: null);
            if (!Process32First(snapshot, ref processEntry))
            {
                int error = Marshal.GetLastWin32Error();
                if (error != ERROR_NO_MORE_FILES)
                    throw new InvalidOperationException("Win32 Error 0x" + error.ToString("X8", CultureInfo.InvariantCulture));
            }
            return processEntry;
        }

        #endregion

        #region CloseHandle

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle([In] IntPtr handle);

        #endregion

        /// <summary>
        /// Gets the parent process id
        /// </summary>
        public static int? GetParentProcessId(int processId)
        {
            IntPtr snapshot = IntPtr.Zero;
            try
            {
                snapshot = CreateToolhelp32Snapshot(SnapshotFlags.Process, 0);
                ProcessEntry32 processEntry = Process32First(snapshot);
                do
                {
                    if (processEntry.processId == processId)
                        return (int)processEntry.parentProcessId;
                } while (Process32Next(snapshot, ref processEntry));
            }
            catch (Exception)
            { }
            finally
            {
                if (snapshot != IntPtr.Zero)
                    CloseHandle(snapshot);
            }
            return null;
        }
    }
}
