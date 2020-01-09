// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.UI;
using Microsoft.SSHDebugPS.Utilities;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.SSHDebugPS.UI
{
    /// <summary>
    /// Interaction logic for DockerContainerPickerWindow.xaml
    /// </summary>
    public partial class ContainerPickerDialogWindow : DialogWindow
    {
        public ContainerPickerDialogWindow(bool supportSSHConnections)
        {
            InitializeComponent();
            this.Model = new ContainerPickerViewModel(supportSSHConnections);
            this.DataContext = Model;
            this.Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            bool listItemFocused = false;
            try
            {
                if (ContainerListBox != null && ContainerListBox.HasItems)
                {
                    if (ContainerListBox.SelectedItem == null)
                    {
                        ContainerListBox.SelectedIndex = 0;
                    }
                    ((ListBoxItem)ContainerListBox.ItemContainerGenerator.ContainerFromItem(ContainerListBox.SelectedItem)).Focus();
                    listItemFocused = true;
                }
            }
            catch (Exception ex)
            {
                // Ignore if focus was failed to be set
                Debug.Fail(ex.ToString());
            }
            if (!listItemFocused)
            {
                //Focus the combo box
                ConnectionTypeComboBox.Focus();
            }
            e.Handled = true;
        }

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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

        private void ContainerListBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ListBoxItem item = e.OriginalSource as ListBoxItem;

            if (item != null && !item.IsSelected)
            {
                item.IsSelected = true;
            }
        }

        private void ContainerListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is ListBox list)
            {
                if (list.SelectedItem is DockerContainerViewModel viewModel)
                {
                    switch (e.Key)
                    {
                        case Key.Right:
                            viewModel.IsExpanded = true;
                            e.Handled = true;
                            break;
                        case Key.Left:
                            viewModel.IsExpanded = false;
                            e.Handled = true;
                            break;
                        case Key.Space:
                            viewModel.IsExpanded = !viewModel.IsExpanded;
                            e.Handled = true;
                            break;
                    }
                }
            }
        }
        #endregion

        #region Private Variables
        private ContainerPickerViewModel _model;
        #endregion
    }
}
