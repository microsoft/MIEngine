// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DebugEngineHost.VSCode;

namespace OpenDebugAD7
{
    internal static class EngineConfigurationExtensions
    {
        public static bool IsCoreClr(this EngineConfiguration config)
        {
            return config != null && String.Equals(config.AdapterId, "coreclr", StringComparison.Ordinal);
        }

        public static bool IsCppDbg(this EngineConfiguration config)
        {
            return config != null
                && (String.Equals(config.AdapterId, "miengine", StringComparison.Ordinal)
                    || String.Equals(config.AdapterId, "cppdbg", StringComparison.Ordinal));
        }
    }
}
