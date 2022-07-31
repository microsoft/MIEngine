// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAREditor
{
    internal class ViewModel : NotifyPropertyChangedImpl
    {
        string _statusText;

        public ViewModel()
        {
            _statusText = "TODO";
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(nameof(StatusText), ref _statusText, value);
        }
    }
}
