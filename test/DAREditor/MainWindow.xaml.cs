// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        readonly ViewModel _viewModel = new ViewModel();

        public MainWindow()
        {
            this.DataContext = _viewModel;
            InitializeComponent();
            ActualRichTextBox.TextChanged += ActualRichTextBox_TextChanged;
            ExpectedRichTextBox.TextChanged += ExpectedRichTextBox_TextChanged;
        }

        bool statusExpected = true;
        bool statusActual = true;
        private void ExpectedRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string content = RichTextHelper.GetContent(ExpectedRichTextBox);
            string content1 = RichTextHelper.GetContent(ActualRichTextBox);
            if (JSONDescrepency.inputJSon(content, out Exception error) == null && error != null)
            {
                _viewModel.StatusText = $"Expected text is not JSON (expected): {error.Message}";
                statusExpected = false;
            }
            else
            {
                statusExpected = true;
                if (statusExpected && statusActual)
                {
                    _viewModel.StatusText = "JSON accepted";
                    Debug.WriteLine("Change sent. New content:", content);
                }
            }
        }

        private void ActualRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string content = RichTextHelper.GetContent(ActualRichTextBox);
            if (JSONDescrepency.inputJSon(content, out Exception error) == null && error != null)
            {
                _viewModel.StatusText = $"Actual Text is not JSON (actual) {error.Message}";
                statusActual = false;
            }
            else
            {
                statusActual = true;
                if (statusActual && statusExpected)
                {
                    _viewModel.StatusText = "JSON accepted";
                    Debug.WriteLine("Change sent. New content:", content);
                }
            }
        }

    }
}
