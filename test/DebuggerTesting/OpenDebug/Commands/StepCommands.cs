// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting.OpenDebug.Commands
{
    public class ThreadCommandArgs : JsonValue
    {
        public int threadId;
    }

    public class ContinueCommand : Command<ThreadCommandArgs>
    {
        public ContinueCommand(int threadId)
            : base("continue")
        {
            this.Args.threadId = threadId;
            this.Timeout = TimeSpan.FromSeconds(10);
        }
    }

    public class StepInCommand : Command<ThreadCommandArgs>
    {
        public StepInCommand(int threadId)
            : base("stepIn")
        {
            this.Args.threadId = threadId;
            this.Timeout = TimeSpan.FromSeconds(10);
        }
    }

    public class StepOutCommand : Command<ThreadCommandArgs>
    {
        public StepOutCommand(int threadId)
            : base("stepOut")
        {
            this.Args.threadId = threadId;
            this.Timeout = TimeSpan.FromSeconds(10);
        }
    }

    public class StepOverCommand : Command<ThreadCommandArgs>
    {
        public StepOverCommand(int threadId)
            : base("next")
        {
            this.Args.threadId = threadId;
            this.Timeout = TimeSpan.FromSeconds(10);
        }
    }
}
