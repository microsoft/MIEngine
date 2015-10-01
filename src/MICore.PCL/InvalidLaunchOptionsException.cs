// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace MICore
{
    public class InvalidLaunchOptionsException : Exception
    {
        internal InvalidLaunchOptionsException(string problemDescription) :
            base(string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_InvalidLaunchOptions, problemDescription))
        {
        }
    }
}