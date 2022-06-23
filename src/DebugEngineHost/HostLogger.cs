// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{
    public enum LogLevel
    {
        /// <summary>
        /// Logs that contain the most detailed messages.
        /// These messages may contain sensitive application data.
        /// These messages are disabled by default and should never be enabled in a production environment.
        /// </summary>
        Trace,
        /// <summary>
        /// Logs that are used for interactive investigation during development.
        /// These logs should primarily contain information useful for debugging and have no long-term value.
        /// </summary>
        Debug,
        /// <summary>
        /// Logs that track the general flow of the application.
        /// These logs should have long-term value.
        /// </summary>
        Information,
        /// <summary>
        /// Logs that highlight an abnormal or unexpected event in the application flow, but do not otherwise cause the application execution to stop.
        /// </summary>
        Warning,
        /// <summary>
        /// Logs that highlight when the current flow of execution is stopped due to a failure.
        /// These should indicate a failure in the current activity, not an application-wide failure.
        /// </summary>
        Error,
        /// <summary>
        /// Logs that describe an unrecoverable application or system crash, or a catastrophic failure that requires immediate attention.
        /// </summary>
        Critical,
        /// <summary>
        /// Not used for writing log messages.
        /// Specifies that a logging category should not write any messages.
        /// </summary>
        None
    }

    public class HostLogChannel
    {
        private readonly Action<LogLevel, string> _log;
        private readonly StreamWriter _logFile;

        private readonly object _lock = new object();

        public HostLogChannel(Action<LogLevel, string> logAction, string file)
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
        public void WriteLine(LogLevel verbosity, string message)
        {
            _log?.Invoke(verbosity, message);
            _logFile?.WriteLine(message);
            _logFile?.Flush();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="verbosity"></param>
        /// <param name="format"></param>
        /// <param name="values"></param>
        public void WriteLine(LogLevel verbosity, string format, params object[] values)
        {
            string message = string.Format(CultureInfo.InvariantCulture, format, values);
            _log?.Invoke(verbosity, message);
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

        public static void InitalizeNatvisLogger(Action<LogLevel, string> callback)
        {
            if (s_natvisLogChannel == null)
            {
                // TODO: Support writing natvis logs to a file.
                s_natvisLogChannel = new HostLogChannel(callback, null);
            }
        }

        public static void InitalizeEngineLogger(Action<LogLevel, string> callback, string logFile)
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
