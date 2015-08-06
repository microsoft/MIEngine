// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidDebugLauncher
{
    internal static class PwdOutputParser
    {
        public static string ExtractWorkingDirectory(string commandOutput, string packageName)
        {
            IEnumerable<string> allLines = GetLines(commandOutput);

            // Linux will allow just about anything in a directory name as long as it is excaped. Android is much
            // more picky about package names. Let's reject characters which are invalid in a package name, highly
            // unlikely that any Android distribution would decide to use in the base directory, and likely to show
            // up in any debug spew we should ignore.
            char[] invalidPackageNameChars = { ' ', '\t', '*', '[', ']', '(', ')', '{', '}', ':' };

            // run-as is giving debug spew on a Galaxy S6, so we need to look at all the lines, and find the one that could be the working directory
            IEnumerable<string> workingDirectoryLines = allLines.Where(
                line => line.Length > 0 &&
                line[0] == '/' &&
                line.IndexOfAny(invalidPackageNameChars) < 0
                );
            if (workingDirectoryLines.Count() == 1)
            {
                return workingDirectoryLines.Single();
            }

            // Handle run-as errors. We will get into this code path if the supplied package name is wrong.
            // Example commandOutput: "run-as: Package 'com.bogus.hellojni' is unknown"
            string runAsLine = allLines.Where(line => line.StartsWith("run-as:", StringComparison.Ordinal))
                .FirstOrDefault();

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

                    throw new LauncherException(telemetryCode, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_ShellCommandFailed, "run-as", errorMessage));
                }
            }

            throw new LauncherException(Telemetry.LaunchFailureCode.BadPwdOutput, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_ShellCommandBadResults, "pwd"));
        }

        private static IEnumerable<string> GetLines(string content)
        {
            using (var reader = new StringReader(content))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;

                    yield return line;
                }
            }
        }
    }
}
