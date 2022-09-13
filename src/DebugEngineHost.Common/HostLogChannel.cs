// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*
 * NOTE: This file is shared between DebugEngineHost and DebugEngineHost.VSCode
 */

using System;
using System.Globalization;
using System.IO;

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
        private readonly Action<string> _log;
        private readonly StreamWriter _logFile;
        private LogLevel _logLevel;

        private HostLogChannel() { }

        public HostLogChannel(Action<string> logAction, string file, LogLevel logLevel)
        {
            _log = logAction;

            if (!string.IsNullOrEmpty(file))
            {
                _logFile = File.CreateText(file);
            }

            _logLevel = logLevel;
        }

        public void SetLogLevel(LogLevel level)
        {
            _logLevel = level;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="verbosity"></param>
        /// <param name="message"></param>
        public void WriteLine(LogLevel level, string message)
        {
            if (level >= _logLevel)
            {
                string levelMsg = string.Format(CultureInfo.InvariantCulture, "[{0}] {1}", level.ToString(), message);
                _log?.Invoke(levelMsg);
                _logFile?.WriteLine(levelMsg);
                _logFile?.Flush();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="verbosity"></param>
        /// <param name="format"></param>
        /// <param name="values"></param>
        public void WriteLine(LogLevel level, string format, params object[] values)
        {
            if (level >= _logLevel)
            {
                string message = string.Format(CultureInfo.InvariantCulture, format, values);
                string levelMsg = string.Format(CultureInfo.InvariantCulture, "[{0}] {1}", level.ToString(), message);
                _log?.Invoke(levelMsg);
                _logFile?.WriteLine(levelMsg);
                _logFile?.Flush();
            }
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
}
