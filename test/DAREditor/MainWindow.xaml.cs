// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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

namespace DAREditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly ViewModel m_viewModel = new ViewModel();

        public MainWindow()
        {
            this.DataContext = m_viewModel;
            InitializeComponent();

            // NOTE: This is supposed to add highlighting
            //TextRange textRange = new TextRange(...);
            //textRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Yellow);
        }
    }
}
