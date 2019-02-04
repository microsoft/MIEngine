using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.SSHDebugPS
{
    public class ContainerPickerViewModel : INotifyPropertyChanged
    {
        public ContainerPickerViewModel()
        {

        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
        }

        public bool IsContainersVisible { get { return true; } }

        private void _directConnectedListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
