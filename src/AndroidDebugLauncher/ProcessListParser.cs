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
    internal class ProcessListParser
    {
        private readonly IList<string> _lines;
        private readonly TextColumn _pidColumn;
        private readonly TextColumn _nameColumn;

        public ProcessListParser(string text) : this(text.GetLines().ToList())
        {
        }

        public ProcessListParser(IList<string> lines)
        {
            // We at least should have a line for the header, a system process, and 'ps'
            if (lines.Count < 3)
            {
                throw GetException();
            }

            _lines = lines;

            TextColumn[] columns = TextColumn.TryParseHeader(lines[0]);
            if (columns == null || columns.Length < 2)
            {
                throw GetException();
            }

            _pidColumn = columns.FirstOrDefault((column) => column.Name.Equals("PID", StringComparison.OrdinalIgnoreCase));
            if (_pidColumn == null)
                throw GetException();

            _nameColumn = columns.FirstOrDefault((column) => column.Name.Equals("NAME", StringComparison.OrdinalIgnoreCase));
            if (_nameColumn == null)
            {
                // use the last column if one wasn't called 'name'
                _nameColumn = columns[columns.Length - 1];
            }
        }

        /// <summary>
        /// Finds the given process in the process list
        /// </summary>
        /// <param name="name">Name of the process</param>
        /// <returns>[Required] List of process ids</returns>
        public List<int> FindProcesses(string name)
        {
            List<int> processList = new List<int>();
            for (int c = 1; c < _lines.Count; c++)
            {
                string row = _lines[c];
                string rowName = _nameColumn.ExtractCell(row);
                if (DoesProcessNameMatch(rowName, name))
                {
                    string pidString = _pidColumn.ExtractCell(row);
                    int pid;
                    if (!int.TryParse(pidString, NumberStyles.None, CultureInfo.InvariantCulture, out pid) ||
                        pid <= 0)
                    {
                        throw GetException();
                    }

                    processList.Add(pid);
                }
            }

            return processList;
        }

        private bool DoesProcessNameMatch(string rowName, string name)
        {
            if (rowName == name)
                return true;

            // The 'ps' listing has a 'Process State Codes' column before the name which the column parser can't figure out.
            // For example, in the below listing it is the S/D/t/R code. Ignore that if the rest of the name would match it.
            //
            //USER     PID   PPID  VSIZE  RSS     WCHAN    PC        NAME
            //root      1     0     2616   780   ffffffff 00000000 S /init
            //root      2     0     0      0     ffffffff 00000000 S kthreadd
            //root      3     2     0      0     ffffffff 00000000 S ksoftirqd/0
            //root      7     2     0      0     ffffffff 00000000 D kworker/u:0H
            //root      7711  2     0      0     ffffffff 00000000 S kworker/0:1
            //root      7818  2     0      0     ffffffff 00000000 S kworker/0:0H
            //u0_a82    7848  195   1488916 42940 ffffffff 00000000 t com.example.hellojni
            //u0_a82    7874  5448  656    372   ffffffff 00000000 S /data/data/com.example.hellojni/lib/gdbserver
            //shell     7915  5448  4460   756   00000000 b6eafb18 R ps
            if (rowName.Length == name.Length + 2 && rowName[1] == ' ' && rowName.EndsWith(name, StringComparison.Ordinal))
                return true;

            return false;
        }

        private static LauncherException GetException()
        {
            return new LauncherException(Telemetry.LaunchFailureCode.BadPsOutput, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_ShellCommandBadResults, "ps"));
        }
    }
}
