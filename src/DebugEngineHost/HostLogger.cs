// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{
    public sealed class HostLogger
    {
        /// <summary>
        /// Callback for programmatic display of log messages
        /// </summary>
        /// <param name="outputString"></param>
        public delegate void OutputCallback(string outputString);

        private StreamWriter _streamWriter;
        private OutputCallback _callback;
        private readonly object _locker = new object();

        internal HostLogger(StreamWriter streamWriter = null, OutputCallback callback = null)
        {
            _streamWriter = streamWriter;
            _callback = callback;
        }

        public void WriteLine(string line)
        {
            lock (_locker)
            {
                if (_streamWriter != null)
                    _streamWriter.WriteLine(line);
                _callback?.Invoke(line);
            }
        }

        public void Flush()
        {
            lock (_locker)
            {
                if (_streamWriter != null)
                    _streamWriter.Flush();
            }
        }

        public void Close()
        {
            lock (_locker)
            {
                if (_streamWriter != null)
                    _streamWriter.Close();
                _streamWriter = null;
            }
        }

        public void Reassign(string logFileName, OutputCallback callback)
        {
            StreamWriter streamWriter = GetStreamForName(logFileName);
            Close();
            lock (_locker)
            {
                _streamWriter = streamWriter;
                _callback = callback;
            }
        }

        internal static StreamWriter GetStreamForName(string logFileName)
        {
            if (string.IsNullOrEmpty(logFileName))
            {
                return null;
            }
            string tempDirectory = Path.GetTempPath();
            StreamWriter writer = null;
            if (Path.IsPathRooted(logFileName) || (!string.IsNullOrEmpty(tempDirectory) && Directory.Exists(tempDirectory)))
            {
                string filePath = Path.Combine(tempDirectory, logFileName);

                try
                {
                    FileStream stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    writer = new StreamWriter(stream);
                }
                catch (IOException)
                {
                    // ignore failures from the log being in use by another process
                }
            }
            else
            {
                throw new ArgumentException("logFileName");
            }
            return writer;
        }
    }
}
