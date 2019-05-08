// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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

            this.AllowRefresh = true;
        }

        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Ultimately what we give back
        public string SelectedQualifier
        {
            get 
            {
                return _selectedQualifier;
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
                OnPropertyChanged("Model");
            }
        }

        public bool AllowRefresh
        {
            get
            {
                return _allowRefresh;
            }
            set
            {
                _allowRefresh = value;
                OnPropertyChanged("AllowRefresh");
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
            if (Model.SelectedDockerInstance != null)
            {
                this.DialogResult = Model.SelectedDockerInstance.GetResult(out _selectedQualifier);
                this.Close();
            }

            e.Handled = true;
        }

        private void DialogWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (Model.SelectedDockerInstance != null)
                {
                    this.DialogResult = Model.SelectedDockerInstance.GetResult(out _selectedQualifier);
                    this.Close();

                    e.Handled = true;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            AllowRefresh = false;

            Model.GenerateContainersFromConnection();

            e.Handled = true;
            AllowRefresh = true;
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
        private bool _allowRefresh;
        private string _selectedQualifier;
        #endregion
    }
}
