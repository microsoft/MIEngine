// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.SSHDebugPS.Docker
{
    internal class TextToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(Visibility) && value is string text)
            {
                // Could have a parameter
                if (parameter != null)
                {
                    // Only allowed param is flip
                    return !string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
                }
                return !string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
