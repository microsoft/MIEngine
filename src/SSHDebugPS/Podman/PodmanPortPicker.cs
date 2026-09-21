// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.SSHDebugPS.Podman
{
    [ComVisible(true)]
    [Guid("E2A3B4C5-6D7E-4F8A-9B0C-1D2E3F4A5B6C")]
    public class PodmanLinuxPortPicker : DockerPortPickerBase
    {
        internal override bool SupportSSHConnections => true;
        internal override ContainerRuntimeType RuntimeType => ContainerRuntimeType.Podman;
    }
}
