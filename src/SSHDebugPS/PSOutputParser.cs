// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.SSHDebugPS.Utilities;

namespace Microsoft.SSHDebugPS
{
    internal class PSOutputParser
    {
        private struct ColumnDef
        {
            /// <summary>
            /// 0-based index for the start of the column
            /// </summary>
            public readonly int Start;

            /// <summary>
            /// 0-based index for the last character in the column
            /// </summary>
            public readonly int End;

            public ColumnDef(int start, int end)
            {
                this.Start = start;
                this.End = end;
            }

            public string Extract(string line)
            {
                if (line.Length <= this.Start)
                    return string.Empty;

                int start = this.Start;
                int end = Math.Min(this.End, line.Length - 1);

                // trim off any spaces
                while (start <= end && line[start] == ' ')
                {
                    start++;
                }
                while (start <= end && line[end] == ' ')
                {
                    end--;
                }

                if (start > end)
                {
                    return String.Empty;
                }

                return line.Substring(start, end - start + 1);
            }
        }

        // Use padding to expand column width. 10 for pid and 32 for userid as that is the max size for each
        // Tested this format with different distributions of Linux and container distributions. This command (and the alternative without the flags) seems 
        // to be the one that works the best between standard *nix and BusyBox implementations of ps.
        private const string PSCommandLineFormat = "ps{0}-o pid=pppppppppp{1} -o ruser=rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr -o args";
        private SystemInformation _currentSystemInformation;
        private ColumnDef _pidCol;
        private ColumnDef _flagsCol;
        private ColumnDef _ruserCol;
        private ColumnDef _argsCol;

        // In order to determine the architecture of a process, we need to run the ps command with 'flags'.
        // However, certain of distros of Linux do not support flags, so only add this for macOS.
        private string PSFlagFormat => _currentSystemInformation.Platform == PlatformID.MacOSX ? " -o flags=ffffffff" : string.Empty;

        public string PSCommandLine => PSCommandLineFormat.FormatInvariantWithArgs(" axww ", PSFlagFormat);
        public string AltPSCommandLine => PSCommandLineFormat.FormatInvariantWithArgs(" ", PSFlagFormat);

        public PSOutputParser(SystemInformation systemInformation)
        {
            _currentSystemInformation = systemInformation;
        }

        public List<Process> Parse(string output)
        {
            List<Process> processList = new List<Process>();

            using (var reader = new StringReader(output))
            {
                string headerLine = reader.ReadLine();
                if (!ProcessHeaderLine(headerLine))
                {
                    throw new CommandFailedException(StringResources.Error_PSFailed);
                }

                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;

                    Process process = SplitPSLine(line);
                    if (process == null)
                        continue;

                    if (process.CommandLine.EndsWith(PSCommandLine, StringComparison.Ordinal))
                        continue; // ignore the 'ps' process that we spawned

                    if (process.CommandLine.EndsWith(AltPSCommandLine, StringComparison.Ordinal))
                        continue;

                    processList.Add(process);
                }

                if (processList.Count == 0)
                {
                    throw new CommandFailedException(StringResources.Error_PSFailed);
                }

                return processList;
            }
        }

        private bool ProcessHeaderLine(/*OPTIONAL*/ string headerLine)
        {
            int index = 0;
            int strLen = headerLine.Length;

            // pid column is a fixed length column
            if (!SkipNonWhitespace(headerLine, ref index))
                return false;
            _pidCol = new ColumnDef(0, index);

            if (!SkipWhitespace(headerLine, ref index))
                return false;

            int colStart = index;
            if (!SkipNonWhitespace(headerLine, ref index))
                return false;

            /// <see cref PSFlagFormat/> on why this is only executed for macOS.
            if (_currentSystemInformation.Platform == PlatformID.MacOSX)
            {
                _flagsCol = new ColumnDef(colStart, index);

                if (!SkipWhitespace(headerLine, ref index))
                    return false;

                colStart = index;
                if (!SkipNonWhitespace(headerLine, ref index))
                    return false;
            }

            _ruserCol = new ColumnDef(colStart, index);

            if (!SkipWhitespace(headerLine, ref index))
                return false;

            // The rest of the line is the args column
            _argsCol = new ColumnDef(index, int.MaxValue);
            
            // comsume the rest of the header
            SkipNonWhitespace(headerLine, ref index);

            // make sure the line is now empty, aside from whitespace
            if (SkipWhitespace(headerLine, ref index))
                return false;

            return true;
        }

        private Process SplitPSLine(string line)
        {
            string pidText = _pidCol.Extract(line);
            uint pid;
            if (!uint.TryParse(pidText, NumberStyles.None, CultureInfo.InvariantCulture, out pid))
                return null;

            uint? flags = null;
            /// <see cref PSFlagFormat/> on why this is only executed for macOS.
            if (_currentSystemInformation.Platform == PlatformID.MacOSX)
            {
                string flagsText = _flagsCol.Extract(line);
                if (!uint.TryParse(flagsText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint tempFlags))
                    return null;
                flags = tempFlags;
            }

            string ruser = _ruserCol.Extract(line);
            string commandLine = _argsCol.Extract(line);
            if (string.IsNullOrEmpty(commandLine))
                return null;

            bool isSameUser;
            if (!string.IsNullOrEmpty(_currentSystemInformation.UserName))
            {
                isSameUser = _currentSystemInformation.UserName.Equals(ruser, StringComparison.Ordinal);
            }
            else
            {
                // If we couldn't get a user name for some reason, accept anything besides 'root'
                isSameUser = !string.Equals(ruser, "root", StringComparison.Ordinal);
            }

            return new Process(pid, _currentSystemInformation.Architecture, flags, ruser, commandLine, isSameUser);
        }

        private static bool SkipWhitespace(string line, ref int index)
        {
            while (true)
            {
                if (index >= line.Length)
                    return false;

                if (!char.IsWhiteSpace(line[index]))
                    return true;

                index++;
            }
        }

        /// <summary>
        /// Opposite of SkipWhitespace. It will skip over non-whitespace characters until the next whitespace or the end.
        /// </summary>
        private static bool SkipNonWhitespace(string line, ref int index)
        {
            while (true)
            {
                if (index >= line.Length)
                    return false;

                if (char.IsWhiteSpace(line[index]))
                    return true;

                index++;
            }
        }
    }
}
