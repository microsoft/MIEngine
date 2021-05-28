// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting
{
    /// <summary>
    /// Attribute used by xUnit theory test for specifying which debuggers are required by the test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class SupportedDebuggerAttribute :
        DebuggerAttribute
    {
        #region Constructor

        public SupportedDebuggerAttribute(SupportedDebugger debugger, SupportedArchitecture architecture)
            : base(debugger, architecture)
        {
        }

        #endregion
    }
}