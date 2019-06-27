using System;
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
    }
}
