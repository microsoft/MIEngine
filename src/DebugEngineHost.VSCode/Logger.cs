// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// VS Code only class to write to the log. This is enabled through the '--engineLogging[=file]' command line argument.
    /// </summary>
    public static class Logger
    {
        public static void WriteFrame([CallerMemberName]string caller = null)
        {
            CoreWrite(caller);
        }

        public static void WriteLine(string s)
        {
            CoreWrite(s);
        }

        private static void CoreWrite(string line)
        {
            HostLogger.Instance?.WriteLine(line);
        }
    }
}
