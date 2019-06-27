// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Server;

namespace Microsoft.SSHDebugPS
{
    public class ProcFSOutputParser
    {
        // for process user, can also use 'stat -c %U /proc/<pid>/exe'
        public static string CommandText => @"echo shell-process:$$; for filename in /proc/[0-9]*; do echo $filename,cmdline:$(tr '\0' ' ' < $filename/cmdline 2>/dev/null),ls:$(ls -lh $filename/exe 2>/dev/null); done";
        public static string EscapedCommandText => CommandText.Replace("$", "\\$");
        private const string ShellProcessPrefix = "shell-process:";
        private readonly Regex _linePattern = new Regex(@"^/proc/([0-9]+),cmdline:(.*),ls:(.*)$", RegexOptions.None);

        private List<Process> _processList = new List<Process>();
        private uint _shellProcess = uint.MaxValue;

        public static List<Process> Parse(string output, string username)
        {
            return new ProcFSOutputParser().ParseInternal(output, username);
        }

        private ProcFSOutputParser()
        { }

        private List<Process> ParseInternal(string output, string username)
        {
            using (var reader = new StringReader(output))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;

                    ProcessLine(line.Trim(), username);
                }

                if (_processList.Count == 0)
                {
                    throw new CommandFailedException(StringResources.Error_PSFailed);
                }

                _processList.Sort(
                    (x, y) => x.Id < y.Id ? -1
                            : x.Id == y.Id ? 0
                            : 1);
            }

            return _processList;
        }

        private void ProcessLine(string line, string username)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            if (line.StartsWith(ShellProcessPrefix))
            {
                if (line.Length > ShellProcessPrefix.Length)
                {
                    uint.TryParse(line.Substring(ShellProcessPrefix.Length).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out _shellProcess);
                }
                return;
            }

            Match match = _linePattern.Match(line);
            if (match == null || !match.Success)
            {
                Debug.Fail("Unexpected output text from ps script");
                return;
            }

            string processIdAsString = match.Groups[1].Value;
            string commandLine = match.Groups[2].Value;
            string[] lsColumns = match.Groups[3].Value.Split(' ', '\t');

            if (!uint.TryParse(processIdAsString, NumberStyles.None, CultureInfo.InvariantCulture, out uint processId))
            {
                Debug.Fail("Unexpected process ID larger than 2^32");
                return;
            }

            if (processId == _shellProcess)
                return; // ignore the shell process

            // Example ls output: lrwxrwxrwx 1 root root 0 Apr 27 17:51 /proc/7/exe
            string procUsername = lsColumns.Length >= 5 ? lsColumns[2] : string.Empty;

            if (commandLine.Length == 0)
            {
                // If we didn't have access to /proc/<PID>/cmdline, use placeholder text
                commandLine = StringResources.ProcessName_Unknown;
            }

            // If the passed in username is empty, then treat all processes as the same user
            bool isSameUser = string.IsNullOrWhiteSpace(username) ? true : username.Equals(procUsername, StringComparison.Ordinal);

            Process process = new Process(processId, procUsername, commandLine, isSameUser);
            _processList.Add(process);
        }
    }
}
