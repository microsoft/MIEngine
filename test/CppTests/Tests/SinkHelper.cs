// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using DebuggerTesting;
using DebuggerTesting.Compilation;
using Xunit;

namespace CppTests.Tests
{
    /// <summary>
    /// Helpers to use the kitchen sink debuggee
    /// </summary>
    internal static class SinkHelper
    {
        public const string Main = "main.cpp";
        public const string Arguments = "arguments.cpp";
        public const string Calling = "calling.cpp";
        public const string Feature = "feature.cpp";
        public const string Threading = "threading.cpp";
        public const string NonTerminating = "nonterminating.cpp";
        public const string Expression = "expression.cpp";
        public const string Environment = "environment.cpp";

        private const string Name = "kitchensink";
        private const string OutputName = "sink";

        public static IDebuggee Open(ILoggingComponent logger, ICompilerSettings settings, int moniker)
        {
            return DebuggeeHelper.Open(logger, settings, moniker, SinkHelper.Name, SinkHelper.OutputName);
        }

        public static IDebuggee OpenAndCompile(ILoggingComponent logger, ICompilerSettings settings, int moniker)
        {
            return DebuggeeHelper.OpenAndCompile(logger, settings, moniker, SinkHelper.Name, SinkHelper.OutputName, SinkHelper.AddSourceFiles);
        }

        private static void AddSourceFiles(IDebuggee debuggee)
        {
            // Add a source files, specify type, compile
            debuggee.AddSourceFiles(
                SinkHelper.Main,
                SinkHelper.Arguments,
                SinkHelper.Calling,
                SinkHelper.Environment,
                SinkHelper.Feature,
                SinkHelper.Threading,
                SinkHelper.NonTerminating,
                SinkHelper.Expression);
            debuggee.CompilerOptions |= CompilerOption.SupportThreading;
        }
    }
}
