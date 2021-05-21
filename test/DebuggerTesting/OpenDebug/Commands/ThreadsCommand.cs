// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting.OpenDebug.Commands.Responses;

namespace DebuggerTesting.OpenDebug.Commands
{
    public class ThreadsCommand : CommandWithResponse<object, ThreadsResponseValue>
    {
        public ThreadsCommand() : base("threads")
        {
        }
    }
}
