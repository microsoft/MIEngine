// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting
{
    /// <summary>
    /// Attribute used by xUnit theory test for specifying which compilers and debuggee architectures
    /// are required by the test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class SupportedCompilerAttribute :
        Attribute
    {
        #region Constructor

        public SupportedCompilerAttribute(SupportedCompiler compiler, SupportedArchitecture debuggeeArchitecture)
        {
            this.Compiler = compiler;
            this.Architecture = debuggeeArchitecture;
        }

        #endregion

        #region Properties

        public SupportedCompiler Compiler { get; private set; }

        public SupportedArchitecture Architecture { get; private set; }

        #endregion
    }
}