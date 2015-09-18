// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Provides direct access to the underlying output window without going through debug events
    /// </summary>
    public static class HostOutputWindow
    {
        // Use an extra class so that we have a seperate class which depends on VS interfaces
        private static class VsImpl
        {
            internal static void SetText(string outputMessage)
            {
                int hr;

                var outputWindow = (IVsOutputWindow)Package.GetGlobalService(typeof(SVsOutputWindow));
                if (outputWindow == null)
                    return;

                IVsOutputWindowPane pane;
                Guid guidDebugOutputPane = VSConstants.GUID_OutWindowDebugPane;
                hr = outputWindow.GetPane(ref guidDebugOutputPane, out pane);
                if (hr < 0)
                    return;

                pane.Clear();
                pane.Activate();

                hr = pane.OutputString(outputMessage);
                if (hr < 0)
                    return;

                var shell = (IVsUIShell)Package.GetGlobalService(typeof(SVsUIShell));
                if (shell == null)
                    return;

                Guid commandSet = VSConstants.GUID_VSStandardCommandSet97;
                object inputVariant = null;
                shell.PostExecCommand(commandSet, (uint)VSConstants.VSStd97CmdID.OutputWindow, 0, ref inputVariant);
            }
        }

        /// <summary>
        /// Write text to the Debug VS Output window pane directly. This is used to write information before the session create event.
        /// </summary>
        /// <param name="outputMessage"></param>
        public static void WriteLaunchError(string outputMessage)
        {
            try
            {
                VsImpl.SetText(outputMessage);
            }
            catch (Exception)
            {
            }
        }
    }
}
