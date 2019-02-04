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

namespace Microsoft.SSHDebugPS
{
    /// <summary>
    /// Interaction logic for DockerContainerPickerWindow.xaml
    /// </summary>
    public partial class ContainerPicker : DialogWindow
    {
        public ContainerPicker()
        {
            InitializeComponent();
            this.DataContext = new ContainerPickerViewModel();
        }

    }
}
