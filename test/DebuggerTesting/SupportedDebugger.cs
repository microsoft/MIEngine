// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting
{
    [Flags]
    public enum SupportedDebugger
    {
        Gdb_Gnu = 0x1,
        Gdb_Cygwin = 0x2,
        Gdb_MinGW = 0x4,
        Lldb = 0x8,
        VsDbg = 0x10,
    }
}