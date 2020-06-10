// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Provides acceess for MIEngine to determine if it is in an EvaluateRequest with specific contexes that do not
    /// exist in enum_PARSEFLAGS.
    /// </summary>
    public sealed class  HostEvaluateRequestContext
    {
        private static bool isClipboardContext;

        /// <summary>
        /// Returns the boolean if the current EvaluareRequest's context is a clipboard context.
        /// </summary>
        /// <returns>true if it is in an evaluate request with a clipboard context</returns>
        public static bool IsClipboardContext()
        {
            return isClipboardContext;
        }

        /// <summary>
        /// Sets the current value of Clipboard Context
        /// </summary>
        /// <param name="clipboardContext"></param>
        public static void SetClipboardContext(bool clipboardContext)
        {
            isClipboardContext = clipboardContext;
        }
    }
}