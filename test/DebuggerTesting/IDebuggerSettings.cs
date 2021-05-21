// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace DebuggerTesting
{
    /// <summary>
    /// Interface describing the settings used to debug a debuggee.
    /// </summary>
    public interface IDebuggerSettings
    {
        #region Properties

        SupportedArchitecture DebuggeeArchitecture { get; }

        string DebuggerName { get; }

        SupportedDebugger DebuggerType { get; }

        string DebuggerPath { get; }

        string DebuggerAdapterPath { get; }

        string MIMode { get; }

        IDictionary<string, string> Properties { get; }

        #endregion
    }
}
