// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting.OpenDebug.Commands
{
    /// <summary>
    /// Command used to start the debuggee after initial breakpoint and configuration
    /// commands have been issued.
    /// </summary>
    public class ConfigurationDoneCommand : Command<object>
    {
        public ConfigurationDoneCommand() : base("configurationDone")
        {
            this.Timeout = TimeSpan.FromSeconds(10);
        }
    }
}
