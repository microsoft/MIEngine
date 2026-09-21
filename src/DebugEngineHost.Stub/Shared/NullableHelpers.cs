// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

global using static global::Microsoft.DebugEngineHost.NullableHelpers;

namespace Microsoft.DebugEngineHost
{
    using System.Diagnostics.CodeAnalysis;
    using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;
    using SysDebug = System.Diagnostics.Debug;

#pragma warning disable 8763 // A method marked [DoesNotReturn] should not return.

    /// <summary>
    /// Helper class to support nullable reference work when compiling against .NET Standard / .NET Framework
    /// </summary>
    public static class NullableHelpers
    {
        /// <summary>
        /// Wrapper around string.IsNullOrEmpty to add the `[NotNullWhen(false)]` annotation
        /// </summary>
        /// <param name="s">string to test</param>
        /// <returns>True if the string is null or empty</returns>
        static public bool IsNullOrEmpty([NotNullWhen(false)] string? s)
        {
            return string.IsNullOrEmpty(s);
        }

        /// <summary>
        /// Wrapper around string.IsNullOrWhiteSpace to add the `[NotNullWhen(false)]` annotation
        /// </summary>
        /// <param name="s">string to test</param>
        /// <returns>True if the string is null, empty, or only whitespace</returns>
        static public bool IsNullOrWhiteSpace([NotNullWhen(false)] string? s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        /// <summary>
        /// This is a shim on top of the <see cref="System.Diagnostics.Debug"/> class which adds attributes used
        /// in nullability analysis. This is important because without the DoesNotReturnIf/DoesNotReturn attributes,
        /// on Debug.Assert/Debug.Fail the C# compiler will see code like:
        /// <code>
        /// Debug.Assert(myArg != null, "Invalid argument")
        /// </code>
        /// And decide that because the code was attempting to handle 'myArg' being null, that it must be possible
        /// for it to be null.
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCode()]
        public static class Debug
        {
            /// <summary>
            /// Checks for a condition; if the condition is false, displays a message box that shows the call stack.
            /// </summary>
            /// <param name="condition">The conditional expression to evaluate. If the condition is true, a failure message is not sent and the message box is not displayed.</param>
            [Conditional("DEBUG")]
            public static void Assert([DoesNotReturnIf(false)] bool condition)
            {
                SysDebug.Assert(condition);
            }

            /// <summary>
            /// Checks for a condition; if the condition is false, outputs a specified message and displays a message box that shows the call stack.
            /// </summary>
            /// <param name="condition">The conditional expression to evaluate. If the condition is true, the specified message is not sent and the message box is not displayed.</param>
            /// <param name="message">The message to send to the <see cref="System.Diagnostics.Trace.Listeners"/> collection.</param>
            [Conditional("DEBUG")]
            public static void Assert([DoesNotReturnIf(false)] bool condition, string message)
            {
                SysDebug.Assert(condition, message);
            }

            /// <summary>
            /// Checks for a condition; if the condition is false, outputs two specified messages and displays a message box that shows the call stack.
            /// </summary>
            /// <param name="condition">The conditional expression to evaluate. If the condition is true, the specified messages are not sent and the message box is not displayed.</param>
            /// <param name="message">The message to send to the <see cref="System.Diagnostics.Trace.Listeners"/> collection.</param>
            /// <param name="detailMessage">The detailed message to send to the <see cref="System.Diagnostics.Trace.Listeners"/> collection.</param>
            [Conditional("DEBUG")]
            public static void Assert([DoesNotReturnIf(false)] bool condition, string message, string detailMessage)
            {
                SysDebug.Assert(condition, message, detailMessage);
            }

            /// <summary>
            /// Emits the specified error message.
            /// </summary>
            /// <param name="message">A message to emit.</param>
            [Conditional("DEBUG")]
            [DoesNotReturn]
            public static void Fail(string message)
            {
                SysDebug.Fail(message);
            }

            /// <summary>
            /// Emits an error message and a detailed error message.
            /// </summary>
            /// <param name="message">A message to emit.</param>
            /// <param name="detailMessage">A detailed message to emit.</param>
            [Conditional("DEBUG")]
            [DoesNotReturn]
            public static void Fail(string message, string detailMessage)
            {
                SysDebug.Fail(message, detailMessage);
            }

            /// <summary>
            /// Writes a message followed by a line terminator to the debugger.
            /// </summary>
            /// <param name="message">A message to write.</param>
            [Conditional("DEBUG")]
            public static void WriteLine(string message)
            {
                SysDebug.WriteLine(message);
            }

            /// <summary>
            /// Writes the value of the object's <see cref="object.ToString"/> method to the debugger.
            /// </summary>
            /// <param name="value">An object whose value is sent to the debugger.</param>
            [Conditional("DEBUG")]
            public static void WriteLine(object value)
            {
                SysDebug.WriteLine(value);
            }

            /// <summary>
            /// Writes a category name and message to the debugger.
            /// </summary>
            /// <param name="message">A message to write.</param>
            /// <param name="category">A category name used to organize the output.</param>
            [Conditional("DEBUG")]
            public static void WriteLine(string message, string category)
            {
                SysDebug.WriteLine(message, category);
            }

            /// <summary>
            /// Writes a category name and the value of the object's <see cref="object.ToString"/> method to the debugger.
            /// </summary>
            /// <param name="value">An object whose value is sent to the debugger.</param>
            /// <param name="category">A category name used to organize the output.</param>
            [Conditional("DEBUG")]
            public static void WriteLine(object value, string category)
            {
                SysDebug.WriteLine(value, category);
            }
        }
    }
}