// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;

namespace Microsoft.DebugEngineHost
{
    public class HostLogChannel
    {
        private readonly Action<string> _log;
        private readonly StreamWriter _logFile;

        public HostLogChannel(Action<string> logAction, string file)
        {
            _log = logAction;

            if (!string.IsNullOrEmpty(file))
            {
                _logFile = File.CreateText(file);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="verbosity"></param>
        /// <param name="message"></param>
        public void WriteLine(string message)
        {
            _log?.Invoke(message);
            _logFile?.WriteLine(message);
            _logFile?.Flush();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="verbosity"></param>
        /// <param name="format"></param>
        /// <param name="values"></param>
        public void WriteLine(string format, params object[] values)
        {
            string message = string.Format(CultureInfo.InvariantCulture, format, values);
            _log?.Invoke(message);
            _logFile?.WriteLine(message);
            _logFile?.Flush();
        }

        public void Flush()
        {
            _logFile?.Flush();
        }

        public void Close()
        {
            _logFile?.Close();
        }
    }

    public sealed class HostLogger
    {
        private static HostLogChannel s_natvisLogChannel;
        public static HostLogChannel s_engineLogChannel;

        public static void EnableNatvisLogger(Action<string> callback)
        {
            if (s_natvisLogChannel == null)
            {
                // TODO: Support writing natvis logs to a file.
                s_natvisLogChannel = new HostLogChannel(callback, null);
            }
        }

        public static void EnableHostLogging(Action<string> callback, string logFile)
        {
            if (s_engineLogChannel == null)
            {
                s_engineLogChannel = new HostLogChannel(callback, logFile);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static HostLogChannel GetEngineLogChannel()
        {
            return s_engineLogChannel;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static HostLogChannel GetNatvisLogChannel()
        {
            return s_natvisLogChannel;
        }

        public static void Reset()
        {
            s_natvisLogChannel = null;
            s_engineLogChannel = null;
        }
    }
}
