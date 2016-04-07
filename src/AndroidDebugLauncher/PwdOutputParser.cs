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
            IEnumerable<string> allLines = commandOutput.GetLines();

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

            RunAsOutputParser.ThrowIfRunAsErrors(commandOutput, packageName);
            throw new LauncherException(Telemetry.LaunchFailureCode.BadPwdOutput, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_ShellCommandBadResults, "pwd"));
        }
    }
}
