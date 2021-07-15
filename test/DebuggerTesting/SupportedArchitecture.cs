// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting
{
    [Flags]
    public enum SupportedArchitecture
    {
        x86 = 0x1,
        x64 = 0x2,
        arm = 0x4
    }
}