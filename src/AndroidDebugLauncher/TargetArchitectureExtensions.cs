// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using MICore;

namespace AndroidDebugLauncher
{
    internal static class TargetArchitectureExtensions
    {
        public static string ToNDKArchitectureName(this TargetArchitecture arch)
        {
            switch (arch)
            {
                case TargetArchitecture.X86:
                    return "x86";

                case TargetArchitecture.X64:
                    return "x64";

                case TargetArchitecture.ARM:
                    return "arm";

                case TargetArchitecture.ARM64:
                    return "arm64";

                default:
                    Debug.Fail("Should be impossible");
                    throw new InvalidOperationException();
            }
        }
    }
}
