using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Provides interactions with the host's debugger
    /// </summary>
    public static class HostDebugger
    {
        /// <summary>
        /// Attach to a process using the provided options
        /// </summary>
        public static void Attach(string filePath, string options, Guid engineId)
        {
            try
            {
                ThreadHelper.Generic.BeginInvoke(() => Internal.LaunchDebugTarget(filePath, options, engineId));
            }
            catch (Exception)
            {
            }
        }

        private static class Internal
        {
            public static void LaunchDebugTarget(string filePath, string options, Guid engineId)
            {
                IVsDebugger4 debugger = (IVsDebugger4)Package.GetGlobalService(typeof(IVsDebugger));
                VsDebugTargetInfo4[] debugTargets = new VsDebugTargetInfo4[1];
                debugTargets[0].dlo = (uint)DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;
                debugTargets[0].bstrExe = filePath;
                debugTargets[0].bstrOptions = options;
                debugTargets[0].guidLaunchDebugEngine = engineId;
                VsDebugTargetProcessInfo[] processInfo = new VsDebugTargetProcessInfo[debugTargets.Length];

                debugger.LaunchDebugTargets4(1, debugTargets, processInfo);
            }
        }
    }
}
