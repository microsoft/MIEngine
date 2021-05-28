// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace DebugAdapterRunner
{
    /// <summary>Base test script command</summary>
    /// <remarks>Commands that are sent to the debug adapters can use DebugAdapterCommand.
    /// Custom commands can be derived from this base class</remarks>
    public class Command
    {
        public string Name;

        public List<DebugAdapterResponse> ExpectedResponses = new List<DebugAdapterResponse>();

        public virtual void Run(DebugAdapterRunner runner)
        {
        }
    }
}
