// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace DebuggerTesting
{
    public enum DebugUIResult
    {
        Unset = 0,
        Debug,
        Ignore,
        IgnoreAll,
        EndProcess,
    }

    public interface IDebugFailUI
    {
        DebugUIResult ShowFailure(string message, string callLocation, string exception, string callStack);
    }

    public static class UDebug
    {

        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Assert(bool condition, string format, params string[] parameters)
        {
            if (!condition)
                UDebug.Fail(format, parameters);
        }

        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void AssertNotNull<T>(T value, string format, params string[] parameters)
            where T : class
        {
            if (value == null)
                UDebug.Fail(format, parameters);
        }

        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void AssertNotNullOrEmpty(string value, string format, params string[] parameters)
        {
            if (string.IsNullOrEmpty(value))
                UDebug.Fail(format, parameters);
        }

        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void AssertService<T>(T service)
            where T : class
        {
            if (service == null)
                UDebug.Fail("Service {0} could not be loaded.", typeof(T).Name);
        }

        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Fail(string format, params object[] parameters)
        {
            UDebug.Fail(null, format, parameters);
        }

        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Fail(Exception ex)
        {
            UDebug.Fail(ex, string.Empty);
        }

        [Conditional("DEBUG")]
        [DebuggerHidden]
        public static void Fail(string message)
        {
            UDebug.Fail((Exception)null, message);
        }

        private static bool? showFailUI;

        private static bool? ShowFailUI
        {
            get
            {
                return showFailUI;
            }
            set
            {
                // Only allow the value to be set once.
                if (null == showFailUI)
                    showFailUI = value;
            }
        }

        [DebuggerHidden]
        [Conditional("DEBUG")]
        public static void Fail(Exception ex, string format, params string[] parameters)
        {
#if DEBUG
            string message = null;
            string exception = null;
            string callStack = null;

            if (!string.IsNullOrEmpty(format))
                message = string.Format(CultureInfo.InvariantCulture, format, parameters);

            if (ex != null)
            {
                exception = ExceptionToString(ex, 0, false);
                callStack = ExceptionToCallStack(ex);
            }

            FailInternal(message, exception, callStack);
#endif
        }

        [DebuggerHidden]
        [Conditional("DEBUG")]
        private static void FailInternal(string message, string exception = null, string callStack = null)
        {
#if DEBUG

            if (callStack == null)
            {
                callStack = GetCurrentCallStack();
            }

            string callLocation = GetFirstUserFrame();

            if (false != ShowFailUI)
            {
                message = message ?? string.Empty;
                message = "Failure: " + message;

                // Are we logging to a file?
                string logFile = Environment.GetEnvironmentVariable("vsassert");
                if (string.IsNullOrEmpty(logFile))
                {
                    ShowFailureInternal(message, callLocation, exception, callStack);
                }
                else
                {
                    using (StreamWriter file = File.AppendText(logFile))
                    {
                        file.WriteLine(message);
                    }
                }
            }
            else
                throw new Exception(message);
#endif
        }

#if DEBUG
        private static HashSet<string> IgnoreTable = new HashSet<string>();

        /// <summary>
        /// A way to override default Debug Fail UI
        /// </summary>
        public static IDebugFailUI DebugFailUI { get; set; }

        [DebuggerStepThrough]
        [DebuggerHidden]
        private static void ShowFailureInternal(string message, string callLocation, string exception, string callStack)
        {
            if (IgnoreTable.Contains(callLocation))
                return;

            message = message ?? string.Empty;

            if (DebugFailUI == null)
                DebugFailUI = new DefaultDebugFailUI();
            DebugUIResult result = DebugUIResult.Ignore;

            try
            {
                result = DebugFailUI.ShowFailure(message, callLocation, exception, callStack);
            }
            catch (InvalidOperationException)
            { }

            switch (result)
            {
                case DebugUIResult.Debug:
                    if (!Debugger.IsAttached)
                    {
                        Debugger.Launch();
                    }
                    Debugger.Break();
                    break;
                case DebugUIResult.EndProcess:
                    // Kill the process
                    Process.GetCurrentProcess().Kill();
                    break;
                case DebugUIResult.IgnoreAll:
                    if (!string.IsNullOrWhiteSpace(callLocation))
                        IgnoreTable.Add(callLocation);
                    break;
            }
        }

        [DebuggerHidden]
        private static string ExceptionToCallStack(Exception ex)
        {
            return ExceptionToCallStack(ex, 0);
        }

        [DebuggerHidden]
        private static string ExceptionToCallStack(Exception ex, int depth = 0)
        {
            StringBuilder sb = new StringBuilder();

            // Runtime Invoke may put the callstack information in the Data dictionary.
            if (ex.Data.Contains("CustomStackTrace"))
            {
                string stackTrace = ex.Data["CustomStackTrace"] as string;
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    sb.Append(ex.GetType().Name); // Name of the exception type
                    sb.AppendLine(" Call Stack (from thread): ");
                    foreach (var line in stackTrace.Split(new string[] { " at " }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string trimLine = line.Trim();
                        if (!string.IsNullOrEmpty(trimLine))
                        {
                            sb.Append("        ");
                            sb.AppendLine(trimLine);
                        }
                    }
                }
            }

            // The stack trace of where the exception was raised (not where the assert was raised)
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                sb.Append(ex.GetType().Name); // Name of the exception type
                sb.AppendLine(" Call Stack: ");
                foreach (var line in ex.StackTrace.Split(new string[] { " at " }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimLine))
                    {
                        sb.AppendLine(trimLine);
                    }
                }
            }

            // Provide more information on common exceptions
            DetailedExceptionHelper(sb, ex, depth, string.Empty);

            // The inner exceptions
            if (ex.InnerException != null)
            {
                string innerCallStack = ExceptionToCallStack(ex.InnerException, depth + 1);
                if (!string.IsNullOrEmpty(innerCallStack))
                {
                    sb.AppendLine();
                    sb.Append(innerCallStack);
                }
            }

            return sb.ToString();
        }

