// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DebuggerTesting.OpenDebug.Commands
{
    public abstract class AttachCommand<T> : Command<T>
        where T : LaunchCommandArgs, new()
    {
        public AttachCommand() 
            : base("attach")
        {
        }
    }
}