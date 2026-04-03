// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SSHDebugPS.Docker;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.SSHDebugPS.Podman
{
    internal class PodmanPort : AD7Port
    {
        public PodmanPort(AD7PortSupplier portSupplier, string name, bool isInAddPort)
             : base(portSupplier, name, isInAddPort)
        { }

        protected override Connection GetConnectionInternal()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return ConnectionManager.GetPodmanConnection(Name, supportSSHConnections: true);
        }
    }
}
