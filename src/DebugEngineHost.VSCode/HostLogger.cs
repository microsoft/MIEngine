// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DebugEngineHost
{
    public static class HostLogger
    {
        private static ILogChannel s_natvisLogChannel;
        private static ILogChannel s_engineLogChannel;

        private static string s_engineLogFile;

        public static void EnableNatvisDiagnostics(Action<string> callback, LogLevel level = LogLevel.Verbose)
        {
            if (s_natvisLogChannel == null)
            {
                // TODO: Support writing natvis logs to a file.
                s_natvisLogChannel = new HostLogChannel(callback, null, level);
            }
        }

        public static void EnableHostLogging(Action<string> callback, LogLevel level = LogLevel.Verbose)
        {
            if (s_engineLogChannel == null)
            {
                s_engineLogChannel = new HostLogChannel(callback, s_engineLogFile, level);
            }
        }

        public static void SetEngineLogFile(string logFile)
        {
            s_engineLogFile = logFile;
        }

        public static ILogChannel GetEngineLogChannel()
        {
            return s_engineLogChannel;
        }

        public static ILogChannel GetNatvisLogChannel()
        {
            return s_natvisLogChannel;
        }

        public static void Reset()
        {
            s_natvisLogChannel?.Close();
            s_natvisLogChannel = null;
            s_engineLogChannel?.Close();
            s_engineLogChannel = null;
        }
    }
}
