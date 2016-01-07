// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DebugEngineHost;
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
            HostTelemetry.SendEvent(Event_LaunchError,
                new KeyValuePair<string, object>(Property_LaunchErrorResult, failureCode),
                new KeyValuePair<string, object>(Property_LaunchErrorTarget, target.ToString())
                );
        }

        public static void SendVcRemoteClientError(string failureCode)
        {
            HostTelemetry.SendEvent(Event_VcRemoteClientError, new KeyValuePair<string, object>(Property_VcRemoteClientErrorResult, failureCode));
        }

        #endregion
    }
}
