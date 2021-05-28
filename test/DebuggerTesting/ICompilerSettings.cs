// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace DebuggerTesting
{
    /// <summary>
    /// Interface describing the settings used to compile a debuggee.
    /// </summary>
    public interface ICompilerSettings
    {
        #region Properties

        string CompilerName { get; }

        SupportedCompiler CompilerType { get; }

        string CompilerPath { get; }

        SupportedArchitecture DebuggeeArchitecture { get; }

        IDictionary<string, string> Properties { get; }

        #endregion
    }
}