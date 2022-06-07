// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using DebuggerTesting.Compilation;

namespace DebuggerTesting.OpenDebug.CrossPlatCpp
{
    public static class DebuggerRunnerExtensions
    {
        public static void Launch(this IDebuggerRunner runner, IDebuggerSettings settings, bool stopAtEntry, string program, params string[] args)
        {
            runner.RunCommand(new LaunchCommand(settings, program, null, false, args) { StopAtEntry = stopAtEntry });
        }

        public static void Launch(this IDebuggerRunner runner, IDebuggerSettings settings, bool stopAtEntry, IDebuggee debuggee, params string[] args)
        {
            runner.Launch(settings, stopAtEntry, debuggee.OutputPath, args);
        }

        public static void Launch(this IDebuggerRunner runner, IDebuggerSettings settings, IDebuggee debuggee, params string[] args)
        {
            runner.Launch(settings, false, debuggee.OutputPath, args);
        }

        public static void Attach(this IDebuggerRunner runner, IDebuggerSettings settings, Process process)
        {
            runner.RunCommand(new AttachCommand(settings, process));
        }

        public static void LaunchCoreDump(this IDebuggerRunner runner, IDebuggerSettings settings, IDebuggee debuggee, string coreDumpPath)
        {
            runner.RunCommand(new LaunchCommand(settings, debuggee.OutputPath, coreDumpPath));
        }
    }
}
