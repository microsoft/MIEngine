// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace Microsoft.DebugEngineHost.VSCode
{
    /// <summary>
    /// Represents a breakpoint that appears in VS Code to enable or disable stopping on
    /// a set of exceptions.
    /// </summary>
    sealed public class ExceptionBreakpointFilter
    {
        private string _filter;

        /// <summary>
        /// The label for the button that will appear in the UI
        /// </summary>
        [JsonRequired]
        public string label { get; set; }

        /// <summary>
        /// The identifier for this filter.
        /// </summary>
        [JsonRequired]
        public string filter
        {
            get
            {
                return _filter;
            }

            set
            {
                _filter = value;
            }
        }

        [JsonRequired]
        public bool supportsCondition { get; set; }

        [JsonRequired]
        public string conditionDescription { get; set; }

        /// <summary>
        /// The default state for the button
        /// </summary>
        public bool @default { get; set; }

        public Guid categoryId { get; set; }

        [JsonIgnore]
        public enum_EXCEPTION_STATE State { get; set; } = enum_EXCEPTION_STATE.EXCEPTION_STOP_SECOND_CHANCE | enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE | enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_FIRST_CHANCE;

    }
}