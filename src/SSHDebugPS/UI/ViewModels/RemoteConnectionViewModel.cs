using liblinux;
using Microsoft.SSHDebugPS.SSH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SSHDebugPS
{
    public interface IConnectionViewModel
    {
        string DisplayName { get; }
    }

    public class LocalConnectionViewModel : IConnectionViewModel
    {
        public LocalConnectionViewModel() { }

        public string DisplayName => UIResources.LocalMachine;
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

        public string DisplayName
        {
            get
            {
                return sshConnection?.Name ?? SSHPortSupplier.GetFormattedSSHConnectionName(connectionInfo);
            }
        }
    }

    // TODO: Remove--simply for testing purposes
    internal class MockConnectionViewModel : IConnectionViewModel
    {
        private string name { get; }

        public MockConnectionViewModel(string name)
        {
            this.name = name;
        }

        public string DisplayName => name;

    }

}
