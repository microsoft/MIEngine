// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting
{
    public abstract class DebuggerAttribute :
        Attribute
    {
        #region Constructor

        public DebuggerAttribute(SupportedDebugger debugger, SupportedArchitecture architecture)
        {
            this.Debugger = debugger;
            this.Architecture = architecture;
        }

        #endregion

        #region Properties

        public SupportedDebugger Debugger { get; private set; }

        public SupportedArchitecture Architecture { get; private set; }

        #endregion
    }
}
