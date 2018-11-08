// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;

namespace Microsoft.SSHDebugPS.SSH
{
    internal class SSHPort : AD7Port, IDebugGdbServerAttach
    {
        public SSHPort(AD7PortSupplier portSupplier, string name, bool isInAddPort)
            : base(portSupplier, name, isInAddPort) { }

        protected override Connection GetConnection()
        {
            if (_connection == null)
            {
                _connection = ConnectionManager.GetSSHConnection(_name);

                if (_connection != null)
                {
                    // User might change connection details via credentials dialog in ConnectionManager.GetInstance, get updated name
                    _name = _connection.Name;
                }
            }

            return _connection;
        }

        public string GdbServerAttachProcess(int id, string preAttachCommand)
        {
            return ((SSHConnection)GetConnection()).AttachToProcess(id, preAttachCommand);
        }
    }
}
