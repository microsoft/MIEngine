// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.SSHDebugPS.Utilities
{
    internal static class StringExtensionMethods
    {
        public static string FormatInvariantWithArgs(this string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        public static string FormatCurrentCultureWithArgs(this string format, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, format, args);
        }

        public static string ToInvariant(this FormattableString value)
        {
            return FormattableString.Invariant(value);
        }

        public static void AssertIfNotEmpty(this string value)
        {
#if DEBUG
            Debug.Assert(string.IsNullOrEmpty(value), "String is expected to be empty but not. Value: {0}".FormatInvariantWithArgs(value));
#endif
        }
    }
}
