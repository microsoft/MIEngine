// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace MICore
{
    internal class NativeMethods
    {
        private const string Libc = "libc";

        [DllImport(Libc, EntryPoint = "kill", SetLastError = true)]
        internal static extern int Kill(int pid, int mode);

        [DllImport(Libc, EntryPoint = "mkfifo", SetLastError = true)]
        internal static extern int MkFifo(string name, int mode);

        [DllImport(Libc, EntryPoint = "geteuid", SetLastError = true)]
        internal static extern uint GetEUid();
    }
}
