// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DebuggerTesting.OpenDebug.Commands
{
    public sealed class SetExceptionBreakpointsCommandArgs : JsonValue
    {
        public string[] filters;
    }

    public class SetExceptionBreakpointsCommand : Command<SetExceptionBreakpointsCommandArgs>
    {
        public const string FilterUserUnhandler = "user-unhandled";
        public const string FilterAll = "all";

        public SetExceptionBreakpointsCommand(params string[] filters)
           : base("setExceptionBreakpoints")
        {
            this.Args.filters = (filters.Length > 0) ? filters : new string[] { FilterUserUnhandler };
        }
    }
}
