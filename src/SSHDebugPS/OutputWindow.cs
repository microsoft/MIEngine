// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.SSHDebugPS
{
    class OutputWindow
    {
        private static OutputWindow _instance;

        public static OutputWindow Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new OutputWindow();
                }

                return _instance;
            }
        }

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

        private IVsOutputWindow outputWindow;
        private IVsUIShell shell;

        private Dictionary<string, PaneInfo> panes = new Dictionary<string, PaneInfo>()
        {
            // The 'Debug' pane exists by default
            { "Debug", new PaneInfo(VSConstants.GUID_OutWindowDebugPane) }
        };

        private OutputWindow()
        {
            outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            shell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
            if (outputWindow == null || shell == null)
            {
                throw new NullReferenceException("Could not create OutputWindow");
            }
        }

        /// <summary>
        /// Writes text directly to the VS Output window.
        /// </summary>
        public void Write(string message, string pane = "Debug")
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
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
                        if (shell != null)
                        {
                            Guid commandSet = VSConstants.GUID_VSStandardCommandSet97;
                            object inputVariant = null;
                            shell.PostExecCommand(commandSet, (uint)VSConstants.VSStd97CmdID.OutputWindow, 0, ref inputVariant);
                        }
                    }

                    // Output the text
                    outputPane.OutputString(message);
                }
                catch (Exception)
                {
                }
            });
        }

        /// <summary>
        /// Writes text directly to the VS Output window, appending a newline.
        /// </summary>
        public void WriteLine(string message, string pane = "Debug")
        {
            Write(string.Concat(message, Environment.NewLine), pane);
        }
    }
}
