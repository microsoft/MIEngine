// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DebuggerTesting.OpenDebug.Commands.Responses
{
    public sealed class DataBreakpointsInfoResponseValue : CommandResponseValue
    {
        public sealed class Body
        {
            public string dataId;
            public string description;
            public string[] accessTypes;
            public bool canPersist;
        }
        public Body body = new Body();
    }
}
