// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.DebugEngineHost;

namespace MICore
{
    /// <summary>
    /// Class which implements logging.
    /// </summary>
    public class Logger
    {
        private static bool s_isInitialized;
        private static bool s_isEnabled;
        private static DateTime s_initTime;
        /// <summary>
        /// Optional logger to get engine diagnostics logs
        /// </summary>
        private ILogChannel EngineLogger => HostLogger.GetEngineLogChannel();
        /// <summary>
        /// Optional logger to get natvis diagnostics logs
        /// </summary>
        public ILogChannel NatvisLogger => HostLogger.GetNatvisLogChannel();
        private static int s_count;
        private readonly int _id;

        #region Command Window

        public class LogInfo
        {
            public string logFile;
            public Action<string> logToOutput;
            public bool enabled;
        };

        private readonly static LogInfo s_cmdLogInfo = new LogInfo();
        public static LogInfo CmdLogInfo { get { return s_cmdLogInfo; } }

        #endregion

        private Logger()
        {
            _id = Interlocked.Increment(ref s_count);
        }

        public static Logger EnsureInitialized()
        {
            Logger res = new Logger();
            if (!s_isInitialized)
            {
                s_isInitialized = true;
                s_initTime = DateTime.Now;

                LoadMIDebugLogger();
                res.WriteLine(LogLevel.Verbose, "Initialized log at: " + s_initTime.ToString(CultureInfo.InvariantCulture));
            }

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                s_isEnabled = true;
            }
#endif
            return res;
        }

        public static void EnableNatvisDiagnostics(LogLevel level)
        {
            HostLogger.EnableNatvisDiagnostics(HostOutputWindow.WriteLaunchError, level);
        }

        public static void LoadMIDebugLogger()
        {
            if (CmdLogInfo.enabled)
            {   // command configured log file
                HostLogger.Reset();
                HostLogger.SetEngineLogFile(CmdLogInfo.logFile);
                HostLogger.EnableHostLogging(CmdLogInfo.logToOutput);
            }

            s_isEnabled = true;
        }

        public static void Reset()
        {
            if (CmdLogInfo.enabled)
            {
                HostLogger.Reset();
                s_isEnabled = false;
                s_isInitialized = false;
            }
        }

        /// <summary>
        /// If logging is enabled, writes a line of text to the log
        /// </summary>
        /// <param name="level">[Required] The level of the log.</param>
        /// <param name="line">[Required] line to write</param>
        public void WriteLine(LogLevel level, string line)
        {
            if (s_isEnabled)
            {
                WriteLineImpl(level, line);
            }
        }

        /// <summary>
        /// If logging is enabled, writes a line of text to the log
        /// </summary>
        /// <param name="level">[Required] The level of the log.</param>
        /// <param name="format">[Required] format string</param>
        /// <param name="args">arguments to use in the format string</param>
        public void WriteLine(LogLevel level, string format, params object[] args)
        {
            if (s_isEnabled)
            {
                WriteLineImpl(level, format, args);
            }
        }

        /// <summary>
        /// If logging is enabled, writes a block of text which may contain newlines to the log
        /// </summary>
        /// <param name="level">[Required] The level of the log.</param>
        /// <param name="prefix">[Optional] Prefix to put on the front of each line</param>
        /// <param name="textBlock">Block of text to write</param>
        public void WriteTextBlock(LogLevel level, string prefix, string textBlock)
        {
            if (s_isEnabled)
            {
                WriteTextBlockImpl(level, prefix, textBlock);
            }
        }

        /// <summary>
        /// If logging is enabled, flushes the log to disk
        /// </summary>
        public void Flush()
        {
            if (s_isEnabled)
            {
                FlushImpl();
            }
        }

        public static bool IsEnabled
        {
            get { return s_isEnabled; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Disable inlining since logging is off by default, and we want to allow the public method to be inlined
        private void WriteLineImpl(LogLevel level, string line)
        {
            string fullLine = String.Format(CultureInfo.CurrentCulture, "{2}: ({0}) {1}", (int)(DateTime.Now - s_initTime).TotalMilliseconds, line, _id);
            HostLogger.GetEngineLogChannel()?.WriteLine(level, fullLine);
#if DEBUG
            Debug.WriteLine("MS_MIDebug: " + fullLine);
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Disable inlining since logging is off by default, and we want to allow the public method to be inlined
        private static void FlushImpl()
        {
            HostLogger.GetEngineLogChannel()?.Flush();
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Disable inlining since logging is off by default, and we want to allow the public method to be inlined
        private void WriteLineImpl(LogLevel level, string format, object[] args)
        {
            WriteLineImpl(level, string.Format(CultureInfo.CurrentCulture, format, args));
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Disable inlining since logging is off by default, and we want to allow the public method to be inlined
        private void WriteTextBlockImpl(LogLevel level, string prefix, string textBlock)
        {
            using (var reader = new StringReader(textBlock))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;

                    if (!string.IsNullOrEmpty(prefix))
                        WriteLineImpl(level, prefix + line);
                    else
                        WriteLineImpl(level, line);
                }
            }
        }
    }
}
