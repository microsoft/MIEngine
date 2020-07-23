// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using liblinux.Persistence;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.SSH;
using Microsoft.SSHDebugPS.Utilities;
using System.Globalization;

namespace Microsoft.SSHDebugPS.UI
{
    public class ContainerPickerViewModel : INotifyPropertyChanged
    {
        private Lazy<bool> _sshAvailable;

        public ContainerPickerViewModel(bool supportSSHConnections)
        {
            SupportSSHConnections = supportSSHConnections;
            InitializeConnections();
            ContainerInstances = new ObservableCollection<IContainerViewModel>();

            // SSH is only available if we have Linux containers.
            _sshAvailable = new Lazy<bool>(() => SupportSSHConnections && IsLibLinuxAvailable());
            AddSSHConnectionCommand = new ContainerUICommand(
                AddSSHConnection,
                (parameter /*unused parameter*/) =>
                    {
                        return _sshAvailable.Value;
                    },
                UIResources.AddNewSSHConnectionLabel,
                UIResources.AddNewSSHConnectionToolTip);

            OKCommand = new ContainerUICommand(
                (dialogObject) =>
                {
                    ContainerPickerDialogWindow dialog = dialogObject as ContainerPickerDialogWindow;
                    dialog.DialogResult = ComputeContainerConnectionString();
                    dialog.Close();
                },
                (parameter /*unused*/) =>
                {
                    return SelectedContainerInstance != null;
                },
                UIResources.OKLabel,
                UIResources.OKLabel
                );

            CancelCommand = new ContainerUICommand(
                (dialogObject) =>
                {
                    ContainerPickerDialogWindow dialog = dialogObject as ContainerPickerDialogWindow;
                    dialog.Close();
                },
                UIResources.CancelLabel,
                UIResources.CancelLabel
                );

            RefreshCommand = new ContainerUICommand(
                (parameter /*unused*/) =>
                {
                    RefreshContainersList();
                },
                (parameter /*unused*/) =>
                {
                    return IsRefreshEnabled;
                },
                UIResources.RefreshHyperlink,
                UIResources.RefreshToolTip);

            PropertyChanged += ContainerPickerViewModel_PropertyChanged;
            SelectedConnection = SupportedConnections.First(item => item is LocalConnectionViewModel) ?? SupportedConnections.First();
        }

        private void InitializeConnections()
        {
            List<IConnectionViewModel> connections = new List<IConnectionViewModel>();
            connections.Add(new LocalConnectionViewModel());
            if (SupportSSHConnections) // we currently only support SSH for Linux Containers
            {
                connections.AddRange(SSHHelper.GetAvailableSSHConnectionInfos().Select(item => new SSHConnectionViewModel(item)));
            }
            SupportedConnections = new ObservableCollection<IConnectionViewModel>(connections);
            OnPropertyChanged(nameof(SupportedConnections));
        }

        internal void RefreshContainersList()
        {
            IsRefreshEnabled = false;

            // Clear everything before retreiving the container list
            ContainerInstances?.Clear();
            UpdateStatusMessage(string.Empty, false);

            // Set the status
            ContainersFoundText = UIResources.QueryingForContainersMessage;

            // Tell the dispatcher to run the Refresh task with a lower priority than Render.
            // This is so that the UI does any necessary updating before the refresh task has completed. 
            // Render = 7
            // Loaded = 6  - Operations are processed when layout and render has finished but just before items at input priority are serviced. 
            // https://docs.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcherpriority
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Input, (Action)(() => { RefreshContainersListInternal(); }));
        }

        private bool ComputeContainerConnectionString()
        {
            if (SelectedContainerInstance != null)
            {
                string remoteConnectionString = string.Empty;
                string containerId;
                if (SelectedConnection.Connection == null)
                {

                    SelectedContainerInstance.GetResult(out containerId);
                }
                else
                {
                    SelectedContainerInstance.GetResult(out containerId);
                    remoteConnectionString = SelectedConnection.Connection.Name;
                }

                SelectedContainerConnectionString = DockerConnection.CreateConnectionString(containerId, remoteConnectionString, Hostname);
                return true;
            }

            return false;
        }

        // The formatted string for the ConnectionType dialog
        public string SelectedContainerConnectionString { get; private set; }

        private const string unknown = "Unknown";

