// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Microsoft.DebugEngineHost
{
    public static class HostLogger
    {
        private static ILogChannel s_natvisLogChannel;
        private static ILogChannel s_engineLogChannel;

        private static string s_engineLogFile;

        private static FeedbackLogBuffer s_circularBuffer;
        private static VSFeedbackLogger s_feedbackLogger;

        public static void EnableHostLogging(Action<string> callback, LogLevel level = LogLevel.Verbose)
        {
            if (s_engineLogChannel == null)
            {
                s_engineLogChannel = new HostLogChannel(callback, s_engineLogFile, level);
            }

            if (s_feedbackLogger == null)
            {
                s_feedbackLogger = new VSFeedbackLogger(EnsureFeedbackBuffer());
            }
        }

        public static void EnableNatvisDiagnostics(Action<string> callback, LogLevel level = LogLevel.Verbose)
        {
            if (s_natvisLogChannel== null)
            {
                s_natvisLogChannel = new HostLogChannel(callback, null, level);
            }
        }

        public static void DisableNatvisDiagnostics()
        {
            s_natvisLogChannel = null;
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

        /// <summary>
        /// Returns true if the feedback log is currently active (buffer has been created).
        /// </summary>
        public static bool IsFeedbackLogEnabled
        {
            get { return s_circularBuffer != null; }
        }

        /// <summary>
        /// Writes a message to the feedback circular buffer and, if active, to the feedback log file.
        /// </summary>
        public static void WriteFeedbackLog(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            string timestamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string logLine = string.Format(CultureInfo.InvariantCulture, "{0}: {1}", timestamp, message);

            EnsureFeedbackBuffer().Write(logLine);
            s_feedbackLogger?.Write(logLine);
        }

        private static FeedbackLogBuffer EnsureFeedbackBuffer()
        {
            if (s_circularBuffer == null)
            {
                Interlocked.CompareExchange(ref s_circularBuffer, new FeedbackLogBuffer(), null);
            }

            return s_circularBuffer;
        }

        /// <summary>
        /// Returns true if feedback entries have ever been written during this session.
        /// </summary>
        internal static bool HasFeedbackEntries
        {
            get
            {
                FeedbackLogBuffer buffer = s_circularBuffer;
                return buffer != null && buffer.HasEntries;
            }
        }

        /// <summary>
        /// Returns log entries added since the last flush, then advances the flush marker.
        /// </summary>
        internal static IReadOnlyCollection<string> GetNewFeedbackEntries()
        {
            return s_circularBuffer?.FlushNewEntries() ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets the path for the feedback log file for a given VS process ID.
        /// </summary>
        internal static string GetFeedbackLogFilePath(int vsPid)
        {
            return Path.Combine(Path.GetTempPath(), string.Format(CultureInfo.InvariantCulture, "Microsoft.VisualStudio.MIDebugEngine-{0}.log", vsPid));
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
