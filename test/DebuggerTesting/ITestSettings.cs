// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DebuggerTesting
{
    /// <summary>
    /// Interface describing the settings that are passed into a theory test.
    /// </summary>
    public interface ITestSettings
    {
        #region Properties

        string Name { get; }

        ICompilerSettings CompilerSettings { get; }

        IDebuggerSettings DebuggerSettings { get; }

        #endregion
    }
}