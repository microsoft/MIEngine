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
    /// Class which implements logging. The logging is control by a registry key. If enabled, logging goes to %TMP%\Microsoft.MIDebug.log
    /// </summary>
    public class Logger
    {
        private static bool s_isInitialized;
        private static bool s_isEnabled;
        private static DateTime s_initTime;
        // NOTE: We never clean this up
        private static HostLogger s_logger;
        private static int s_count;
        private int _id;

        private Logger()
        {
            _id = Interlocked.Increment(ref s_count);
        }

        public static Logger EnsureInitialized(HostConfigurationStore configStore)
        {
            Logger res = new Logger();
            if (!s_isInitialized)
            {
                s_isInitialized = true;
                s_initTime = DateTime.Now;

                LoadMIDebugLogger(configStore);
                res.WriteLine("Initialized log at: " + s_initTime);
            }

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                s_isEnabled = true;
            }
#endif
            return res;
        }

        public static void LoadMIDebugLogger(HostConfigurationStore configStore)
        {
            if (s_logger == null)
            {
                s_logger = configStore.GetLogger("EnableMIDebugLogger", "Microsoft.MIDebug.log");
                if (s_logger != null)
                {
                    s_isEnabled = true;
                }
            }
        }

        /// <summary>
        /// If logging is enabled, writes a line of text to the log
        /// </summary>
        /// <param name="line">[Required] line to write</param>
        public void WriteLine(string line)
        {
            if (s_isEnabled)
            {
                WriteLineImpl(line);
            }
        }

        /// <summary>
        /// If logging is enabled, writes a line of text to the log
        /// </summary>
        /// <param name="format">[Required] format string</param>
        /// <param name="args">arguments to use in the format string</param>
        public void WriteLine(string format, params object[] args)
        {
            if (s_isEnabled)
            {
                WriteLineImpl(format, args);
            }
        }

        /// <summary>
        /// If logging is enabled, writes a block of text which may contain newlines to the log
        /// </summary>
        /// <param name="prefix">[Optional] Prefix to put on the front of each line</param>
        /// <param name="textBlock">Block of text to write</param>
        public void WriteTextBlock(string prefix, string textBlock)
        {
            if (s_isEnabled)
            {
                WriteTextBlockImpl(prefix, textBlock);
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
        private void WriteLineImpl(string line)
        {
            string fullLine = String.Format(CultureInfo.CurrentCulture, "{2}: ({0}) {1}", (int)(DateTime.Now - s_initTime).TotalMilliseconds, line, _id);
            s_logger?.WriteLine(fullLine);
#if DEBUG
            Debug.WriteLine("MS_MIDebug: " + fullLine);
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Disable inlining since logging is off by default, and we want to allow the public method to be inlined
        private static void FlushImpl()
        {
            s_logger?.Flush();
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Disable inlining since logging is off by default, and we want to allow the public method to be inlined
        private void WriteLineImpl(string format, object[] args)
        {
            WriteLineImpl(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Disable inlining since logging is off by default, and we want to allow the public method to be inlined
        private void WriteTextBlockImpl(string prefix, string textBlock)
        {
            using (var reader = new StringReader(textBlock))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;

                    if (!string.IsNullOrEmpty(prefix))
                        WriteLineImpl(prefix + line);
                    else
                        WriteLineImpl(line);
                }
            }
        }
    }
}
