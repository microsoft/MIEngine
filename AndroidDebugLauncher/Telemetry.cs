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

namespace AndroidDebugLauncher
{
    /// <summary>
    /// Class for sending telemetry
    /// </summary>
    internal static class Telemetry
    {
        private static bool s_isDisabled;

        private const string Event_LaunchError = @"VS/Diagnostics/Debugger/Android/LaunchFailure";
        private const string Property_LaunchErrorResult = @"VS.Diagnostics.Debugger.Android.FailureResult";

        #region LaunchFailure support

        public enum LaunchFailureCode
        {
            /// <summary>
            /// Indicates that the error shouldn't be sent to telemetry
            /// </summary>
            NoReport,

            /// <summary>
            /// run-as returned 'Package 'bla' is unknown'
            /// </summary>
            RunAsPackageUnknown,

            /// <summary>
            /// run-as failed in some other way
            /// </summary>
            RunAsFailure,

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
        public static void SendLaunchError(string launchErrorTelemetryResult)
        {
            SendEvent(Event_LaunchError, new KeyValuePair<string, object>(Property_LaunchErrorResult, launchErrorTelemetryResult));
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
