// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using liblinux;
using Microsoft.SSHDebugPS.SSH;

namespace Microsoft.SSHDebugPS.UI
{
    public interface IConnectionViewModel
    {
        string DisplayName { get; }

        IConnection Connection { get; }
    }

    public class LocalConnectionViewModel : IConnectionViewModel
    {
        public LocalConnectionViewModel() { }

        #region IConnectionViewModel

        public string DisplayName => UIResources.LocalMachine;

        public IConnection Connection { get => null; }

        #endregion
    }

    internal class SSHConnectionViewModel : IConnectionViewModel
    {
        private ConnectionInfo connectionInfo { get; }

        internal SSHConnectionViewModel(SSHConnection connection)
        {
            this.sshConnection = connection;
        }

        internal SSHConnectionViewModel(ConnectionInfo connectionInfo)
        {
            this.connectionInfo = connectionInfo;
        }

        private SSHConnection sshConnection;
        protected IConnection GetConnection()
        {
            if (this.sshConnection == null)
            {
                if (this.connectionInfo != null)
                {
                    this.sshConnection = ConnectionManager.CreateSSHConnectionFromConnectionInfo(connectionInfo);
                }
            }
            return this.sshConnection;
        }

        #region IConnectionViewModel

        public string DisplayName
        {
            get
            {
                return sshConnection?.Name ?? SSHPortSupplier.GetFormattedSSHConnectionName(connectionInfo);
            }
        }

        public IConnection Connection { get => GetConnection(); }

        #endregion
    }
}
