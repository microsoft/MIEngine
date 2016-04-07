// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace AndroidDebugLauncher
{
    internal static class StringExtensions
    {
        public static IEnumerable<string> GetLines(this string content)
        {
            using (var reader = new StringReader(content))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;

                    yield return line;
                }
            }
        }
    }
}
