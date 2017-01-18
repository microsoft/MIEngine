// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.Internal.VisualStudio.Shell.Interop;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Enumeration of Host User Interfaces that an engine can be run from.
    /// This must be kept in sync with all DebugEngineHost implentations
    /// </summary>
    public enum HostUIIdentifier
    {
        /// <summary>
        /// Visual Studio IDE
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        VSIDE = 0,
        /// <summary>
        /// Visual Studio Code
        /// </summary>
        VSCode = 1,
        /// <summary>
        /// XamarinStudio
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        XamarinStudio = 2
    }

    /// <summary>
    /// Static class which provides the initialization method for Microsoft.DebugEngineHost
    /// </summary>
    public static class Host
    {
        // Seperate class to make sure that we can catch any exceptions from the missing shell assemblies in glass
        static internal class Impl
        {
            internal static void EnsureMainThreadInitialized()
            {
                // This call is to initialize the global service provider while we are still on the main thread.
                // Do not remove this this, even though the return value goes unused.
                var globalProvider = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider;

#if LAB
                // Force the IVsTelemetryService to complete its lazy loading. There is a hang caused by trying to
                // send telemetry while Visual Studio is launching the debugger if the telemetry helper also needs 
                // to load the telemetry service on the main thread.
                // Do not remove this this, even though the return value goes unused.
                var telemetryService = TelemetryHelper.TelemetryService;
#endif
            }
        }

        /// <summary>
        /// Called by a debug engine to ensure that the main thread is initialized.
        /// </summary>
        public static void EnsureMainThreadInitialized()
        {
            try
            {
                Impl.EnsureMainThreadInitialized();
            }
            catch
            {
                // In glass, VS types will be missing. Ignore the exceptions.
            }
        }

        /// <summary>
        /// Called by a debug engine to determine which UI is using it.
        /// </summary>
        /// <returns></returns>
        public static HostUIIdentifier GetHostUIIdentifier()
        {
            return HostUIIdentifier.VSIDE;
        }
    }
}
