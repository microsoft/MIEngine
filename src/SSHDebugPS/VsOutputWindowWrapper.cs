// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.SSHDebugPS
{
    internal static class VsOutputWindowWrapper
    {
        private static readonly Lazy<IVsOutputWindow> s_outputWindowLazy = new Lazy<IVsOutputWindow>(() =>
        {
            IVsOutputWindow outputWindow = null;
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            }
            catch (Exception)
            {
                Debug.Fail("Could not get OutputWindow service.");
            }
            return outputWindow;
        }, LazyThreadSafetyMode.PublicationOnly);

        private static readonly Lazy<IVsUIShell> s_shellLazy = new Lazy<IVsUIShell>(() =>
        {
            IVsUIShell shell = null;
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            }
            catch (Exception)
            {
                Debug.Fail("Could not get VSShell service.");
            }
            return shell;
            // Use "PublicationOnly", because the implementation of GetService does its own locking
        }, LazyThreadSafetyMode.PublicationOnly);

        private class PaneInfo
        {
            public PaneInfo(Guid paneId)
            {
                this._paneId = paneId;
                this.Shown = false;
            }

            internal Guid _paneId;
            internal bool Shown { get; set; }
        }

        private const string s_defaultOutputPane = "Debug";

        private static readonly Dictionary<string, PaneInfo> s_panes = new Dictionary<string, PaneInfo>()
        {
            // The 'Debug' pane exists by default
            { s_defaultOutputPane, new PaneInfo(VSConstants.GUID_OutWindowDebugPane) }
        };

        /// <summary>
        /// Writes text directly to the VS Output window.
        /// </summary>
        public static void Write(string message, string pane = s_defaultOutputPane)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    // Get the Output window
                    IVsOutputWindow outputWindow = s_outputWindowLazy.Value;
                    if (outputWindow == null)
                    {
                        return;
                    }

                    // Get the pane guid
                    PaneInfo paneInfo;
                    if (!s_panes.TryGetValue(pane, out paneInfo))
                    {
                        // Pane didn't exist, create it
                        paneInfo = new PaneInfo(Guid.NewGuid());
                        s_panes.Add(pane, paneInfo);
                    }

                    // Get the pane
                    IVsOutputWindowPane outputPane;
                    if (outputWindow.GetPane(ref paneInfo._paneId, out outputPane) != VSConstants.S_OK)
                    {
                        // Failed to get the pane - might need to create it first
                        outputWindow.CreatePane(ref paneInfo._paneId, pane, fInitVisible: 1, fClearWithSolution: 1);
                        outputWindow.GetPane(ref paneInfo._paneId, out outputPane);
                    }

                    // The first time we output text to a pane, ensure it's visible
                    if (!paneInfo.Shown)
                    {
                        paneInfo.Shown = true;

                        // Switch to the pane of the Output window
                        outputPane.Activate();

                        // Show the output window
                        IVsUIShell shell = s_shellLazy.Value;
                        if (shell != null)
                        {
                            object inputVariant = null;
                            shell.PostExecCommand(VSConstants.GUID_VSStandardCommandSet97, (uint)VSConstants.VSStd97CmdID.OutputWindow, 0, ref inputVariant);
                        }
                    }

                    // Output the text
                    outputPane.OutputStringThreadSafe(message);
                }
                catch (Exception)
                {
                    Debug.Fail("Failed to write to output pane.");
                }
            }).FileAndForget("VS/Diagnostics/Debugger/SSHDebugPS/VsOutputWindowWrapper/Write");
        }

        /// <summary>
        /// Writes text directly to the VS Output window, appending a newline.
        /// </summary>
        public static void WriteLine(string message, string pane = s_defaultOutputPane)
        {
            Write(string.Concat(message, Environment.NewLine), pane);
        }
    }
}
