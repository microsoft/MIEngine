// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// In-memory circular buffer that caches recent log messages for feedback reports.
    /// </summary>
    internal class FeedbackLogBuffer
    {
        private const int s_maxLogBufferSize = 256 * 1024;

        private readonly LinkedList<string> _logBuffer = new LinkedList<string>();
        private int _logLength;
        private long _writeSequence;
        private long _lastFlushSequence;
        private readonly object _syncObj = new object();

        /// <summary>
        /// Returns true if no entries have been written since the last flush.
        /// </summary>
        internal bool IsEmpty
        {
            get
            {
                lock (_syncObj)
                {
                    return _writeSequence == _lastFlushSequence;
                }
            }
        }

        internal void Write(string logLine)
        {
            lock (_syncObj)
            {
                if (logLine.Length > s_maxLogBufferSize)
                {
                    logLine = logLine.Substring(0, s_maxLogBufferSize);
                }

                _logBuffer.AddLast(logLine);
                _logLength += logLine.Length;
                _writeSequence++;

                while (_logLength > s_maxLogBufferSize)
                {
                    string entry = _logBuffer.First();
                    _logLength -= entry.Length;
                    _logBuffer.RemoveFirst();
                }
            }
        }

        internal IReadOnlyCollection<string> FlushNewEntries()
        {
            lock (_syncObj)
            {
                long newEntryCount = System.Math.Min(_writeSequence - _lastFlushSequence, _logBuffer.Count);
                _lastFlushSequence = _writeSequence;

                if (newEntryCount <= 0)
                {
                    return Array.Empty<string>();
                }

                int skipCount = _logBuffer.Count - (int)newEntryCount;
                return _logBuffer.Skip(skipCount).ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Opens the feedback log file for appending with shared read/write access.
        /// </summary>
        internal static StreamWriter OpenLogFile(string logFileName)
        {
            var fs = new FileStream(logFileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            try
            {
                return new StreamWriter(fs, Encoding.UTF8);
            }
            catch
            {
                fs.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Writes a collection of log entries to the given writer.
        /// </summary>
        internal static void WriteEntries(StreamWriter writer, IEnumerable<string> entries)
        {
            foreach (string logLine in entries)
            {
                writer.WriteLine(logLine);
            }

            writer.Flush();
        }
    }
}
