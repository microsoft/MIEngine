// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SSHDebugPS
{
    // TODO: Make this internal and use IntenalsVisibleTo to allow for unit test
    public class PSOutputParser
    {
        public class Process
        {
            public uint Id { get; private set; }
            public string CommandLine { get; private set; }
            public string UserName { get; private set; }
            public bool IsSameUser { get; private set; }

            public Process(uint id, string userName, string commandLine, bool isSameUser)
            {
                this.Id = id;
                this.UserName = userName;
                this.CommandLine = commandLine;
                this.IsSameUser = isSameUser;
            }
        }
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

        private const string UserNamePrefix = "CurrentUserName: ";
        private const string PSCommandLine = "ps -axww -o pid=A,ruser=B,args=C";
        public const string CommandText = "echo " + UserNamePrefix + "$USER; " + PSCommandLine;
        private string _currentUserName;
        private ColumnDef _pidCol;
        private ColumnDef _ruserCol;
        private ColumnDef _argsCol;

        public static List<Process> Parse(string output)
        {
            var @this = new PSOutputParser();
            return @this.ParseInternal(output);
        }

        private PSOutputParser()
        {
        }

        private List<Process> ParseInternal(string output)
        {
            List<Process> processList = new List<Process>();

            using (var reader = new StringReader(output))
            {
                if (!ProcessUserName(reader.ReadLine()))
                {
                    throw new CommandFailedException(StringResources.Error_PSFailed);
                }

                if (!ProcessHeaderLine(reader.ReadLine()))
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

                    if (process.CommandLine.EndsWith(PSCommandLine))
                        continue; // ignore the 'ps' process that we spawned

                    processList.Add(process);
                }

                if (processList.Count == 0)
                {
                    throw new CommandFailedException(StringResources.Error_PSFailed);
                }

                return processList;
            }
        }

        private bool ProcessUserName(/*OPTIONAL*/ string userNameLine)
        {
            if (userNameLine == null)
                return false;

            if (!userNameLine.StartsWith(UserNamePrefix))
                return false;

            _currentUserName = userNameLine.Substring(UserNamePrefix.Length).Trim();
            return true;
        }

        private bool ProcessHeaderLine(/*OPTIONAL*/ string headerLine)
        {
            int index = 0;
            if (!SkipWhitespace(headerLine, ref index))
                return false;
            // pid column is right justified so the pid column stops at the 'A' index
            if (headerLine[index] != 'A')
                return false;

            _pidCol = new ColumnDef(0, index);

            index++;
            if (!SkipWhitespace(headerLine, ref index))
                return false;
            if (headerLine[index] != 'B')
                return false;
            // ruser and args columns are left justified
            int colStart = index++;
            if (!SkipWhitespace(headerLine, ref index))
                return false;
            if (headerLine[index] != 'C')
                return false;

            _ruserCol = new ColumnDef(colStart, index - 1);

            // The rest of the line is the args column
            _argsCol = new ColumnDef(index, int.MaxValue);

            // make sure the line is now empty, aside from whitespace
            index++;
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

            string ruser = _ruserCol.Extract(line);
            string commandLine = _argsCol.Extract(line);
            if (string.IsNullOrEmpty(commandLine))
                return null;

            bool isSameUser;
            if (!string.IsNullOrEmpty(_currentUserName))
            {
                isSameUser = _currentUserName.Equals(ruser, StringComparison.Ordinal);
            }
            else
            {
                // If we couldn't get a user name for some reason, accept anything besides 'root'
                isSameUser = !ruser.Equals("root", StringComparison.Ordinal);
            }

            return new Process(pid, ruser, commandLine, isSameUser);
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
    }
}