#endif //DEBUG

        [DebuggerHidden]
        public static string ExceptionToString(Exception ex)
        {
            return ExceptionToString(ex, 0, true);
        }

        /// <summary>
        /// Converts an exception to a string that contains more useful
        /// information than the default ex.ToString()
        /// </summary>
        [DebuggerHidden]
        private static string ExceptionToString(Exception ex, int depth, bool showCallStack)
        {
            if (ex == null)
                return string.Empty;

            string padding = new string(' ', depth * 4);

            StringBuilder sb = new StringBuilder();
            sb.Append(padding);
            sb.Append(ex.GetType().Name); // Name of the exception type
            sb.Append(": ");

            // The message
            string message = AddPadding(ex.Message, padding);
            if (!string.IsNullOrEmpty(message))
            {
                sb.AppendLine(message);
            }
            else
            {
                sb.AppendLine();
            }

            if (showCallStack)
            {
                // The stack trace of where the exception was raised (not where the assert was raised)
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    sb.AppendLine();
                    foreach (var line in ex.StackTrace.Split(new string[] { " at " }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string trimLine = line.Trim();
                        if (!string.IsNullOrEmpty(trimLine))
                        {
                            sb.AppendLine(trimLine);
                        }
                    }
                }
            }

            // Provide more information on common exceptions
            DetailedExceptionHelper(sb, ex, depth, padding);

            // The inner exceptions
            if (ex.InnerException != null)
            {
                sb.Append(padding);
                sb.AppendLine("Inner Exception: ");
                sb.Append(ExceptionToString(ex.InnerException, depth + 1, showCallStack: false));
            }

            return sb.ToString();
        }

        // Provide more information on common exceptions
        private static void DetailedExceptionHelper(StringBuilder sb, Exception ex, int depth, string padding)
        {
            // Reflection type loads occur a lot in this code, so give more info
            // on why they failed
            if (ex is ReflectionTypeLoadException)
            {
                foreach (var exLoader in (ex as ReflectionTypeLoadException).LoaderExceptions.Take(5))
                {
                    sb.AppendLine();
                    sb.Append(padding);
                    sb.AppendLine("Loader exception: ");
                    sb.Append(ExceptionToString(exLoader, depth + 1, showCallStack: false));
                }
            }
            else if (ex is FileNotFoundException)
            {
                FileNotFoundException exception = (FileNotFoundException)ex;
                bool hasFileName = !String.IsNullOrEmpty(exception.FileName);
                bool hasFusionLog = false;
#if !CORECLR
                hasFusionLog = !String.IsNullOrEmpty(exception.FusionLog);
#endif
                if (hasFileName || hasFusionLog)
                {
                    sb.AppendLine();
                    if (hasFileName)
                    {
                        sb.Append(padding);
                        sb.Append("File: ");
                        sb.AppendLine(exception.FileName);
                    }
#if !CORECLR
                    if (hasFusionLog)
                    {
                        sb.Append(padding);
                        sb.Append("Fusion log: ");
                        sb.AppendLine(exception.FusionLog);
                    }
#endif
                }
            }
            else if (ex is AggregateException)
            {
                AggregateException exception = (AggregateException)ex;
                if (exception.InnerExceptions != null)
                {
                    foreach (var exInner in exception.InnerExceptions.Take(5))
                    {
                        sb.AppendLine();
                        sb.Append(padding);
                        sb.AppendLine("Aggregate exception: ");
                        sb.Append(ExceptionToString(exInner, depth + 1, showCallStack: false));
                    }
                }
            }
        }

        // Add padding before new lines to make indents consistent
        private static string AddPadding(string message, string padding)
        {
            message = message.Trim();
            // Change tabs to spaces
            message = Regex.Replace(message, "\t", "    ");

            // Handle any kind of new line that is spewed out of an exception
            // Change everything to '\n' first, then back to NewLine (AKA \r\n, AKA CRLF)
            message = Regex.Replace(message, Environment.NewLine, "\n");
            message = Regex.Replace(message, "\r", "\n");
            message = Regex.Replace(message, "\n", Environment.NewLine + padding);
            return message;
        }


