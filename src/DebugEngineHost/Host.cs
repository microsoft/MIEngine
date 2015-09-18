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
        public static void EnsureMainThreadInitialized()
        {
            //This call is to initialize the global service provider while we are still on the main thread.
            //Do not remove this this, even though the return value goes unused.
            var globalProvider = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider;
        }
    }
}
