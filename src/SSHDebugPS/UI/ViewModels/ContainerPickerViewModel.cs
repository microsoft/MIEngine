// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.SSH;

namespace Microsoft.SSHDebugPS.UI
{
    public class ContainerPickerViewModel : INotifyPropertyChanged
    {
        public ContainerPickerViewModel()
        {
            InitializeConnections();
            ContainerInstances = new ObservableCollection<IContainerInstance>();
            _sshAvailable = new Lazy<bool>(() => IsLibLinuxAvailable());
            AddSSHConnectionCommand = new BaseCommand(AddSSHConnection, () => { return CanAddConnection; });
            PropertyChanged += ContainerPickerViewModel_PropertyChanged;

            SelectedConnection = SupportedConnections.First(item => item is LocalConnectionViewModel) ?? SupportedConnections.First();
        }

        private void InitializeConnections()
        {
            List<IConnectionViewModel> connections = new List<IConnectionViewModel>();
            connections.Add(new LocalConnectionViewModel());
            connections.AddRange(ConnectionManager.GetAvailableSSHConnectionInfos().Select(item => new SSHConnectionViewModel(item)));

            SupportedConnections = new ObservableCollection<IConnectionViewModel>(connections);
            OnPropertyChanged(nameof(SupportedConnections));
        }

        internal void RefreshContainersList()
        {
            IsRefreshEnabled = false;

            try
            {
                StatusText = UIResources.SearchingStatusText; // Change text to reflect finding?
                ContainerInstances?.Clear();

                IEnumerable<IContainerInstance> containers;

                if (SelectedConnection is LocalConnectionViewModel)
                {
                    containers = ConnectionManager.GetLocalDockerContainers();
                }
                else
                {
                    StatusText = UIResources.SSHConnectingStatusText;
                    var connection = SelectedConnection.Connection;
                    if (connection == null)
                    {
                        StatusText = UIResources.SSHConnectionFailedStatusText;
                        return;
                    }
                    containers = ConnectionManager.GetRemoteDockerContainers(connection);
                }

                ContainerInstances = new ObservableCollection<IContainerInstance>(containers);
                OnPropertyChanged(nameof(ContainerInstances));

                if (ContainerInstances.Count() > 0)
                {
                    StatusText = String.Format(UIResources.ContainersFoundStatusText, ContainerInstances.Count());
                }
                else
                {
                    StatusText = String.Empty;
                }
            }
            catch (Exception ex)
            {
                StatusText = String.Format(CultureInfo.CurrentCulture, UIResources.ErrorStatusTextFormat, ex.Message);
                StatusIsError = true;
                return;
            }
            finally
            {
                IsRefreshEnabled = true;
            }
        }

        public void AddSSHConnection()
        {
            if (CanAddConnection)
            {
                SSHConnection connection = ConnectionManager.GetSSHConnection(string.Empty) as SSHConnection;
                if (connection != null)
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

        private Lazy<bool> _sshAvailable;
        public bool CanAddConnection
        {
            get
            {
                return _sshAvailable?.Value ?? false;
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

        public ICommand AddSSHConnectionCommand { get; }

        #endregion

        #region Event, EventHandlers and Properties

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ContainerPickerViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(SelectedConnection)))
            {
                RefreshContainersList();
            }
        }

        private string _statusText;
        public string StatusText
        {
            get
            {
                return _statusText;
            }
            set
            {
                _statusText = value;
                StatusIsError = false; // reset the StatusText to not be an error.
                OnPropertyChanged(nameof(StatusText));
            }
        }

        private bool _statusIsError;
        public bool StatusIsError
        {
            get
            {
                return _statusIsError;
            }
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
        public ObservableCollection<IContainerInstance> ContainerInstances { get; private set; }

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

        private IContainerInstance _selectedContainerInstance;
        public IContainerInstance SelectedContainerInstance
        {
            get
            {
                return _selectedContainerInstance;
            }
            set
            {
                if (!(_selectedContainerInstance != null && _selectedContainerInstance.Name.Equals(value.Name, StringComparison.Ordinal)))
                {
                    _selectedContainerInstance = value;
                    OnPropertyChanged(nameof(SelectedContainerInstance));
                }
            }
        }

        private IConnectionViewModel _selectedConnection;
        public IConnectionViewModel SelectedConnection
        {
            get
            {
                return _selectedConnection;
            }
            set
            {
                if (!(_selectedConnection != null && _selectedConnection.DisplayName.Equals(value.DisplayName, StringComparison.Ordinal)))
                {
                    _selectedConnection = value;
                    OnPropertyChanged(nameof(SelectedConnection));
                }
            }
        }
        #endregion
    }
}
