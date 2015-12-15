// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Static class which provides the initialization method for Microsoft.DebugEngineHost
    /// </summary>
    public static class Host
    {
        /// <summary>
        /// Called by a debug engine to ensure that the main thread is initialized.
        /// </summary>
        public static void EnsureMainThreadInitialized()
        {
            //This call is to initialize the global service provider while we are still on the main thread.
            //Do not remove this this, even though the return value goes unused.
            var globalProvider = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider;
        }
    }
}
