// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebuggerTesting.OpenDebug.Commands.Responses;
using Newtonsoft.Json;

namespace DebuggerTesting.OpenDebug.Commands
{
    public sealed class StackTraceArgs : ThreadCommandArgs
    {
        public int levels;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? startFrame;
    }

    public class StackTraceCommand : CommandWithResponse<StackTraceArgs, StackTraceResponseValue>
    {
        public StackTraceCommand(int threadId, int? startFrame = null)
            : base("stackTrace")
        {
            this.Args.threadId = threadId;
            this.Args.levels = 20;
            this.Args.startFrame = startFrame;
        }
    }
}