        private void RefreshContainersListInternal()
        {
            int totalContainers = 0;
            try
            {
                IContainerViewModel selectedContainer = SelectedContainerInstance;
                SelectedContainerInstance = null;

                IEnumerable<DockerContainerInstance> containers;

                if (SelectedConnection is LocalConnectionViewModel)
                {
                    // containers = DockerHelper.GetLocalDockerContainers(Hostname, out totalContainers);
                    DockerHelper.TryGetLocalDockerContainers(Hostname, out containers, out totalContainers);
                }
                else
                {
                    ContainersFoundText = UIResources.SSHConnectingStatusText;
                    var connection = SelectedConnection.Connection;
                    if (connection == null)
                    {
                        UpdateStatusMessage(UIResources.SSHConnectionFailedStatusText, isError: true);
                        return;
                    }
                    containers = DockerHelper.GetRemoteDockerContainers(connection, Hostname, out totalContainers);
                }

                string serverOS;
                bool getServerOS = DockerHelper.TryGetServerOS(Hostname, out serverOS);

                if (getServerOS)
                {
                    bool lcow;
                    // bool getLCOW = DockerHelper.LCOW(Hostname, out lcow);
                    bool getLCOW = DockerHelper.TryGetLCOW(Hostname, out lcow);
                    TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                    serverOS = textInfo.ToTitleCase(serverOS);

                    if (lcow && serverOS.Contains("Windows"))
                    {
                        foreach (DockerContainerInstance container in containers)
                        {
                            string containerPlatform = string.Empty;
                            DockerHelper.TryGetContainerPlatform(Hostname, container.Name, out containerPlatform);
                            container.Platform = textInfo.ToTitleCase(containerPlatform);
                        }
                    }
                    else
                    {
                        foreach (DockerContainerInstance container in containers)
                        {
                            container.Platform = serverOS;
                        }
                    }
                }
                else
                {
                    foreach (DockerContainerInstance container in containers)
                    {
                        container.Platform = unknown;
                    }
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
                UpdateStatusMessage(UIResources.ErrorStatusTextFormat.FormatCurrentCultureWithArgs(ex.Message), isError: true);
                return;
            }
            finally
            {
                if (ContainerInstances.Count() > 0)
                {
                    if (ContainerInstances.Count() < totalContainers)
                    {
                        UpdateStatusMessage(UIResources.ContainersNotAllParsedStatusText.FormatCurrentCultureWithArgs(totalContainers - ContainerInstances.Count()), isError: false);
                        ContainersFoundText = UIResources.ContainersNotAllParsedText.FormatCurrentCultureWithArgs(ContainerInstances.Count(), totalContainers);
                    }
                    else
                    {
                        ContainersFoundText = UIResources.ContainersFoundStatusText.FormatCurrentCultureWithArgs(ContainerInstances.Count());
                    }
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

        // Default is to Support SSH Connections
        private bool _supportSSHConnections = true;
        public bool SupportSSHConnections
        {
            get
            {
                return _supportSSHConnections;
            }
            private set
            {
                _supportSSHConnections = value;
                OnPropertyChanged(nameof(SupportSSHConnections));
            }
        }

        #region Commands

        public IContainerUICommand AddSSHConnectionCommand { get; }
        public IContainerUICommand OKCommand { get; }
        public IContainerUICommand CancelCommand { get; }
        public IContainerUICommand RefreshCommand { get; }

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
                    // If the user is updating the hostname, clear the container list and the selected container list.
                    SelectedContainerInstance = null;
                    ContainerInstances?.Clear();
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

        public void UpdateStatusMessage(string statusMessage, bool isError)
        {
            // If the message is updated to empty, we want to clear the StatusIsError value.
            if (string.IsNullOrEmpty(statusMessage))
            {
                if (!string.IsNullOrEmpty(_statusMessage))
                {
                    _statusMessage = statusMessage;
                    OnPropertyChanged(nameof(StatusMessage));
                }

                if (_statusIsError != false)
                {
                    _statusIsError = false;
                    OnPropertyChanged(nameof(StatusIsError));
                }
                return;
            }

            if (!string.Equals(_statusMessage, statusMessage, StringComparison.CurrentCulture))
            {
                _statusMessage = statusMessage;
                OnPropertyChanged(nameof(StatusMessage));

                if (_statusIsError != isError)
                {
                    _statusIsError = isError;
                    OnPropertyChanged(nameof(StatusIsError));
                }
                return;
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
        }

        private bool _statusIsError;
        public bool StatusIsError
        {
            get => _statusIsError;
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

                RefreshCommand.NotifyCanExecuteChanged();
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
                // Update the status of the OK button
                OKCommand.NotifyCanExecuteChanged();
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
