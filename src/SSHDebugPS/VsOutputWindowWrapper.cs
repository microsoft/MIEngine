// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.SSHDebugPS
{
    internal static class VsOutputWindowWrapper
    {
        private static Lazy<IVsOutputWindow> outputWindowLazy = new Lazy<IVsOutputWindow>(() =>
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

        private static Lazy<IVsUIShell> shellLazy = new Lazy<IVsUIShell>(() =>
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
                this.paneId = paneId;
                this.Shown = false;
            }

            internal Guid paneId;
            internal bool Shown { get; set; }
        }

        private const string DefaultOutputPane = "Debug";

        private static Dictionary<string, PaneInfo> panes = new Dictionary<string, PaneInfo>()
        {
            // The 'Debug' pane exists by default
            { DefaultOutputPane, new PaneInfo(VSConstants.GUID_OutWindowDebugPane) }
        };

        /// <summary>
        /// Writes text directly to the VS Output window.
        /// </summary>
        public static void Write(string message, string pane = DefaultOutputPane)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    // Get the Output window
                    IVsOutputWindow outputWindow = outputWindowLazy.Value;
                    if (outputWindow == null)
                    {
                        return;
                    }

                    // Get the pane guid
                    PaneInfo paneInfo;
                    if (!panes.TryGetValue(pane, out paneInfo))
                    {
                        // Pane didn't exist, create it
                        paneInfo = new PaneInfo(Guid.NewGuid());
                        panes.Add(pane, paneInfo);
                    }

                    // Get the pane
                    IVsOutputWindowPane outputPane;
                    if (outputWindow.GetPane(ref paneInfo.paneId, out outputPane) != VSConstants.S_OK)
                    {
                        // Failed to get the pane - might need to create it first
                        outputWindow.CreatePane(ref paneInfo.paneId, pane, fInitVisible: 1, fClearWithSolution: 1);
                        outputWindow.GetPane(ref paneInfo.paneId, out outputPane);
                    }

                    // The first time we output text to a pane, ensure it's visible
                    if (!paneInfo.Shown)
                    {
                        paneInfo.Shown = true;

                        // Switch to the pane of the Output window
                        outputPane.Activate();

                        // Show the output window
                        IVsUIShell shell = shellLazy.Value;
                        if (shell != null)
                        {
                            object inputVariant = null;
                            shell.PostExecCommand(VSConstants.GUID_VSStandardCommandSet97, (uint)VSConstants.VSStd97CmdID.OutputWindow, 0, ref inputVariant);
                        }
                    }

                    // Output the text
                    outputPane.OutputString(message);
                }
                catch (Exception)
                {
                    Debug.Fail("Failed to write to output pane.");
                }
            }).FileAndForget("vs/SSHDebugPS/VsOutputWindowWrapper/Write");
        }

        /// <summary>
        /// Writes text directly to the VS Output window, appending a newline.
        /// </summary>
        public static void WriteLine(string message, string pane = DefaultOutputPane)
        {
            Write(string.Concat(message, Environment.NewLine), pane);
        }
    }
}