#if DEBUG

        static readonly private Type[] ClassesToIgnore = {
            typeof(UDebug),
            typeof(Parameter),
            typeof(DefaultDebugFailUI),
            typeof(Environment)
        };

        [DebuggerHidden]
        private static string GetFirstUserFrame()
        {
            return GetCurrentCallStack(justFirstLine: true);
        }

        [DebuggerHidden]
        private static string GetCurrentCallStack()
        {
            return GetCurrentCallStack(justFirstLine: false);
        }

        [DebuggerHidden]
        private static string GetCurrentCallStack(bool justFirstLine)
        {
            // Use the Environment.StackTrace API to get the stack trace.
            IEnumerable<string> frames = Environment.StackTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (Type ignoreType in ClassesToIgnore)
            {
                frames = frames.Where(x => x.IndexOf(ignoreType.FullName, StringComparison.OrdinalIgnoreCase) < 0);
            }

            // Remove " at " part of string
            frames = frames.Select(x => x.Substring(6).Trim());

            if (justFirstLine)
                return frames.FirstOrDefault() ?? string.Empty;

            return string.Join(Environment.NewLine, frames) ?? string.Empty;
        }

#endif
    }

#if DEBUG
    /// <summary>
    /// Implements the Debug Fail UI for the UDebug class
    /// </summary>
    internal class DefaultDebugFailUI : IDebugFailUI
    {
        DebugUIResult IDebugFailUI.ShowFailure(string message, string callLocation, string exception, string callStack)
        {
            Debug.Fail(exception + ": " + message, callStack);
            return DebugUIResult.Ignore;
        }
    }
#endif //DEBUG
}