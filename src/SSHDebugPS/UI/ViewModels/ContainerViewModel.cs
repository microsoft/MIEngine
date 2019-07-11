// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using liblinux.Shell;
using Microsoft.SSHDebugPS.Docker;
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
        }

        public abstract bool GetResult(out string selectedQualifier);
        public abstract WindowsInput.ICommand ExpandCommand { get; }

        #region INotifyPropertyChanged and helper

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion

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

        public override WindowsInput.ICommand ExpandCommand => new ContainerUICommand(Expand, "ExpandDetails", "ExpandToolTip");

        private void Expand(object parameter)
        {
            IsExpanded = !IsExpanded;
        }

        // Gets the first 12 characters and appends an ellipsis
        public string ShortId { get => Id.Substring(0, 12); }
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
    }
}
