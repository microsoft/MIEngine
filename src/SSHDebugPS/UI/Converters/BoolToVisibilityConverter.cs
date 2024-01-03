// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.SSHDebugPS.Docker
{
    internal class BoolToVisibilityConverter : IValueConverter
    {
        public bool Negative { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(Visibility))
            {
                if (Negative)
                {
                    // Only allowed param is flip
                    return (bool)value ? Visibility.Collapsed : Visibility.Visible;
                }
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
