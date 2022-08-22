// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DAREditor
{
    internal class ViewModel : NotifyPropertyChangedImpl
    {
        string _statusText;

        JSONDescrepency jsonconvert = new JSONDescrepency();
        public ViewModel()
        {
          _statusText = "TODO";
            
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }
        

       

        
    }
   
}
