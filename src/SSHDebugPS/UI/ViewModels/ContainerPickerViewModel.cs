using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.SSH;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.SSHDebugPS.UI
{
    public class ContainerPickerViewModel : INotifyPropertyChanged
    {
        public ContainerPickerViewModel()
        {
            InitializeConnections();
            this.SelectedConnection = SupportedConnections.First(item => item is LocalConnectionViewModel) ?? SupportedConnections.First();

            this.DockerContainers = new ObservableCollection<DockerContainerInstance>();
            this.CanAddConnection = new Lazy<bool>(() => IsLibLinuxAvailable());

            //Commands
            this.AddSSHConnectionCommand = new BaseCommand(this.AddSSHConnection, () => { return this.CanAddConnection.Value; });

            // Start Docker Instance generation on the selected connection
            GenerateContainersFromConnection();
        }

        private void SupportedConnections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged("SupportedConnections");
        }

        private void InitializeConnections()
        {
            if (this.SupportedConnections != null)
            {
                this.SupportedConnections.CollectionChanged -= SupportedConnections_CollectionChanged;
                this.SupportedConnections = null;
            }

            List<IConnectionViewModel> connections = new List<IConnectionViewModel>();
            connections.Add(new LocalConnectionViewModel());
            connections.AddRange(ConnectionManager.GetAvailableSSHConnectionInfos().Select(item => new SSHConnectionViewModel(item)));

            this.SupportedConnections = new ObservableCollection<IConnectionViewModel>(connections);
            this.SupportedConnections.CollectionChanged += SupportedConnections_CollectionChanged;
            this.OnPropertyChanged(nameof(SupportedConnections));
        }

        public void GenerateContainersFromConnection()
        {
            StatusText = UIResources.SearchingStatusText; // Change text to reflect finding?
            this.DockerContainers.Clear();
            foreach (DockerContainerInstance instance in DockerContainerInstance.GetMockInstances()) // TODO: Replace Get Mock Instances with way to generate
            {
                this.DockerContainers.Add(instance);
            }

            if (this.DockerContainers.Count() > 0)
            {
                StatusText = String.Format(UIResources.ContainersFoundStatusText, this.DockerContainers.Count());
            }
            else
            {
                StatusText = String.Empty;
            }
        }

        protected void AddSSHConnection()
        {
            if (CanAddConnection.Value)
            {
                SSHConnection connection = ConnectionManager.GetSSHConnection(string.Empty) as SSHConnection;
                if(connection != null)
                {
                    SSHConnectionViewModel sshConnection = new SSHConnectionViewModel(connection);
                    SupportedConnections.Add(sshConnection);
                    SelectedConnection = sshConnection;
                }
            }
            else
            {
                Debug.Fail("AddSSHConnection cannot be called.");
            }
        }

        protected Lazy<bool> CanAddConnection;

        private bool IsLibLinuxAvailable()
        {
            try
            {
                return SSHPortSupplier.IsLibLinuxAvailable();
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        #region Commands

        public ICommand AddSSHConnectionCommand { get; }

        #endregion

        #region EventHandlers and Properties

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string StatusText
        {
            get
            {
                return _statusText;
            }
            set
            {
                _statusText = value;
                OnPropertyChanged("StatusText");
            }
        }

        public ObservableCollection<IConnectionViewModel> SupportedConnections { get; private set; }
        public ObservableCollection<DockerContainerInstance> DockerContainers { get; }

        public DockerContainerInstance SelectedDockerInstance
        {
            get
            {
                return _selectedDockerInstance;
            }
            set
            {
                _selectedDockerInstance = value;
                OnPropertyChanged("SelectedInstance");
            }
        }

        public IConnectionViewModel SelectedConnection
        {
            get
            {
                return _selectedConnection;
            }
            set
            {
                _selectedConnection = value;
                OnPropertyChanged("SelectedConnection");
            }
        }

        private DockerContainerInstance _selectedDockerInstance;
        private IConnectionViewModel _selectedConnection;
        private string _statusText;
        #endregion
    }
}
