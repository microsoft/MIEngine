// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DebuggerTesting
{
    public static class StringExtensions
    {
        /// <summary>
        /// Formats the format string with the given arguments using the current culture.
        /// </summary>
        public static string FormatWithArgs(this string format, params object[] args)
        {
            return format.FormatWithArgs(CultureInfo.CurrentCulture, args);
        }

        /// <summary>
        /// Formats the format string with the given arguments using the invariant culture.
        /// </summary>
        public static string FormatInvariantWithArgs(this string format, params object[] args)
        {
            return format.FormatWithArgs(CultureInfo.InvariantCulture, args);
        }

        /// <summary>
        /// Formats the format string with the given arguments using the given culture.
        /// </summary>
        private static string FormatWithArgs(this string format, IFormatProvider provider, params object[] args)
        {
            return String.Format(provider, format, args);
        }

        /// <summary>
        /// Determines if string has ASCII characters only.
        /// </summary>
        public static bool HasAsciiOnly(this string value)
        {
            if (String.IsNullOrEmpty(value))
                return true;

            return value.All(c => c <= 0x7F);
        }

        /// <summary>
        /// Converts a string to its base64 representation using the given encoding.
        /// </summary>
        public static string ToBase64String(this string value, Encoding encoding)
        {
            Parameter.ThrowIfNull(encoding, nameof(encoding));

            if (String.IsNullOrEmpty(value))
                return value;

            return Convert.ToBase64String(encoding.GetBytes(value));
        }

        /// <summary>
        /// Creates a string representation of the contents of the dictionary.
        /// Returns a line for each entry in the format "key=value"
        /// </summary>
        public static string ToReadableString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, string prefix = null)
        {
            if (dictionary == null || dictionary.Count == 0)
                return string.Empty;

            // Creates an aggregate that accumulates in a string builder
            return dictionary.Aggregate(
                new StringBuilder(),
                (sb, x) => sb.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}={2}", prefix, x.Key, x.Value).AppendLine(),
                sb => sb.ToString());
        }

        /// <summary>
        /// Parses a string to int. If the string is not a valid int, this converts to null
        /// </summary>
        public static int? ToInt(this string value)
        {
            int x;
            if (int.TryParse(value, out x))
                return x;
            else
                return null;
        }

        /// <summary>
        /// Parses a string to bool. If the string is not a valid bool, this converts to null
        /// </summary>
        public static bool? ToBool(this string value)
        {
            bool x;
            if (bool.TryParse(value, out x))
                return x;
            else
                return null;
        }

    }
}