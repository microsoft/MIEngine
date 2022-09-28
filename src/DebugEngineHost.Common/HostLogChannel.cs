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
        /// Logs that are used for interactive investigation during development.
        /// These logs should primarily contain information useful for debugging and have no long-term value.
        /// </summary>
        Verbose,
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
        /// Not used for writing log messages.
        /// Specifies that a logging category should not write any messages.
        /// </summary>
        None
    }

    // This must match the interface in DebugEngineHost.ref.cs
    public interface ILogChannel
    {
        void WriteLine(LogLevel level, string message);

        void WriteLine(LogLevel level, string format, params object[] values);

        void Flush();

        void Close();
    }

    public class HostLogChannel : ILogChannel
    {
        private readonly Action<string> _log;
        private readonly StreamWriter _logFile;
        private LogLevel _logLevel;

        private readonly object _lock = new object();

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

        /// <summary>
        /// Sets the log level to the provided level.
        /// </summary>
        /// <param name="level">The level to set the logger.</param>
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
            lock (_lock)
            {
                if (level >= _logLevel)
                {
                    string prefix = string.Empty;
                    // Only indicate level if not verbose.
                    if (level != LogLevel.Verbose)
                    {
                        prefix = string.Format(CultureInfo.InvariantCulture, "[{0}] ", level.ToString());
                    }
                    string levelMsg = string.Format(CultureInfo.InvariantCulture, "{0}{1}", prefix, message);
                    _log?.Invoke(levelMsg);
                    _logFile?.WriteLine(levelMsg);
                    _logFile?.Flush();
                }

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
            lock (_lock)
            {
                if (level >= _logLevel)
                {
                    string prefix = string.Empty;
                    // Only indicate level if not verbose.
                    if (level != LogLevel.Verbose)
                    {
                        prefix = string.Format(CultureInfo.InvariantCulture, "[{0}] ", level.ToString());
                    }
                    string message = string.Format(CultureInfo.InvariantCulture, format, values);
                    string levelMsg = string.Format(CultureInfo.InvariantCulture, "{0}{1}", prefix, message);
                    _log?.Invoke(levelMsg);
                    _logFile?.WriteLine(levelMsg);
                    _logFile?.Flush();
                }
            }
        }

        public void Flush()
        {
            lock (_lock)
            {
                _logFile?.Flush();
            }
        }

        public void Close()
        {
            lock (_lock)
            {
                _logFile?.Close();
            }
        }
    }
}
