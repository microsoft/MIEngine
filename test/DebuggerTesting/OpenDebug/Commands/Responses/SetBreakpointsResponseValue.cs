// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DebuggerTesting.OpenDebug.Commands.Responses
{
    public sealed class SetBreakpointsResponseValue : CommandResponseValue
    {
        public sealed class Body
        {
            public sealed class Breakpoint
            {
                public int? id;
                public bool? verified;
                public int? line;
                public string message;
            }
            public Breakpoint[] breakpoints;
        }
        public Body body = new Body();
    }
}
