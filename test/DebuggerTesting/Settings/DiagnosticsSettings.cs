// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting.Settings
{
    public static class DiagnosticsSettings
    {
        private static bool? logMIEngine;

        /// <summary>
        /// Set to true to log mi engine output
        /// </summary>
        public static bool LogMIEngine
        {
            get
            {
                // Allow setting to be overridden for dev environments
                if (DiagnosticsSettings.logMIEngine == null)
                    DiagnosticsSettings.logMIEngine = Environment.GetEnvironmentVariable("TEST_LOGMIENGINE").ToBool();

                if (DiagnosticsSettings.logMIEngine == null)
                    DiagnosticsSettings.logMIEngine = true;

                return logMIEngine.Value;
            }
        }

        private static bool? logDebugAdapter;

        /// <summary>
        /// Set to true to log debug adapter output
        /// </summary>
        public static bool LogDebugAdapter
        {
            get
            {
                // Allow setting to be overridden for dev environments
                if (DiagnosticsSettings.logDebugAdapter == null)
                    DiagnosticsSettings.logDebugAdapter = Environment.GetEnvironmentVariable("TEST_LOGDEBUGADAPTER").ToBool();

                if (DiagnosticsSettings.logDebugAdapter == null)
                    DiagnosticsSettings.logDebugAdapter = true;

                return DiagnosticsSettings.logDebugAdapter.Value;
            }
        }
    }
}
