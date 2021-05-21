// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit.Abstractions;

namespace DebuggerTesting
{
    /// <summary>
    /// A component that can log to the test output.
    /// </summary>
    public interface ILoggingComponent
    {
        ITestOutputHelper OutputHelper { get; }
    }

    /// <summary>
    /// Provide some extensions to a test logging component.
    /// </summary>
    public static class LoggingTestExtensions
    {
        public static void WriteLine(this ILoggingComponent component, string message = "", params object[] args)
        {
            // Invalid XML characters cause xUnit not to write the whole results file
            // For now we only handle null characters since that's what's causing issues and
            // this is just a temporary workaround.
            // See https://github.com/xunit/xunit/issues/876#issuecomment-253337669
            if (!string.IsNullOrEmpty(message))
            {
                message = message.Replace("\0", "<null>");
            }

            if (args.Length > 0)
                component?.OutputHelper?.WriteLine(message, args);
            else
                component?.OutputHelper?.WriteLine(message);
        }

        public static void WriteLines(this ILoggingComponent component, StreamReader reader)
        {
            Parameter.ThrowIfNull(reader, nameof(reader));

            string line = null;
            while (null != (line = reader.ReadLine()))
            {
                component.WriteLine(line);
            }
        }

        /// <summary>
        /// Log a message with details on the tests current settings
        /// </summary>
        public static void WriteSettings(this ILoggingComponent component, ITestSettings testSettings)
        {
            component.WriteLine("Test: {0}", testSettings.Name);
            component.WriteLine(testSettings.CompilerSettings.ToString());
            component.WriteLine(testSettings.DebuggerSettings.ToString());
        }

        /// <summary>
        /// Log a message commenting on what the test is currently trying to accomplish.
        /// </summary>
        public static void Comment(this ILoggingComponent component, string message, params object[] args)
        {
            component.WriteLine();
            component.WriteLine("# " + message + GetTimestamp(), args);
        }

        /// <summary>
        /// Log a message commenting on what the overall purpose of the test
        /// </summary>
        public static void TestPurpose(this ILoggingComponent component, string message)
        {
            string timestamp = GetTimestamp();
            string commentLine = new string('#', message.Length + timestamp.Length + 4);
            component.WriteLine(commentLine);
            component.WriteLine("# {0}{1} #", message , timestamp);
            component.WriteLine(commentLine);
        }

        private static string GetTimestamp()
        {
            return " ({0:HH:mm:ss.fff})".FormatInvariantWithArgs(DateTime.Now);
        }
    }
}
