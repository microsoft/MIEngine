// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.DebugEngineHost.VSCode
{
    sealed public class ExceptionSettings
    {
        public enum TriggerState
        {
            unhandled = enum_EXCEPTION_STATE.EXCEPTION_STOP_SECOND_CHANCE,
            userUnhandled = enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_UNCAUGHT,
            thrown = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE | enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_FIRST_CHANCE,
            all = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE | enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_FIRST_CHANCE |
                enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_UNCAUGHT | enum_EXCEPTION_STATE.EXCEPTION_STOP_SECOND_CHANCE
        };

        sealed public class CategoryConfiguration
        {
            [JsonRequired]
            public string Name;

            [JsonRequired]
            public Guid Id;

            public Dictionary<string, TriggerState> DefaultTriggers = new Dictionary<string, TriggerState>();
        }


        private IList<CategoryConfiguration> _categories = new List<CategoryConfiguration>();
        private IList<ExceptionBreakpointFilter> _exceptionFilters = new List<ExceptionBreakpointFilter>();

        public IList<CategoryConfiguration> Categories
        {
            get { return _categories; }
        }

        /// <summary>
        /// /*OPTIONAL*/ Set of exception breakpoints that appears in VS Code
        /// </summary>
        public IList<ExceptionBreakpointFilter> ExceptionBreakpointFilters
        {
            get { return _exceptionFilters; }
        }

        internal ExceptionSettings()
        {
        }

        internal void MakeReadOnly()
        {
            _categories = new ReadOnlyCollection<CategoryConfiguration>(_categories);
            _exceptionFilters = new ReadOnlyCollection<ExceptionBreakpointFilter>(_exceptionFilters);
        }
    }
}