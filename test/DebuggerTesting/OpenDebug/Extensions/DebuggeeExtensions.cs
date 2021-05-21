// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting.Compilation;
using DebuggerTesting.OpenDebug.Commands;

namespace DebuggerTesting.OpenDebug.Extensions
{
    public static class DebuggeeExtensions
    {
        /// <summary>
        /// Creates a SourceBreakpoints object that can be used to keep
        /// track of all the breakpoints in a file.
        /// </summary>
        public static SourceBreakpoints Breakpoints(this IDebuggee debuggee, string sourceRelativePath, params int[] lineNumbers)
        {
            SourceBreakpoints breakpoints = new SourceBreakpoints(debuggee, sourceRelativePath);
            foreach (int lineNumber in lineNumbers)
                breakpoints.Add(lineNumber);
            return breakpoints;
        }
    }
}
