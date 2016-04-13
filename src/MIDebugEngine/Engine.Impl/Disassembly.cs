// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using MICore;

namespace Microsoft.MIDebugEngine
{
    internal class DisasmInstruction
    {
        public ulong Addr;
        public string AddressString;
        public string Symbol;
        public uint Offset;
        public string Opcode;
        public string File;
        public uint Line;
        public uint OffsetInLine;
    };
    internal class DisassemblyBlock
    {
        static private long s_touchCount = 0;
        internal long Touch;

        private readonly DisasmInstruction[] _instructions;

        private int FindIndex(ulong addr)
        {
            return Array.FindIndex(_instructions, (p) => p.Addr == addr);
        }

        internal ulong Address { get { return _instructions[0].Addr; } }
        internal int Count { get { return _instructions.Length; } }

        public bool Contains(ulong addr, int cnt)
        {
            if (_instructions.Length == 0)
                return false;
            if (_instructions[0].Addr > addr || addr > _instructions[_instructions.Length - 1].Addr)
                return false;
            int i = FindIndex(addr);
            if (i < 0)
            {
                return false;
            }
            i += cnt;
            Touch = Interlocked.Increment(ref s_touchCount);
            return 0 <= i && i <= _instructions.Length;
        }

        public bool TryFetch(ulong addr, int cnt, out IEnumerable<DisasmInstruction> instructions)
        {
            instructions = null;
            if (!Contains(addr, cnt))
                return false;
            int i = FindIndex(addr);
            if (cnt < 0)
            {
                i = i + cnt;
                cnt = -cnt;
            }
            instructions = new ArraySegment<DisasmInstruction>(_instructions, i, cnt);
            return true;
        }

        public DisasmInstruction At(ulong addr)
        {
            Debug.Assert(Contains(addr, 1), "Address not in block");
            Touch = ++s_touchCount;
            for (int i = 0; i < _instructions.Length; ++i)
            {
                if (_instructions[i].Addr == addr)
                {
                    return _instructions[i];
                }
            }
            return null;
        }
        public DisassemblyBlock(DisasmInstruction[] ins)
        {
            Touch = ++s_touchCount;
            _instructions = ins;
        }
    }
    internal class Disassembly
    {
        private const int cacheSize = 10;   // number of cached blocks to keep
        private SortedList<ulong, DisassemblyBlock> _disassemlyCache;
        private DebuggedProcess _process;

        public Disassembly(DebuggedProcess process)
        {
            _process = process;
            _disassemlyCache = new SortedList<ulong, DisassemblyBlock>();
        }

        // 
        /// <summary>
        /// Fetch disassembly for a range of instructions. May return more instructions than asked for.
        /// </summary>
        /// <param name="address">Beginning address of an instruction to use as a starting point for disassembly.</param>
        /// <param name="nInstructions">Number of instructions to disassemble. Negative values indicate disassembly backwards from "address".</param>
        /// <returns></returns>
        public async Task<IEnumerable<DisasmInstruction>> FetchInstructions(ulong address, int nInstructions)
        {
            IEnumerable<DisasmInstruction> ret = null;

            lock (_disassemlyCache)
            {
                // check the cache
                var kv = _disassemlyCache.FirstOrDefault((p) => p.Value.TryFetch(address, nInstructions, out ret));
                if (kv.Value != null)
                    return ret;
            }

            ulong endAddress;
            ulong startAddress;
            if (nInstructions >= 0)
            {
                startAddress = address;
                endAddress = startAddress + (ulong)(_process.MaxInstructionSize * nInstructions);
            }
            else
            {
                endAddress = address;
                startAddress = endAddress - (uint)(_process.MaxInstructionSize * -nInstructions);
                endAddress++;   // make sure to fetch an instruction covering the original start address
            }
            DisasmInstruction[] instructions = await Disassemble(_process, startAddress, endAddress);

            if (instructions != null && instructions.Length > 0 && nInstructions < 0)
            {
                instructions = await VerifyDisassembly(instructions, startAddress, address);
            }

            if (instructions != null && instructions.Length > 0)
            {
                DisassemblyBlock block = new DisassemblyBlock(instructions);
                lock (_disassemlyCache)
                {
                    // push to the cache
                    DeleteRangeFromCache(block);    // removes any entry with the same key
                    if (_disassemlyCache.Count >= cacheSize)
                    {
                        long max = 0;
                        int toDelete = -1;
                        for (int i = 0; i < _disassemlyCache.Count; ++i)
                        {
                            var e = _disassemlyCache.ElementAt(i);
                            if (e.Value.Touch > max)
                            {
                                max = e.Value.Touch;
                                toDelete = i;
                            }
                        }
                        Debug.Assert(toDelete >= 0, "Failed to flush from the cache");
                        _disassemlyCache.RemoveAt(toDelete);
                    }
                    _disassemlyCache.Add(block.Address, block);
                }
                var kv = block.TryFetch(address, nInstructions, out ret);
            }

            return ret;
        }

