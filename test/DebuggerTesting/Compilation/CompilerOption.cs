// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting.Compilation
{
    [Flags]
    public enum CompilerOption
    {
        None = 0x0,
        GenerateSymbols = 0x1,
        SupportThreading = 0x2,
        OptimizeLevel1 = 0x4,  
        OptimizeLevel2 = 0x8,
        OptimizeLevel3 = 0x10
    }
}
