// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.SSHDebugPS.UI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.SSHDebugPS.Docker
{
    [ComVisible(true)]
    //{91BDF293-E6A0-49C4-B033-6F36CFC4FF98}
    [Guid("91BDF293-E6A0-49C4-B033-6F36CFC4FF98")]
    public class DockerLinuxPortPicker : DockerPortPickerBase
    {
        internal override bool SupportSSHConnections => true;
    }
    
    [ComVisible(true)]
    //{AE75778B-70E8-4F53-B3FE-59E048D3D01B}
    [Guid("AE75778B-70E8-4F53-B3FE-59E048D3D01B")]
    public class DockerWindowsPortPicker : DockerPortPickerBase
    {
        internal override bool SupportSSHConnections => false;
    }

    [ComVisible(true)]
    public abstract class DockerPortPickerBase : IDebugPortPicker
    {
        internal abstract bool SupportSSHConnections { get; }

        int IDebugPortPicker.DisplayPortPicker(IntPtr hwndParentDialog, out string pbstrPortId)
        {
            // If this is null, then the PortPicker handler shows an error. Set to empty by default
            return ConnectionManager.ShowContainerPickerWindow(hwndParentDialog, SupportSSHConnections, out pbstrPortId) ?
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