        private async Task<DisasmInstruction[]> VerifyDisassembly(DisasmInstruction[] instructions, ulong startAddress, ulong targetAddress)
        {
            int count = 0;
            while (instructions[instructions.Length - 1].Addr != targetAddress && count < _process.MaxInstructionSize)
            {
                instructions = null;    // throw away the previous instructions
                count++;
                startAddress--;         // back up one byte
                instructions = await Disassemble(_process, startAddress, targetAddress + 1); // try again
            }
            return instructions;
        }

        private void DeleteRangeFromCache(DisassemblyBlock block)
        {
            for (int i = 0; i < _disassemlyCache.Count; ++i)
            {
                DisassemblyBlock elem = _disassemlyCache.ElementAt(i).Value;
                if (block.Contains(elem.Address, elem.Count))
                {
                    _disassemlyCache.RemoveAt(i);
                    break;
                }
            }
        }

        // this is inefficient so we try and grab everything in one gulp
        internal static async Task<DisasmInstruction[]> Disassemble(DebuggedProcess process, ulong startAddr, ulong endAddr)
        {
            string cmd = "-data-disassemble -s " + EngineUtils.AsAddr(startAddr, process.Is64BitArch) + " -e " + EngineUtils.AsAddr(endAddr, process.Is64BitArch) + " -- 0";
            Results results = await process.CmdAsync(cmd, ResultClass.None);
            if (results.ResultClass != ResultClass.done)
            {
                return null;
            }

            return DecodeDisassemblyInstructions(results.Find<ValueListValue>("asm_insns").AsArray<TupleValue>());
        }

        // this is inefficient so we try and grab everything in one gulp
        internal async Task<IEnumerable<DisasmInstruction>> Disassemble(DebuggedProcess process, string file, uint line, uint dwInstructions)
        {
            if (file.IndexOf(' ') >= 0) // only needs escaping if filename contains a space
            {
                file = process.EscapePath(file);
            }
            string cmd = "-data-disassemble -f " + file + " -l " + line.ToString() + " -n " + dwInstructions.ToString() + " -- 1";
            Results results = await process.CmdAsync(cmd, ResultClass.None);
            if (results.ResultClass != ResultClass.done)
            {
                return null;
            }

            return DecodeSourceAnnotatedDisassemblyInstructions(results.Find<ResultListValue>("asm_insns").FindAll<TupleValue>("src_and_asm_line"));
        }

        private static DisasmInstruction[] DecodeDisassemblyInstructions(TupleValue[] items)
        {
            DisasmInstruction[] instructions = new DisasmInstruction[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                DisasmInstruction inst = new DisasmInstruction();
                inst.Addr = items[i].FindAddr("address");
                inst.AddressString = items[i].FindString("address");
                inst.Symbol = items[i].TryFindString("func-name");
                inst.Offset = items[i].Contains("offset") ? items[i].FindUint("offset") : 0;
                inst.Opcode = items[i].FindString("inst");
                inst.Line = 0;
                inst.File = null;
                instructions[i] = inst;
            }
            return instructions;
        }
        private static IEnumerable<DisasmInstruction> DecodeSourceAnnotatedDisassemblyInstructions(TupleValue[] items)
        {
            foreach (var item in items)
            {
                uint line = item.FindUint("line");
                string file = item.FindString("file");
                ValueListValue asm_items = item.Find<ValueListValue>("line_asm_insn");
                uint lineOffset = 0;
                foreach (var asm_item in asm_items.Content)
                {
                    DisasmInstruction disassemblyData = new DisasmInstruction();
                    disassemblyData.Addr = asm_item.FindAddr("address");
                    disassemblyData.AddressString = asm_item.FindString("address");
                    disassemblyData.Symbol = asm_item.TryFindString("func-name");
                    disassemblyData.Offset = asm_item.Contains("offset") ? asm_item.FindUint("offset") : 0;
                    disassemblyData.Opcode = asm_item.FindString("inst");
                    disassemblyData.Line = line;
                    disassemblyData.File = file;
                    if (lineOffset == 0)
                    {
                        lineOffset = disassemblyData.Offset;    // offset to start of current line
                    }
                    disassemblyData.OffsetInLine = disassemblyData.Offset - lineOffset;
                    yield return disassemblyData;
                }
            }
        }
    }
}
