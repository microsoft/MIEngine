// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidDebugLauncher
{
    /// <summary>
    /// Code for parsing output from programs that write tables with fixed-width colmuns. Used for parsing
    /// the output from the 'ps' command.
    /// </summary>
    [DebuggerDisplay("Name={Name}")]
    internal class TextColumn
    {
        /// <summary>
        /// [Required] Name of the column
        /// </summary>
        readonly public string Name;

        /// <summary>
        /// [Required] Character index where the column starts
        /// </summary>
        readonly public int StartIndex;

        /// <summary>
        /// [Optional] Length of the colmun. This will be -1 for the last column.
        /// </summary>
        readonly public int Length;

        private TextColumn(string name, int startIndex, int length)
        {
            this.Name = name;
            this.StartIndex = startIndex;
            this.Length = length;
        }

        /// <summary>
        /// Parsers the header line from a table, returning an array of columns
        /// </summary>
        /// <param name="headerLine">The line of text with the header</param>
        /// <returns>[Optional] Array of columns</returns>
        public static TextColumn[] TryParseHeader(string headerLine)
        {
            List<TextColumn> columns = new List<TextColumn>();
            int startIndex = 0;
            int position = 0;

            // Move past any initial whitespace
            if (!SkipWhitespace(ref position, headerLine))
            {
                return null; // line is nothing but whitespace
            }
            int nameStartIndex = position;

            while (true)
            {
                // Skip until the start of the next column
                int nextWhitespace = headerLine.IndexOfAny(s_spaceChars, position);
                position = nextWhitespace;
                if (position >= 0 && SkipWhitespace(ref position, headerLine))
                {
                    // Position is currently at the start of the next column. Create an object for the column we just went past
                    TextColumn newColumn = new TextColumn(headerLine.Substring(nameStartIndex, nextWhitespace - nameStartIndex), startIndex, position - startIndex);
                    columns.Add(newColumn);
                    nameStartIndex = startIndex = position;
                }
                else
                {
                    // Create the last column
                    if (nextWhitespace < 0)
                        nextWhitespace = headerLine.Length;
                    TextColumn newColumn = new TextColumn(headerLine.Substring(nameStartIndex, nextWhitespace - nameStartIndex), startIndex, -1);
                    columns.Add(newColumn);

                    return columns.ToArray();
                }
            }
        }

        public string ExtractCell(string row)
        {
            if (row.Length < this.StartIndex)
                return string.Empty;

            int length = this.Length;
            if (length < 0)
                length = row.Length - this.StartIndex;

            // The header and the data rows don't exsactly match up where they start -
            // sometimes the data rows are a column sooner (ex: 'NAME', 'PC') and sometimes the
            // data rows are a colmn after (ex: 'PID', 'PPID').
            //
            // USER     PID   PPID  VSIZE  RSS     WCHAN    PC         NAME
            // root      1     0     640    496   c00bd520 00019fb8 S /init
            // root      2     0     0      0     c00335a0 00000000 S kthreadd
            // root      3     2     0      0     c001e39c 00000000 S ksoftirqd/0

            int startIndex = this.StartIndex;

            int? delta = FindColumnBreak(row, this.StartIndex);
            if (!delta.HasValue)
                return string.Empty;

            startIndex += delta.Value;
            length -= delta.Value;

            if (this.Length > 0)
            {
                delta = FindColumnBreak(row, startIndex + length);
                if (delta.HasValue)
                {
                    length += delta.Value;
                }
            }

            while (length > 0 && IsSpace(row, startIndex + length - 1))
            {
                length--;
            }

            return row.Substring(startIndex, length);
        }

        /// <summary>
        /// Hunt arround to try and find a column break near the specified index
        /// </summary>
        /// <param name="row">line of text to test</param>
        /// <param name="index">Expected spot for the column break</param>
        /// <returns>null if no column break could be found, otherwise the delta between the expected index and where it was really found</returns>
        private int? FindColumnBreak(string row, int index)
        {
            int[] candidateDeltas = { 0, -1, 1, -2, 2, -3, 3 };
            foreach (int delta in candidateDeltas)
            {
                if (IsColmnBreakIndex(row, index + delta))
                {
                    return delta;
                }
            }

            return null;
        }

        /// <summary>
        /// Check if the specified position is a column break
        /// </summary>
        /// <param name="row">Line of text to check</param>
        /// <param name="index">Index to check</param>
        /// <returns>Returns true if the position before index is a space, and index is NOT a space</returns>
        private static bool IsColmnBreakIndex(string row, int index)
        {
            return IsSpace(row, index - 1) && !IsSpace(row, index);
        }

        private static bool SkipWhitespace(ref int position, string headerLine)
        {
            while (true)
            {
                if (position == headerLine.Length)
                {
                    position = -1;
                    return false;
                }

                if (!IsSpace(headerLine[position]))
                    return true;

                position++;
            }
        }

        static private readonly char[] s_spaceChars = { ' ', '\t' };
        private static bool IsSpace(char ch)
        {
            return (ch == ' ' || ch == '\t');
        }

        private static bool IsSpace(string str, int index)
        {
            if (index < 0 || index >= str.Length)
                return true; // index outside of the range of a string. It is considered whitespace

            return IsSpace(str[index]);
        }
    }
}
