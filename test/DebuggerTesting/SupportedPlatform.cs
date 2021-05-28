// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting
{
    [Flags]
    public enum SupportedPlatform
    {
        Linux = 0x1,
        MacOS = 0x2,
        Windows = 0x4
    }
}