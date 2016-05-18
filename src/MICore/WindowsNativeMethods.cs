// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace MICore
{
    internal class WindowsNativeMethods
    {
#if CORECLR
        [DllImport("kernel32")]
        internal static extern bool DebugBreakProcess(SafeHandle hProcess);
#else
        [DllImport("kernel32")]
        internal static extern bool DebugBreakProcess(IntPtr hProcess);
#endif

    }
}
