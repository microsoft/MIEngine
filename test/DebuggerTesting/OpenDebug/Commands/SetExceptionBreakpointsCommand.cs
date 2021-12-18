// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DebuggerTesting.OpenDebug.Commands
{
    public sealed class ExceptionFilterOptions : JsonValue
    {
        public string filterId;
        public string condition;
    }

    public sealed class SetExceptionBreakpointsCommandArgs : JsonValue
    {
        public string[] filters;
        public ExceptionFilterOptions[] filterOptions;
    }

    public class SetExceptionBreakpointsCommand : Command<SetExceptionBreakpointsCommandArgs>
    {
        public const string FilterUserUnhandler = "user-unhandled";
        public const string FilterAll = "all";

        public SetExceptionBreakpointsCommand(string[] filters, ExceptionFilterOptions[] filterOptions)
           : base("setExceptionBreakpoints")
        {
            if (filters != null)
            {
                this.Args.filters = (filters.Length > 0) ? filters : new string[] { FilterUserUnhandler };
            }
            this.Args.filterOptions = filterOptions;
        }
    }
}
