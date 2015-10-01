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
    public struct SourceLine
    {
        public uint Line { get; private set; }
        public ulong AddrStart { get; private set; }

        public void Set(uint line, ulong addr)
        {
            Line = line;
            AddrStart = addr;
        }

        public bool EndOfFunction { get { return Line == 0; } }
    }

    internal class SourceLineCache
    {
        private Dictionary<string, SourceLine[]> _mapFileToLinenums;
        private DebuggedProcess _process;

        public SourceLineCache(DebuggedProcess process)
        {
            _process = process;
            _mapFileToLinenums = new Dictionary<string, SourceLine[]>();
        }
        internal async Task<SourceLine[]> GetLinesForFile(string file)
        {
            string fileKey = file;
            lock (_mapFileToLinenums)
            {
                if (_mapFileToLinenums.ContainsKey(fileKey))
                {
                    return _mapFileToLinenums[fileKey];
                }
            }
            SourceLine[] lines = null;
            lines = await LinesForFile(fileKey);
            lock (_mapFileToLinenums)
            {
                if (_mapFileToLinenums.ContainsKey(fileKey))
                {
                    return _mapFileToLinenums[fileKey];
                }
                if (lines != null)
                {
                    _mapFileToLinenums.Add(fileKey, lines);
                }
                else
                {
                    _mapFileToLinenums.Add(fileKey, new SourceLine[0]);    // empty list to prevent requerying. Release this list on dynamic library loading
                }
                return _mapFileToLinenums[fileKey];
            }
        }
        private async Task<SourceLine[]> LinesForFile(string file)
        {
            string cmd = "-symbol-list-lines " + _process.EscapePath(file);
            Results results = await _process.CmdAsync(cmd, ResultClass.None);

            if (results.ResultClass != ResultClass.done)
            {
                return null;
            }

            ValueListValue lines = results.Find<ValueListValue>("lines");
            SourceLine[] list = new SourceLine[lines.Content.Length];
            for (int i = 0; i < lines.Content.Length; ++i)
            {
                ulong addr = lines.Content[i].FindAddr("pc");
                uint line = lines.Content[i].FindUint("line");
                list[i].Set(line, addr);
            }
            return list;
        }

        internal void OnLibraryLoad()
        {
            lock (_mapFileToLinenums)
            {
                List<string> toDelete = new List<string>();
                foreach (var l in _mapFileToLinenums)
                {
                    if (l.Value.Length == 0)
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
