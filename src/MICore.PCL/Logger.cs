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
    public static class Logger
    {
        private static bool s_isInitialized;
        private static bool s_isEnabled;
        private static DateTime s_initTime;

        // NOTE: We never clean this up
        private static StreamWriter s_streamWriter;

        public static void EnsureInitialized(HostConfigurationStore configStore)
        {
            if (!s_isInitialized)
            {
                s_isInitialized = true;
                s_initTime = DateTime.Now;

                object enableMILoggerValue = configStore.GetOptionalValue("Debugger", "EnableMIDebugLogger");
                if (IsRegValueTrue(enableMILoggerValue))
                {
                    string tempDirectory = Environment.GetEnvironmentVariable("TMP");
                    if (!string.IsNullOrEmpty(tempDirectory) && Directory.Exists(tempDirectory))
                    {
                        string filePath = Path.Combine(tempDirectory, "Microsoft.MIDebug.log");

                        try
                        {
                            FileStream stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                            s_streamWriter = new StreamWriter(stream);
                            s_isEnabled = true;
                        }
                        catch (IOException)
                        {
                            // ignore failures from the log being in use by another process
                        }
                    }
                }
                WriteLine("Initialized log at: " + s_initTime);
            }

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                s_isEnabled = true;
            }
#endif
        }

        /// <summary>
        /// If logging is enabled, writes a line of text to the log
        /// </summary>
        /// <param name="line">[Required] line to write</param>
        public static void WriteLine(string line)
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
        public static void WriteLine(string format, params object[] args)
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
        public static void WriteTextBlock(string prefix, string textBlock)
        {
            if (s_isEnabled)
            {
                WriteTextBlockImpl(prefix, textBlock);
            }
        }

        /// <summary>
        /// If logging is enabled, flushes the log to disk
        /// </summary>
        public static void Flush()
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
        private static void WriteLineImpl(string line)
        {
            if (s_streamWriter != null)
            {
                lock (s_streamWriter)
                {
                    s_streamWriter.WriteLine(String.Format(CultureInfo.CurrentCulture, "({0}) {1}", (int)(DateTime.Now - s_initTime).TotalMilliseconds, line));
                }
            }

#if DEBUG
            Debug.WriteLine("MS_MIDebug: " + String.Format(CultureInfo.CurrentCulture, "({0}) {1}", (int)(DateTime.Now - s_initTime).TotalMilliseconds, line));
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Disable inlining since logging is off by default, and we want to allow the public method to be inlined
        private static void FlushImpl()
        {
            if (s_streamWriter != null)
            {
                lock (s_streamWriter)
                {
                    s_streamWriter.Flush();
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Disable inlining since logging is off by default, and we want to allow the public method to be inlined
        private static void WriteLineImpl(string format, object[] args)
        {
            WriteLineImpl(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Disable inlining since logging is off by default, and we want to allow the public method to be inlined
        private static void WriteTextBlockImpl(string prefix, string textBlock)
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

        private static bool IsRegValueTrue(object enableMILoggerValue)
        {
            if (enableMILoggerValue == null)
                return false;
            if (!(enableMILoggerValue is int))
                return false;

            return ((int)enableMILoggerValue) != 0;
        }
    }
}
