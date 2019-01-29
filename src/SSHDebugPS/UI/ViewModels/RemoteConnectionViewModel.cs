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

    internal class LocalConnectionViewModel : IConnectionViewModel
    {
        public LocalConnectionViewModel() { }

        string IConnectionViewModel.DisplayName => UIResources.LocalMachine;
    }

    internal class RemoteConnectionViewModel : IConnectionViewModel
    {
        private IConnection connection { get; }

        public RemoteConnectionViewModel(IConnection connection)
        {
            this.connection = connection;
        }

        string IConnectionViewModel.DisplayName => connection.Name;

    }
}
