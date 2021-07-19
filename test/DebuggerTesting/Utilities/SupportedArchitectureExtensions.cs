// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting.Utilities
{
    public static class SupportedArchitectureExtensions
    {
        #region Methods

        public static string ToArchitectureString(this SupportedArchitecture architecture)
        {
            switch (architecture)
            {
                case SupportedArchitecture.x86:
                    return "x86";
                case SupportedArchitecture.x64:
                    return "x64";
                case SupportedArchitecture.arm:
                    return "arm";
                default:
                    throw new ArgumentOutOfRangeException(nameof(architecture));
            }
        }

        #endregion
    }
}
