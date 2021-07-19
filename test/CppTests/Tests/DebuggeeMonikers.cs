// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace CppTests.Tests
{
    /// <summary>
    /// To make it easier to work with debuggees, each test works with a copy of the original debuggee.
    /// This allows tests to compile with different options, remove symbols or modify source in ways that
    /// would effect other tests.
    /// The debuggee will be copied into a folder appended with the moniker.
    /// </summary>
    internal static class DebuggeeMonikers
    {
        internal static class HelloWorld
        {
            public const int Sample = 1;
        }

        internal static class KitchenSink
        {
            public const int Attach = 1;
            public const int Threading = 2;
            public const int Breakpoint = 3;
            public const int Execution = 4;
            public const int Expression = 5;
            public const int Environment = 6;
        }

        internal static class SharedLib
        {
            public const int Default = 1;
            public const int MismatchedSource = 2;
        }

        internal static class CoreDump
        {
            public const int Default = 1;
            public const int MismatchedSource = 2;
            public const int Action = 3;
        }

        internal static class Exception
        {
            public const int Default = 1;
        }

        internal static class Optimization
        {
            public const int OptimizationWithSymbols = 1;
            public const int OptimizationWithoutSymbols = 2;
        }

        internal static class SourceMapping
        {
            public const int Default = 1;
        }
    }
}
