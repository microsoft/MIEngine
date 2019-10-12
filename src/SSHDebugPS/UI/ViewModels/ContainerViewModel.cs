// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using liblinux.Shell;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.Utilities;
using Microsoft.VisualStudio.Shell;
using WindowsInput = System.Windows.Input;

namespace Microsoft.SSHDebugPS.UI
{
    public interface IContainerViewModel
        : INotifyPropertyChanged, IEquatable<IContainerViewModel>
    {
        string Name { get; }
        string Id { get; }
        WindowsInput.ICommand ExpandCommand { get; }
        bool GetResult(out string selectedQualifier);
        bool IsExpanded { get; set; }
        bool IsSelected { get; set; }
    }

    public abstract class ContainerViewModel<T> : IContainerViewModel
        where T : IContainerInstance
    {
        public T Instance { get; private set; }

        public ContainerViewModel(T instance)
        {
            Instance = instance;
            RefreshContainerProperties();
        }

        public abstract bool GetResult(out string selectedQualifier);
        public abstract WindowsInput.ICommand ExpandCommand { get; }

        #region INotifyPropertyChanged and helper

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion

        protected IDictionary<string, string> containerProperties = new Dictionary<string, string>();
        protected abstract void RefreshContainerPropertiesInternal();

        private void RefreshContainerProperties()
        {
            RefreshContainerPropertiesInternal();
            OnPropertyChanged(nameof(ContainerProperties));
        }
        
        public ObservableCollection<ContainerProperty> ContainerProperties 
        { 
            get 
            {
                return new ObservableCollection<ContainerProperty>(
                    containerProperties.Keys.Select(
                        item => 
                        new ContainerProperty(this, item, containerProperties[item])).ToList()); 
            }
        } 

        #region IEquatable
        public static bool operator ==(ContainerViewModel<T> left, ContainerViewModel<T> right)
        {
            if (ReferenceEquals(null, left) || ReferenceEquals(null, right))
            {
                return ReferenceEquals(left, right);
            }

            return left.Equals(right);
        }

        public static bool operator !=(ContainerViewModel<T> left, ContainerViewModel<T> right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is IContainerViewModel instance)
            {
                return Equals(instance);
            }
            return false;
        }

        public bool Equals(IContainerViewModel instance)
        {
            if (instance is object)
            {
                return EqualsInternal(instance);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return GetHashCodeInternal();
        }

        #endregion

        #region Helpers

        protected abstract bool EqualsInternal(IContainerViewModel instance);
        protected abstract int GetHashCodeInternal();

        #endregion

        public string Name => Instance.Name;
        public string Id => Instance.Id;

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;

            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }
    }

    public class DockerContainerViewModel
        : ContainerViewModel<DockerContainerInstance>
    {
        public DockerContainerViewModel(DockerContainerInstance instance)
            : base(instance)
        { }

        public string FormattedListOfPorts
        {
            get
            {
                return string.IsNullOrWhiteSpace(Instance.Ports) ?
                    UIResources.NoPortsText :
                    Instance.Ports.Replace(", ", "\r\n");
            }
        }

        public override WindowsInput.ICommand ExpandCommand => new ContainerUICommand(Expand, string.Empty, UIResources.ExpanderToolTip);

        private void Expand(object parameter)
        {
            IsExpanded = !IsExpanded;
            OnPropertyChanged(nameof(DockerViewModelAutomationName));
        }

        protected override void RefreshContainerPropertiesInternal()
        {
            containerProperties.Clear();
            containerProperties.Add(UIResources.ImageLabelText, Image);
            containerProperties.Add(UIResources.CommandLabelText, Command);
            containerProperties.Add(UIResources.StatusLabelText, Status);
            containerProperties.Add(UIResources.CreatedLabelText, Created);
            containerProperties.Add(UIResources.PortsLabelText, FormattedListOfPorts);
        }

        // Gets the first 12 characters and appends an ellipsis
        public string ShortId { get => Id.Length > 12 ? Id.Substring(0, 12) : Id; }
        public string Image => Instance.Image;
        public string Command => Instance.Command;
        public string Status => Instance.Status;
        public string Created => Instance.Created;

        public override bool GetResult(out string selectedQualifier)
        {
            selectedQualifier = Name;
            return true;
        }

        protected override bool EqualsInternal(IContainerViewModel instance)
        {
            if (instance is DockerContainerViewModel dockerVM)
            {
                return this.Instance.Equals(dockerVM.Instance);
            }
            return false;
        }

        protected override int GetHashCodeInternal()
        {
            return Instance.GetHashCode();
        }

        #region AutomationProperty Helpers
        // Override for ScreenReader
        public string DockerViewModelAutomationName
        {
            get
            {
                string text = String.Join(" ",
                    UIResources.NameLabelText, Name,
                    UIResources.IdLabelText, ShortId);

                if (IsExpanded)
                {
                    text = String.Join(" ", text,
                    UIResources.ImageLabelText, Image,
                    UIResources.CommandLabelText, Command,
                    UIResources.StatusLabelText, Status,
                    UIResources.CreatedLabelText, Created,
                    UIResources.PortsLabelText, !String.IsNullOrEmpty(Instance.Ports) ? FormattedListOfPorts : UIResources.NoPortsText);
                }
                return text;
            }
        }

        public string ExpanderItemStatus
        {
            get
            {
                return IsExpanded ? UIResources.Expanded : UIResources.Collapsed;
            }
        }
        #endregion
    }

    public class ContainerProperty
    {
        public ContainerProperty(IContainerViewModel viewModel, string key, string value)
        {
            ViewModel = viewModel;
            Key = key;
            Value = value;
        }

        public ContainerProperty(KeyValuePair<string, string> property)
        {
            Key = property.Key;
            Value = property.Value;
        }

        public string Key { get; }
        public string Value { get; }
        public IContainerViewModel ViewModel { get; }
    }
}
