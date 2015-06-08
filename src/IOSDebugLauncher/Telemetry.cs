// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOSDebugLauncher
{
    /// <summary>
    /// Class for sending telemetry
    /// </summary>
    internal static class Telemetry
    {
        private static bool s_isDisabled;

        private const string Event_LaunchError = @"VS/Diagnostics/Debugger/iOS/LaunchFailure";
        private const string Property_LaunchErrorResult = @"VS.Diagnostics.Debugger.iOS.FailureResult";
        private const string Property_LaunchErrorTarget = @"VS.Diagnostics.Debugger.iOS.Target";

        private const string Event_VcRemoteClientError = @"VS/Diagnostics/Debugger/iOS/VcRemoteClientFailure";
        private const string Property_VcRemoteClientErrorResult = @"VS.Diagnostics.Debugger.iOS.VcRemoteClientFailureResult";


        #region LaunchFailure support

        public enum LaunchFailureCode
        {
            /// <summary>
            /// Indicates that the error shouldn't be sent to telemetry
            /// </summary>
            NoReport,

            /// <summary>
            /// No error, launch is success
            /// </summary>
            LaunchSuccess,

            /// <summary>
            /// Launch failed for unkown reason
            /// </summary>
            LaunchFailure,

            /// <summary>
            /// vcremote is returning json that cannot be parsed correctly
            /// </summary>
            BadJson,

            /// <summary>
            /// vcremote was unable to return a remote path for the given packageID
            /// </summary>
            BadPackageId,
        };

        public enum VcRemoteFailureCode
        {
            /// <summary>
            /// Indicates no error occured when calling vcremote
            /// </summary>
            VcRemoteSucces,

            /// <summary>
            /// Unable to access vcremote due to bad authorization (certificate issue)
            /// </summary>
            VcRemoteUnauthorized,

            /// <summary>
            /// Unable to reach vcremote for some reason
            /// </summary>
            VcRemoteNoConnection,

            /// <summary>
            /// Unknown Error from vcremote
            /// </summary>
            VcRemoteUnkown,
        }

        public static void SendLaunchError(string failureCode, IOSDebugTarget target)
        {
            SendEvent(Event_LaunchError,
                new KeyValuePair<string, object>(Property_LaunchErrorResult, failureCode),
                new KeyValuePair<string, object>(Property_LaunchErrorTarget, target.ToString())
                );
        }

        public static void SendVcRemoteClientError(string failureCode)
        {
            SendEvent(Event_VcRemoteClientError, new KeyValuePair<string, object>(Property_VcRemoteClientErrorResult, failureCode));
        }

        #endregion

        private static void SendEvent(string eventName, params KeyValuePair<string, object>[] eventProperties)
        {
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
        }

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
    }
}
