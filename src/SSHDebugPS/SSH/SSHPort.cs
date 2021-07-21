// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.SSHDebugPS.SSH
{
    internal class SSHPort : AD7Port, IDebugGdbServerAttach
    {
        public SSHPort(AD7PortSupplier portSupplier, string name, bool isInAddPort)
            : base(portSupplier, name, isInAddPort) { }

        protected override Connection GetConnectionInternal()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return ConnectionManager.GetSSHConnection(Name);
        }

        public string GdbServerAttachProcess(int id, string preAttachCommand)
        {
            return ((SSHConnection)GetConnection()).AttachToProcess(id, preAttachCommand);
        }
    }
}
