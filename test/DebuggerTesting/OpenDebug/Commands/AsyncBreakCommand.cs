// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DebuggerTesting.OpenDebug.Commands
{
    /// <summary>
    /// Causes the debugger to break into code
    /// </summary>
    public class AsyncBreakCommand : Command<ThreadCommandArgs>
    {
        public AsyncBreakCommand()
            : base("pause")
        {
            this.Args.threadId = 0;
        }
    }
}
