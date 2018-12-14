// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Globalization;

namespace OpenDebug
{
    public class Terminal
    {
        private static char[] s_ARGUMENT_SEPARATORS = new char[] { ' ', '\t' };

        /*
         * Enclose the given string in quotes if it contains space or tab characters.
         */
        public static string Quote(string arg)
        {
            if (arg.IndexOfAny(s_ARGUMENT_SEPARATORS) >= 0)
            {
                return '"' + arg + '"';
            }
            return arg;
        }
    }
}
