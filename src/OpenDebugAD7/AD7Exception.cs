// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace OpenDebugAD7
{
    internal class AD7Exception : Exception
    {
        public AD7Exception(string message) : base(message)
        {
        }

        public AD7Exception(string scenario, string reason) : base(string.Format(CultureInfo.CurrentCulture, scenario, reason))
        {
        }
    }
}