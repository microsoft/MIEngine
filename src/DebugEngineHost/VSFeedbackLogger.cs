// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Watches for the VS feedback tool's semaphore file and writes to a log file when recording is active.
    /// </summary>
    internal class VSFeedbackLogger
    {
        private const string s_vsFeedbackSemaphoreDir = @"Microsoft\VSFeedbackCollector";
        private const string s_vsFeedbackSemaphoreFile = "feedback.recording.json";

        private readonly int _vsPid;
        private readonly System.DateTime _vsStartTime;
        private bool _enabled;

        private readonly FileSystemWatcher _vsFeedbackFileWatcher;
        private readonly FeedbackLogBuffer _circularBuffer;

        private StreamWriter _logWriter;
        private readonly object _syncObj = new object();

        internal VSFeedbackLogger(FeedbackLogBuffer circularBuffer)
        {
            _circularBuffer = circularBuffer;

            try
            {
                Process vsProcess = Process.GetCurrentProcess();
                _vsPid = vsProcess.Id;
                _vsStartTime = vsProcess.StartTime;
                _enabled = false;

                string vsFeedbackTempDir = Path.Combine(Path.GetTempPath(), s_vsFeedbackSemaphoreDir);

                Directory.CreateDirectory(vsFeedbackTempDir);

                _vsFeedbackFileWatcher = new FileSystemWatcher(vsFeedbackTempDir, s_vsFeedbackSemaphoreFile);
                _vsFeedbackFileWatcher.Created += OnFeedbackSemaphoreCreated;
                _vsFeedbackFileWatcher.Deleted += OnFeedbackSemaphoreDeleted;
                _vsFeedbackFileWatcher.Changed += OnFeedbackSemaphoreChanged;

                if (File.Exists(Path.Combine(vsFeedbackTempDir, s_vsFeedbackSemaphoreFile)))
                {
                    OnFeedbackSemaphoreCreated(_vsFeedbackFileWatcher, new FileSystemEventArgs(WatcherChangeTypes.Created, vsFeedbackTempDir, s_vsFeedbackSemaphoreFile));
                }

                _vsFeedbackFileWatcher.EnableRaisingEvents = true;
            }
            catch
            {
                _vsFeedbackFileWatcher?.Dispose();
                _vsFeedbackFileWatcher = null;
            }
        }

        private void OnFeedbackSemaphoreCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (!_enabled && IsLoggingEnabledForThisVSInstance(e.FullPath))
                {
                    lock (_syncObj)
                    {
                        if (!_enabled)
                        {
                            string logFileName = HostLogger.GetFeedbackLogFilePath(_vsPid);
                            StreamWriter writer = FeedbackLogBuffer.OpenLogFile(logFileName);
                            try
                            {
                                FeedbackLogBuffer.WriteEntries(writer, _circularBuffer.FlushNewEntries());
                            }
                            catch
                            {
                                writer.Dispose();
                                throw;
                            }

                            _logWriter = writer;
                            _enabled = true;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void OnFeedbackSemaphoreDeleted(object sender, FileSystemEventArgs e)
        {
            lock (_syncObj)
            {
                if (_enabled)
                {
                    _enabled = false;
                    _circularBuffer.FlushNewEntries();

                    if (_logWriter != null)
                    {
                        _logWriter.Dispose();
                        _logWriter = null;
                    }
                }
            }
        }

        private void OnFeedbackSemaphoreChanged(object sender, FileSystemEventArgs e)
        {
            OnFeedbackSemaphoreCreated(sender, e);
        }

        private bool IsLoggingEnabledForThisVSInstance(string semaphoreFilePath)
        {
            try
            {
                if (_vsStartTime > File.GetCreationTime(semaphoreFilePath))
                {
                    return false;
                }

                string content = File.ReadAllText(semaphoreFilePath);
                JObject root = JObject.Parse(content);
                JContainer pidCollection = root["processIds"] as JContainer;
                if (pidCollection != null)
                {
                    return pidCollection.Values<int>().Contains(_vsPid);
                }
            }
            catch
            {
            }

            return false;
        }

        internal void Write(string logLine)
        {
            if (_enabled)
            {
                lock (_syncObj)
                {
                    if (_logWriter != null)
                    {
                        _logWriter.WriteLine(logLine);
                        _logWriter.Flush();
                    }
                }
            }
        }
    }
}
