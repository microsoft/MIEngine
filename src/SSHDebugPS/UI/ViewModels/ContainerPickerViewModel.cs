using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.SSH;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.SSHDebugPS
{
    public class ContainerPickerViewModel : INotifyPropertyChanged
    {
        public ContainerPickerViewModel()
        {
            // Add the default local machine
            this.connections.Add(new LocalConnectionViewModel());
        }

        #region Properties
        private List<IConnectionViewModel> connections = new List<IConnectionViewModel>();
        internal ObservableCollection<IConnectionViewModel> Connections { get; }

        private List<DockerContainerInstance> dockerContainers = new List<DockerContainerInstance>();
        internal ObservableCollection<DockerContainerInstance> DockerContainers { get; }

        private bool isLocalConnection = true;
        public bool IsLocalConnection
        {
            get => isLocalConnection;
            set
            {
                if (value != isLocalConnection)
                {
                    isLocalConnection = value;
                    OnPropertyChanged(nameof(IsLocalConnection));
                }
            }
        }

        protected bool RemoteConnectionIsVisibile
        {
            get { return !this.IsLocalConnection; }
        }

        public bool IsContainersVisible { get { return true; } }

        public ICommand RefreshCommand { get; set; }
        public ICommand SSHConnect { get; set; }

        #endregion

        #region Event and Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // validate
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // just close and cancel
        }


        private void _directConnectedListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        #endregion
    }
}
