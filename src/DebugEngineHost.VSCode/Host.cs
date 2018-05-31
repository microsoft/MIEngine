// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Enumeration of Host User Interfaces that an engine can be run from.
    /// This must be kept in sync with all DebugEngineHost implementations (see _Readme.txt)
    /// </summary>
    public enum HostUIIdentifier
    {
        /// <summary>
        /// Visual Studio IDE
        /// </summary>
        VSIDE = 0,
        /// <summary>
        /// Visual Studio Code
        /// </summary>
        VSCode = 1,
        /// <summary>
        /// XamarinStudio
        /// </summary>
        XamarinStudio = 2
    }

    public static class Host
    {
        public static void EnsureMainThreadInitialized()
        {
            // no initialization is required
        }

        /// <summary>
        /// Called by a debug engine to determine which UI is using it.
        /// </summary>
        /// <returns></returns>
        public static HostUIIdentifier GetHostUIIdentifier()
        {
            return HostUIIdentifier.VSCode;
        }
    }
}
