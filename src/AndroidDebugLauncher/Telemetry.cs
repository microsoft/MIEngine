// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using Microsoft.DebugEngineHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidDebugLauncher
{
    /// <summary>
    /// Class for sending telemetry
    /// </summary>
    internal static class Telemetry
    {
        private const string Event_LaunchError = @"VS/Diagnostics/Debugger/Android/LaunchFailure";
        private const string Property_LaunchErrorResult = @"VS.Diagnostics.Debugger.Android.FailureResult";
        private const string Property_LaunchTargetEngine = @"VS.Diagnostics.Debugger.Android.TargetEngine";

        #region LaunchFailure support

        public enum LaunchFailureCode
        {
            /// <summary>
            /// Indicates that the error shouldn't be sent to telemetry
            /// </summary>
            NoReport,

            /// <summary>
            /// run-as returned 'Package 'package' is unknown'
            /// </summary>
            RunAsPackageUnknown,

            /// <summary>
            /// run-as failed in some other way
            /// </summary>
            RunAsFailure,

            /// <summary>
            /// run-as returned 'Package 'package' is not debuggable'
            /// </summary>
            RunAsPackageNotDebuggable,

            /// <summary>
            /// We didn't understand the pwd output, but it doesn't look like a run-as failure.
            /// </summary>
            BadPwdOutput,

            /// <summary>
            /// Output from 'ps' couldn't be processed
            /// </summary>
            BadPsOutput,

            /// <summary>
            /// NDK or SDK registry key is missing
            /// </summary>
            DirectoryFromRegistryFailure,

            /// <summary>
            /// Android SDK path is bogus
            /// </summary>
            InvalidAndroidSDK,
            DeviceNotResponding,
            NoGdbServer,
            GDBServerFailed,
            ActivityManagerFailed,
            MultipleApplicationProcesses,
            PackageDidNotStart,
            BadAndroidVersionFormat,

            /// <summary>
            /// AdbShell.Exec failed. I don't think this will ever happen, as the server side of adb eats failures.
            /// </summary>
            AdbShellFailed,
            BadDeviceAbi,
            UnsupportedAndroidVersion,
            DeviceOffline,
        };

        /// <summary>
        /// Obtains the string which should be reported to telemetry for an exception
        /// </summary>
        /// <param name="exception">[Required] Exception that occurred</param>
        /// <returns>[Optional] result to report, null if nothing should be reported</returns>
        public static string GetLaunchErrorResultValue(Exception exception)
        {
            LauncherException @this = exception as LauncherException;
            if (@this != null)
            {
                if (@this.TelemetryCode == LaunchFailureCode.NoReport)
                {
                    return null;
                }
                else
                {
                    return @this.TelemetryCode.ToString();
                }
            }
            else
            {
                return exception.GetType().FullName;
            }
        }
        public static void SendLaunchError(string launchErrorTelemetryResult, string targetEngine)
        {
            HostTelemetry.SendEvent(Event_LaunchError,
                new KeyValuePair<string, object>(Property_LaunchErrorResult, launchErrorTelemetryResult),
                new KeyValuePair<string, object>(Property_LaunchTargetEngine, targetEngine));
        }

        #endregion
    }
}
