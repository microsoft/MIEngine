// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AndroidDebugLauncher
{
    internal static class RunAsOutputParser
    {
        /// <summary>
        /// Handle run-as errors. We will get into this code path if the supplied package name is wrong.
        /// </summary>
        /// <param name="commandOutput">Output of the run-as command executed through adb shell.
        /// Example "run-as: Package 'com.bogus.hellojni' is unknown"</param>
        /// <param name="packageName">Package name used as the first argument to run-as</param>
        public static void ThrowIfRunAsErrors(string commandOutput, string packageName)
        {
            IEnumerable<string> allLines = commandOutput.GetLines();
            string runAsLine = allLines.FirstOrDefault(line => line.StartsWith("run-as:", StringComparison.Ordinal));

            if (runAsLine != null)
            {
                string errorMessage = runAsLine.Substring("run-as:".Length).Trim();
                if (errorMessage.Length > 0)
                {
                    if (!char.IsPunctuation(errorMessage[errorMessage.Length - 1]))
                    {
                        errorMessage = string.Concat(errorMessage, ".");
                    }

                    Telemetry.LaunchFailureCode telemetryCode = Telemetry.LaunchFailureCode.RunAsFailure;

                    if (errorMessage == string.Format(CultureInfo.InvariantCulture, "Package '{0}' is unknown.", packageName))
                    {
                        telemetryCode = Telemetry.LaunchFailureCode.RunAsPackageUnknown;
                        errorMessage = string.Concat(errorMessage, "\r\n\r\n", LauncherResources.Error_RunAsUnknownPackage);
                    }

                    else if (errorMessage == string.Format(CultureInfo.InvariantCulture, "Package '{0}' is not debuggable.", packageName))
                    {
                        telemetryCode = Telemetry.LaunchFailureCode.RunAsPackageNotDebuggable;
                        errorMessage = string.Concat(errorMessage, "\r\n\r\n", LauncherResources.Error_RunAsNonDebuggablePackage);
                    }

                    throw new LauncherException(telemetryCode, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_ShellCommandFailed, "run-as", errorMessage));
                }
            }
        }
    }
}
