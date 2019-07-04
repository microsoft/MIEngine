// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.SSHDebugPS.Utilities;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.SSHDebugPS.UI
{
    /// <summary>
    /// Interaction logic for DockerContainerPickerWindow.xaml
    /// </summary>
    public partial class ContainerPickerDialogWindow : DialogWindow
    {
        public ContainerPickerDialogWindow()
        {
            InitializeComponent();
            this.Model = new ContainerPickerViewModel();
            this.DataContext = Model;
        }

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Ultimately what we give back
        public string SelectedContainerConnectionString
        {
            get
            {
                return _selectedContainerConnectionString;
            }
        }

        public ContainerPickerViewModel Model
        {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
                OnPropertyChanged(nameof(Model));
            }
        }
        #endregion

        #region Event Handlers

        private void ListBox_GotKeyboardFocus(object sender, RoutedEventArgs e)
        {
            ListBoxItem item = e.OriginalSource as ListBoxItem;

            if (item != null && !item.IsSelected)
            {
                item.IsSelected = true;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = ComputeContainerConnectionString();
            this.Close();

            e.Handled = true;
        }

        private void DialogWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                this.DialogResult = ComputeContainerConnectionString();
                if (this.DialogResult.GetValueOrDefault(false))
                {
                    e.Handled = true;
                }
            }
        }

        private void HostnameTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Model.RefreshContainersList();
                e.Handled = true;
            }
        }

        private bool ComputeContainerConnectionString()
        {
            if (Model.SelectedContainerInstance != null)
            {
                string remoteConnectionString = string.Empty;
                string containerId;
                if (Model.SelectedConnection.Connection == null)
                {

                    Model.SelectedContainerInstance.GetResult(out containerId);
                }
                else
                {
                    Model.SelectedContainerInstance.GetResult(out containerId);
                    remoteConnectionString = Model.SelectedConnection.Connection.Name;
                }

                string dockerString = string.IsNullOrWhiteSpace(Model.Hostname) ? containerId : "{0}::{1}".FormatInvariantWithArgs(Model.Hostname, containerId);

                _selectedContainerConnectionString = string.IsNullOrWhiteSpace(remoteConnectionString) ? dockerString : "{0}/{1}".FormatInvariantWithArgs(remoteConnectionString, dockerString);
                return true;
            }

            return false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            Model.RefreshContainersList();

            e.Handled = true;
        }

        private void ContainerListBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ListBoxItem item = e.OriginalSource as ListBoxItem;

            if (item != null && !item.IsSelected)
            {
                item.IsSelected = true;
            }
        }
        #endregion

        #region Private Variables
        private ContainerPickerViewModel _model;
        private string _selectedContainerConnectionString;
        #endregion
    }
}
