// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using DebuggerTesting;
using DebuggerTesting.Compilation;
using Xunit;

namespace CppTests.Tests
{
    internal static class DebuggeeHelper
    {
        private static object s_lock = new object();

        public static IDebuggee OpenAndCompile(ILoggingComponent logger, ICompilerSettings settings, int moniker, string name, string outputname, Action<IDebuggee> addSourceFiles)
        {
            Assert.NotNull(addSourceFiles);
            lock (s_lock)
            {
                IDebuggee debuggee = Debuggee.Create(logger, settings, name, moniker, outputname);
                addSourceFiles(debuggee);
                debuggee.Compile();
                return debuggee;
            }
        }

        public static IDebuggee Open(ILoggingComponent logger, ICompilerSettings settings, int moniker, string name, string outputname)
        {
            lock (s_lock)
            {
                IDebuggee debuggee = Debuggee.Open(logger, settings, name, moniker, outputname);
                Assert.True(File.Exists(debuggee.OutputPath), "The debuggee was not compiled. Missing " + debuggee.OutputPath);
                return debuggee;
            }
        }
    }
}
