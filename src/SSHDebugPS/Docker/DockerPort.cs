// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.using System;

using Microsoft.VisualStudio.Shell;

namespace Microsoft.SSHDebugPS.Docker
{
    internal class DockerPort : AD7Port
    {
        public DockerPort(AD7PortSupplier portSupplier, string name, bool isInAddPort)
             : base(portSupplier, name, isInAddPort)
        { }

        protected override Connection GetConnectionInternal()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return ConnectionManager.GetDockerConnection(Name, supportSSHConnections: true);
        }
    }
}
