// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SSHDebugPS.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;
using static Microsoft.VisualStudio.Shell.VsTaskLibraryHelper;
using System.Linq;

namespace Microsoft.SSHDebugPS.WSL
{
    internal static class WSLCommandLine
    {
        static string s_exePath;
        public static string ExePath
        {
            get
            {
                if (s_exePath == null)
                    throw new InvalidOperationException();

                return s_exePath;
            }
        }

        public static void EnsureInitialized()
        {
            if (s_exePath == null)
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), Environment.Is64BitProcess ? "System32" : "sysnative", "wsl.exe");
                if (!File.Exists(path))
                {
                    throw new WSLException(StringResources.Error_WSLNotInstalled);
                }

                s_exePath = path;
            }
        }

        public static ProcessStartInfo GetExecStartInfo(string distroName, string commandText)
        {
            return GetWSLStartInfo(GetExecCommandLineArgs(distroName, commandText), Encoding.UTF8);
        }

        public static string GetExecCommandLineArgs(string distroName, string commandText)
        {
            return FormattableString.Invariant($"-d {distroName} -- {commandText}");
        }

        public static IEnumerable<string> GetInstalledDistros()
        {
            HashSet<string> distributions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ThreadHelper.JoinableTaskFactory.Run(StringResources.WaitingOp_EnumeratingWSLDistros, async (progress, cancellationToken) =>
            {
                ProcessStartInfo startInfo = GetWSLStartInfo("-l -v", Encoding.Unicode);

                ProcessResult processResult = await LocalProcessAsyncRunner.ExecuteProcessAsync(startInfo, cancellationToken);
                if (processResult.ExitCode != 0)
                {
                    const int NoDistrosExitCode = -1;
                    if (processResult.ExitCode == NoDistrosExitCode)
                    {
                        // Older versions of wsl don't like the '-v' and will also fail with -1 for that reason. Check if this is why we are seeing the failure
                        ProcessResult retryResult = await LocalProcessAsyncRunner.ExecuteProcessAsync(GetWSLStartInfo("-l", Encoding.Unicode), cancellationToken);

                        // If the exit code is still NoDistros then they really don't have any distros
                        if (retryResult.ExitCode == NoDistrosExitCode)
                        {
                            throw new WSLException(StringResources.Error_WSLNoDistros);
                        }
                        // For any other code, they don't have WSL 2 installed.
                        else
                        {
                            throw new WSLException(StringResources.WSL_V2Required);
                        }
                    }
                    else
                    {
                        if (processResult.StdErr.Count > 0)
                        {
                            VsOutputWindowWrapper.WriteLine(StringResources.Error_WSLExecErrorOut_Args2.FormatCurrentCultureWithArgs(startInfo.FileName, startInfo.Arguments));
                            foreach (string line in processResult.StdErr)
                            {
                                VsOutputWindowWrapper.WriteLine("\t" + line);
                            }
                        }

                        throw new WSLException(StringResources.Error_WSLEnumDistrosFailed_Args1.FormatCurrentCultureWithArgs(processResult.ExitCode));
                    }
                }

                // Parse the installed distributions
                /* Ouput looks like:
                    NAME                   STATE           VERSION
                * Ubuntu                 Stopped         2
                    docker-desktop-data    Running         2
                    docker-desktop         Running         2
                */
                Regex distributionRegex = new Regex(@"^\*?\s+(?<name>\S+)\s");
                foreach (string line in processResult.StdOut.Skip(1))
                {
                    Match match = distributionRegex.Match(line);
                    if (match.Success)
                    {
                        string distroName = match.Groups["name"].Value;
                        if (distroName.StartsWith("docker-desktop", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        distributions.Add(distroName);
                    }
                }
            });
            if (distributions.Count == 0)
            {
                throw new WSLException(StringResources.Error_WSLNoDistros);
            }

            return distributions;
        }

        private static ProcessStartInfo GetWSLStartInfo(string args, Encoding encoding)
        {
            return new ProcessStartInfo(s_exePath, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = encoding,
                StandardErrorEncoding = encoding
            };
        }
    }
}