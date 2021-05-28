// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace DebuggerTesting.Compilation
{
    public interface IDebuggee
    {
        #region Methods

        void AddDefineConstant(string name, string value = null);

        void AddLibraries(params string[] libraries);

        void AddSourceFiles(params string[] fileNames);

        IDebuggee Clone();

        void Compile();

        Process Launch(params string[] arguments);

        #endregion

        #region Properties

        CompilerOption CompilerOptions { get; set; }

        string OutputPath { get; }

        CompilerOutputType OutputType { get; }

        string SourceRoot { get; }

        #endregion
    }
}