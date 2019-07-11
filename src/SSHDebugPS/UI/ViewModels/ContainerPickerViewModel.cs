// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.SSH;
using Microsoft.SSHDebugPS.Utilities;

namespace Microsoft.SSHDebugPS.UI
{
    public class ContainerPickerViewModel : INotifyPropertyChanged
    {
        private Lazy<bool> _sshAvailable;

        public ContainerPickerViewModel()
        {
            InitializeConnections();
            ContainerInstances = new ObservableCollection<IContainerViewModel>();

            _sshAvailable = new Lazy<bool>(() => IsLibLinuxAvailable());
            AddSSHConnectionCommand = new ContainerUICommand(
                AddSSHConnection,
                (parameter /*unused parameter*/) =>
                    {
                        return _sshAvailable.Value;
                    },
                UIResources.AddNewSSHConnectionLabel,
                UIResources.AddNewSSHConnectionToolTip);

            PropertyChanged += ContainerPickerViewModel_PropertyChanged;
            SelectedConnection = SupportedConnections.First(item => item is LocalConnectionViewModel) ?? SupportedConnections.First();
        }

        private void InitializeConnections()
        {
            List<IConnectionViewModel> connections = new List<IConnectionViewModel>();
            connections.Add(new LocalConnectionViewModel());
            connections.AddRange(SSHHelper.GetAvailableSSHConnectionInfos().Select(item => new SSHConnectionViewModel(item)));

            SupportedConnections = new ObservableCollection<IConnectionViewModel>(connections);
            OnPropertyChanged(nameof(SupportedConnections));
        }

        internal void RefreshContainersList()
        {
            IsRefreshEnabled = false;

            try
            {
                IContainerViewModel selectedContainer = SelectedContainerInstance;
                SelectedContainerInstance = null;

                ContainersFoundText = UIResources.SearchingStatusText;

                // Clear everything
                ContainerInstances?.Clear();
                StatusText = string.Empty;
                StatusIsError = false;

                IEnumerable<DockerContainerInstance> containers;

                if (SelectedConnection is LocalConnectionViewModel)
                {
                    containers = DockerHelper.GetLocalDockerContainers(Hostname);
                }
                else
                {
                    ContainersFoundText = UIResources.SSHConnectingStatusText;
                    var connection = SelectedConnection.Connection;
                    if (connection == null)
                    {
                        StatusText = UIResources.SSHConnectionFailedStatusText;
                        StatusIsError = true;
                        return;
                    }
                    containers = DockerHelper.GetRemoteDockerContainers(connection, Hostname);
                }

                ContainerInstances = new ObservableCollection<IContainerViewModel>(containers.Select(item => new DockerContainerViewModel(item)).ToList());
                OnPropertyChanged(nameof(ContainerInstances));

                if (ContainerInstances.Count() > 0)
                {
                    
                    if (selectedContainer != null)
                    {
                        var found = ContainerInstances.FirstOrDefault(c => selectedContainer.Equals(c));
                        if (found != null)
                        {
                            SelectedContainerInstance = found;
                            return;
                        }
                    }
                    SelectedContainerInstance = ContainerInstances[0];
                }
            }
            catch (Exception ex)
            {
                StatusText = UIResources.ErrorStatusTextFormat.FormatCurrentCultureWithArgs(ex.Message);
                StatusIsError = true;
                return;
            }
            finally
            {
                if (ContainerInstances.Count() > 0)
                {
                    ContainersFoundText = UIResources.ContainersFoundStatusText.FormatCurrentCultureWithArgs(ContainerInstances.Count());
                }
                else
                {
                    ContainersFoundText = UIResources.NoContainersFound;
                }
                IsRefreshEnabled = true;
            }
        }

        private static void AddSSHConnection(object parameter)
        {
            if (parameter is ContainerPickerViewModel vm && vm.AddSSHConnectionCommand.CanExecute(parameter))
            {
                SSHConnection connection = ConnectionManager.GetSSHConnection(string.Empty) as SSHConnection;
                if (connection != null)
                {
                    SSHConnectionViewModel sshConnection = new SSHConnectionViewModel(connection);
                    vm.SupportedConnections.Add(sshConnection);
                    vm.SelectedConnection = sshConnection;
                }
            }
            else
            {
                Debug.Fail("Unable to call AddSSHConnection");
            }
        }

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

        public IContainerUICommand AddSSHConnectionCommand { get; }

        #endregion

        #region Event, EventHandlers and Properties

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ContainerPickerViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, nameof(SelectedConnection), StringComparison.Ordinal))
            {
                RefreshContainersList();
            }
        }

        private string _hostname;
        public string Hostname
        {
            get => _hostname;
            set
            {
                if (!string.Equals(_hostname, value, StringComparison.Ordinal))
                {
                    _hostname = value;
                    OnPropertyChanged(nameof(Hostname));
                }
            }
        }

        private string _containersFoundText;
        public string ContainersFoundText
        {
            get => _containersFoundText;
            set
            {
                _containersFoundText = value;
                OnPropertyChanged(nameof(ContainersFoundText));
            }
        }

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (!string.Equals(_statusText, value, StringComparison.CurrentCulture))
                {
                    StatusIsError = false; // reset the StatusText to not be an error.
                    _statusText = value;
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        private bool _statusIsError;
        public bool StatusIsError
        {
            get => _statusIsError && StatusText.Length > 0;
            set
            {
                if (_statusIsError != value)
                {
                    _statusIsError = value;
                    OnPropertyChanged(nameof(StatusIsError));
                }
            }
        }

        public ObservableCollection<IConnectionViewModel> SupportedConnections { get; private set; }
        public ObservableCollection<IContainerViewModel> ContainerInstances { get; private set; }

        private bool _isRefreshEnabled = true;
        public bool IsRefreshEnabled
        {
            get => _isRefreshEnabled;
            set
            {
                if (_isRefreshEnabled != value)
                {
                    _isRefreshEnabled = value;
                    OnPropertyChanged(nameof(IsRefreshEnabled));
                }
            }
        }

        private IContainerViewModel _selectedContainerInstance;
        public IContainerViewModel SelectedContainerInstance
        {
            get => _selectedContainerInstance;
            set
            {
                // checking cases that they are not equal
                if (!object.Equals(_selectedContainerInstance, value))
                {
                    _selectedContainerInstance = value;
                    OnPropertyChanged(nameof(SelectedContainerInstance));
                }
            }
        }

        private IConnectionViewModel _selectedConnection;
        public IConnectionViewModel SelectedConnection
        {
            get => _selectedConnection;
            set
            {
                if ((_selectedConnection != null && _selectedConnection != value)
                    || (_selectedConnection == null && value != null))
                {
                    _selectedConnection = value;
                    OnPropertyChanged(nameof(SelectedConnection));
                }
            }
        }
        #endregion
    }
}
