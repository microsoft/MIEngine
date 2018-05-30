// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MICore;

namespace Microsoft.MIDebugEngine
{
    public class SourceLineMap : Dictionary<ulong, SourceLine>
    {
        public SourceLineMap(int capacity)
            : base(capacity)
        { }

        public void Add(ulong addr, uint line)
        {
            base.Add(addr, new SourceLine(line, addr));
        }

        public void Replace(ulong addr, uint line)
        {
            base[addr] = new SourceLine(line, addr);
        }
    }

    public struct SourceLine
    {
        public uint Line { get; private set; }
        public ulong AddrStart { get; private set; }

        public SourceLine(uint line, ulong addr)
        {
            Line = line;
            AddrStart = addr;
        }

        public bool EndOfFunction { get { return Line == 0; } }
    }

    internal class SourceLineCache
    {
        private Dictionary<string, SourceLineMap> _mapFileToLinenums;
        private DebuggedProcess _process;

        public SourceLineCache(DebuggedProcess process)
        {
            _process = process;
            _mapFileToLinenums = new Dictionary<string, SourceLineMap>();
        }

        public void Clear()
        {
            this._mapFileToLinenums.Clear();
        }

        internal async Task<SourceLineMap> GetLinesForFile(string file)
        {
            string fileKey = file;
            lock (_mapFileToLinenums)
            {
                if (_mapFileToLinenums.ContainsKey(fileKey))
                {
                    return _mapFileToLinenums[fileKey];
                }
            }
            SourceLineMap linesMap = null;
            linesMap = await LinesForFile(fileKey);
            lock (_mapFileToLinenums)
            {
                if (_mapFileToLinenums.ContainsKey(fileKey))
                {
                    return _mapFileToLinenums[fileKey];
                }
                if (linesMap != null)
                {
                    _mapFileToLinenums.Add(fileKey, linesMap);
                }
                else
                {
                    _mapFileToLinenums.Add(fileKey, new SourceLineMap(0));    // empty list to prevent requerying. Release this list on dynamic library loading
                }
                return _mapFileToLinenums[fileKey];
            }
        }
        private async Task<SourceLineMap> LinesForFile(string file)
        {
            string cmd = "-symbol-list-lines " + _process.EscapeSymbolPath(file);
            Results results = await _process.CmdAsync(cmd, ResultClass.None);

            if (results.ResultClass != ResultClass.done)
            {
                return null;
            }

            ValueListValue lines = results.Find<ValueListValue>("lines");
            SourceLineMap linesMap = new SourceLineMap(lines.Content.Length);
            for (int i = 0; i < lines.Content.Length; ++i)
            {
                ulong addr = lines.Content[i].FindAddr("pc");
                uint line = lines.Content[i].FindUint("line");

                if (linesMap.ContainsKey(addr))
                {
                    // It is actually fairly common for an address to map to more than one line. For instance,
                    // in debug builds destructors can have an entry to line 0 as well as one to the correct line.
                    // Release builds with inlining will hit this very often.
                    // Unforunately, without more context, it is impossible to know which line is the "right" line.
                    // For the inline case, any line will be acceptable. For the destructor case, we should prefer
                    // a non-zero line.
                    if (linesMap[addr].Line == 0)
                    {
                        linesMap.Replace(addr, line);
                    }
                }
                else
                {
                    linesMap.Add(addr, line);
                }
            }
            return linesMap;
        }

        internal void OnLibraryLoad()
        {
            lock (_mapFileToLinenums)
            {
                List<string> toDelete = new List<string>();
                foreach (var l in _mapFileToLinenums)
                {
                    if (l.Value.Count == 0)
                    {
                        toDelete.Add(l.Key);
                    }
                }
                foreach (var file in toDelete)
                {
                    _mapFileToLinenums.Remove(file);   // requery for line numbers next time they are asked for
                }
            }
        }
    }
}
