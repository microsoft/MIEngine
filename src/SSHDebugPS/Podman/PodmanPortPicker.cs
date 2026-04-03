// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.SSHDebugPS.Podman
{
    [ComVisible(true)]
    [Guid("ADB54F86-C0F3-4E7A-8E92-ED98BC558372")]
    public class PodmanLinuxPortPicker : PodmanPortPickerBase
    {
        internal override bool SupportSSHConnections => true;
    }

    [ComVisible(true)]
    public abstract class PodmanPortPickerBase : IDebugPortPicker
    {
        internal abstract bool SupportSSHConnections { get; }

        int IDebugPortPicker.DisplayPortPicker(IntPtr hwndParentDialog, out string pbstrPortId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return ConnectionManager.ShowContainerPickerWindow(hwndParentDialog, SupportSSHConnections, PodmanContainerRuntime.Instance, out pbstrPortId) ?
                VSConstants.S_OK : VSConstants.S_FALSE;
        }

        private VisualStudio.OLE.Interop.IServiceProvider _serviceProvider = null;
        int IDebugPortPicker.SetSite(VisualStudio.OLE.Interop.IServiceProvider pSP)
        {
            _serviceProvider = pSP;
            return VSConstants.S_OK;
        }
    }
}
