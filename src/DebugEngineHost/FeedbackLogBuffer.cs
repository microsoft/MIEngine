// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

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
                    return new string[0];
                }

                int skipCount = _logBuffer.Count - (int)newEntryCount;
                return _logBuffer.Skip(skipCount).ToList().AsReadOnly();
            }
        }
    }
}
