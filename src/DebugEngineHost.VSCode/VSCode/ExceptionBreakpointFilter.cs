// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public const string Filter_All = "all";
        public const string Filter_UserUnhandled = "user-unhandled";

        private string _filter;

        /// <summary>
        /// The label for the button that will appear in the UI
        /// </summary>
        [JsonRequired]
        public string label { get; set; }

        /// <summary>
        /// The identifier for this filter. Currently this should be 'all' or 'user-unhandled'
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
                if (value != Filter_All && value != Filter_UserUnhandled)
                {
                    Debug.Fail("Invalid ExceptionBreakpointFilter");
                    throw new ArgumentOutOfRangeException("filter");
                }
                _filter = value;
            }
        }

        /// <summary>
        /// The default state for the button
        /// </summary>
        public bool @default { get; set; }
    }
}