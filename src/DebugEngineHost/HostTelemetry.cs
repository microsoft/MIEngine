// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Globalization;
using Microsoft.Internal.VisualStudio.Shell;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Static class providing telemetry reporting services to debug engines. Telemetry 
    /// reports go to Microsoft, and so in general this functionality should not be used 
    /// by non-Microsoft implemented debug engines.
    /// </summary>
    public static class HostTelemetry
    {
#if LAB
        private static bool s_isDisabled;
#endif

        /// <summary>
        /// Reports a telemetry event to Microsoft.
        /// </summary>
        /// <param name="eventName">Name of the event. This should generally start with the 
        /// prefix 'VS/Diagnostics/Debugger/'</param>
        /// <param name="eventProperties">0 or more properties of the event. Property names 
        /// should generally start with the prefix 'VS.Diagnostics.Debugger.'</param>
        [Conditional("LAB")]
        public static void SendEvent(string eventName, params KeyValuePair<string, object>[] eventProperties)
        {
#if LAB
            if (s_isDisabled)
                return;

            try
            {
                Internal.SendEvent(eventName, eventProperties);
            }
            catch
            {
                // disable telemetry in the future so that we don't keep failing if types are unavailable
                s_isDisabled = true;
            }
#endif
        }

        /// <summary>
        /// Reports the current exception to Microsoft's telemetry service. 
        /// 
        /// *NOTE*: This should only be called from a 'catch(...) when' handler.
        /// </summary>
        /// <param name="currentException">Exception object to report.</param>
        /// <param name="engineName">Name of the engine reporting the exception. Ex:Microsoft.MIEngine</param>
        public static void ReportCurrentException(Exception currentException, string engineName)
        {
            Debug.Fail(string.Format(CultureInfo.InvariantCulture, "{0} was raised and would normally be reported to telemetry.\n\nStack trace: {1}", currentException.GetType(), currentException.StackTrace));

#if LAB
            VisualStudio.Debugger.DkmComponentManager.ReportCurrentNonFatalException(currentException, engineName);
#endif
        }

#if LAB
        /// <summary>
        /// Internal class used to reference shell types. This is needed to allow our code to safely reference 14.0 types
        /// while still allowing us to run in glass or in Visual Studio 12.0. Any calls in this class need to be guarded
        /// by a try/catch
        /// </summary>
        private static class Internal
        {
            internal static void SendEvent(string eventName, params KeyValuePair<string, object>[] eventProperties)
            {
                var telemetryEvent = TelemetryHelper.TelemetryService.CreateEvent(eventName);
                foreach (var property in eventProperties)
                {
                    telemetryEvent.SetProperty(property.Key, property.Value);
                }
                TelemetryHelper.DefaultTelemetrySession.PostEvent(telemetryEvent);
            }
        }
#endif
    }
}
