// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DebugEngineHost
{
    public sealed class HostLogger
    {
        public delegate void OutputCallback(string outputMessage);

        private static HostLogger s_instance;
        private static readonly object s_lock = new object();

        /// <summary>[Optional] VSCode-only host logger instance.</summary>
        public static HostLogger Instance { get { return s_instance; } }

        /// <summary>[Optional] VSCode-only method for obtaining the current host logger instance.</summary>
        public static void EnableHostLogging()
        {
            if (s_instance == null)
            {
                lock (s_lock)
                {
                    if (s_instance == null)
                    {
                        s_instance = new HostLogger();
                    }
                }
            }
        }

        private string _logFilePath = null;
        private System.IO.StreamWriter _logFile = null;

        /// <summary>Callback for logging text to the desired output stream.</summary>
        public Action<string> LogCallback { get; set; } = null;

        /// <summary>The path to the log file.</summary>
        public string LogFilePath
        {
            get
            {
                return _logFilePath;
            }
            set
            {
                _logFile?.Dispose();
                _logFilePath = value;

                if (!String.IsNullOrEmpty(_logFilePath))
                {
                    _logFile = System.IO.File.CreateText(_logFilePath);
                }
            }
        }

        private HostLogger() { }

        public void WriteLine(string line)
        {
            lock (s_lock)
            {
                _logFile?.WriteLine(line);
                _logFile?.Flush();
                LogCallback?.Invoke(line);
            }
        }

        public void Flush()
        {
        }

        public void Close()
        {
        }

        /// <summary>
        /// Get a logger after the user has explicitly configured a log file/callback
        /// </summary>
        /// <param name="logFileName"></param>
        /// <param name="callback"></param>
        /// <returns>The host logger object</returns>
        public static HostLogger GetLoggerFromCmd(string logFileName, HostLogger.OutputCallback callback)
        {
            throw new NotImplementedException();
        }
    }
}
