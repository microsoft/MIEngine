// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace OpenDebugAD7
{
    internal static class LanguageUtilities
    {
        /// <summary>
        /// This method does a simple validation on the input 'identifier'.
        /// </summary>
        /// <param name="identifier">the identifier to validate</param>
        /// <returns>'true' if this is a valid identifier name. 'false' if otherwise.</returns>
        public static bool IsValidIdentifier(string identifier)
        {
            // Check to see if identifier is null or blank.
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return false;
            }

            /*
             * Most languages require the first character in the indentifier not be a number.
             * 
             * Languages:
             * - C
             * - C++
             * - Rust
             * - Python
             */
            string digits = "0123456789";
            if (digits.Contains(identifier[0], StringComparison.Ordinal))
            {
                return false;
            }

            /*
             * Check to see if we got an '=' if the user is trying to do a 
             * comparison on a non-existant psuedo variable like 'name'.
             */
            if (identifier.Where(c => c == '=').Any())
            {
                return false;
            }

            return true;
        }
    }
}
