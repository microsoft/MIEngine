// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting
{
    [Flags]
    public enum SupportedCompiler
    {
        ClangPlusPlus = 0x1,
        GPlusPlus = 0x2,
        VisualCPlusPlus = 0x4,
        XCodeBuild = 0x8
    }
}