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
using System.Globalization;

namespace Microsoft.MIDebugEngine
{
    internal class DisasmInstruction
    {
        public ulong Addr;
        public string AddressString;
        public string Symbol;
        public uint Offset;
        public string Opcode;
        public string CodeBytes;
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
            // allow addresses within an instruction to match the instruction
            for (int i = 0; i < _instructions.Length - 1; ++i)
            {
                if (_instructions[i].Addr <= addr && _instructions[i + 1].Addr > addr)
                {
                    return i;
                }
            }
            if (_instructions[_instructions.Length - 1].Addr == addr)
            {
                return _instructions.Length - 1;
            }

            return -1;
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

        public bool TryFetch(ulong addr, int cnt, out ICollection<DisasmInstruction> instructions)
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

        public bool TryFetch(ulong startAddr, ulong endAddr, out ICollection<DisasmInstruction> instructions)
        {
            instructions = null;
            if (!Contains(startAddr, 1))
                return false;
            if (!Contains(endAddr, 1))
                return false;
            int i = FindIndex(startAddr);
            int j = FindIndex(endAddr);
            instructions = new ArraySegment<DisasmInstruction>(_instructions, i, j-i);
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

        private ICollection<DisasmInstruction> UpdateCache(ulong address, int nInstructions, DisasmInstruction[] instructions)
        {
            ICollection<DisasmInstruction> ret = null;
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

        /// <summary>
        /// Seek backward n instructions from a target address
        /// </summary>
        /// <param name="address">target address</param>
        /// <param name="nInstructions">number of instructions</param>
        /// <returns> address - n on failure, else the address of an instruction n back from the target address</returns>
        public async Task<ulong> SeekBack(ulong address, int nInstructions)
        {
            ICollection<DisasmInstruction> ret = null;
            ulong defaultAddr = address >= (ulong)nInstructions ? address - (ulong)nInstructions : 0;

            lock (_disassemlyCache)
            {
                // check the cache, look for it to contain nInstructions back from the address
                var kv = _disassemlyCache.FirstOrDefault((p) => p.Value.TryFetch(address, -nInstructions, out ret));
                if (kv.Value != null)
                    return ret.First().Addr;
            }
            ulong endAddress;
            ulong startAddress;
            var range = await _process.FindValidMemoryRange(address, (uint)(_process.MaxInstructionSize * (nInstructions+1)), (int)(_process.MaxInstructionSize * -nInstructions));
            startAddress = range.Item1;
            endAddress = range.Item2;
            if (endAddress - startAddress == 0 || address < startAddress) // bad address range, no instructions
            {
                return defaultAddr;
            }
            lock (_disassemlyCache)
            {
                // check the cache with the adjusted range
                var kv = _disassemlyCache.FirstOrDefault((p) => p.Value.TryFetch(startAddress, address < endAddress ? address : endAddress, out ret));
            }
            if (ret == null)
            {
                DisasmInstruction[] instructions = await Disassemble(_process, startAddress, endAddress);
                if (instructions == null)
                {
                    return defaultAddr;    // unknown error condition
                }

                // when seeking back require that the disassembly contain an instruction at the target address (x86 has varying length instructions) 
                instructions = await VerifyDisassembly(instructions, startAddress, endAddress, address);

                ret = UpdateCache(address, -nInstructions, instructions);
                if (ret == null)
                {
                    return defaultAddr;
                }
            }

            int nLess = ret.Count((i) => i.Addr < address);
            if (nLess < nInstructions)
            {
                // not enough instructions were fetched; back up one byte for each missing instruction
                return ret.First().Addr < (ulong)(nInstructions - nLess) ? 0 : (ulong)((long)ret.First().Addr - (nInstructions - nLess));
            }
            else
            {
                return ret.First().Addr;
            }
        }

        // 
        /// <summary>
        /// Fetch disassembly for a range of instructions. 
        /// </summary>
        /// <param name="address">Beginning address of an instruction to use as a starting point for disassembly.</param>
        /// <param name="nInstructions">Number of instructions to disassemble.</param>
        /// <returns></returns>
        public async Task<ICollection<DisasmInstruction>> FetchInstructions(ulong address, int nInstructions)
        {
            ICollection<DisasmInstruction> ret = null;

            lock (_disassemlyCache)
            {
                // check the cache
                var kv = _disassemlyCache.FirstOrDefault((p) => p.Value.TryFetch(address, nInstructions, out ret));
                if (kv.Value != null)
                    return ret;
            }

            ulong endAddress;
            ulong startAddress;
            var range = await _process.FindValidMemoryRange(address, (uint)(_process.MaxInstructionSize * nInstructions), 0);
            startAddress = range.Item1;
            endAddress = range.Item2;
            int gap = (int)(startAddress - address);   // num of bytes before instructions begin
            if (endAddress > startAddress && nInstructions > gap)
            {
                nInstructions -= gap;
            }
            else
            {
                nInstructions = 0;
            }
            if (endAddress - startAddress == 0 || nInstructions == 0)
            {
                return null;
            }
            lock (_disassemlyCache)
            {
                // re-check the cache with the verified memory range
                var kv = _disassemlyCache.FirstOrDefault((p) => p.Value.TryFetch(startAddress, nInstructions, out ret));
                if (kv.Value != null)
                    return ret;
            }

            DisasmInstruction[] instructions = await Disassemble(_process, startAddress, endAddress);

            instructions = await VerifyDisassembly(instructions, startAddress, endAddress, address);

            return UpdateCache(address, nInstructions, instructions);
        }

        private async Task<DisasmInstruction[]> VerifyDisassembly(DisasmInstruction[] instructions, ulong startAddress, ulong endAddress, ulong targetAddress)
        {
            if (startAddress > targetAddress || targetAddress > endAddress)
            {
                return instructions;
            }
            var originalInstructions = instructions;
            int count = 0;
            while (instructions != null && (instructions.Length == 0 || Array.Find(instructions, (i)=>i.Addr == targetAddress) == null) && count < _process.MaxInstructionSize)
            {
                count++;
                startAddress--;         // back up one byte
                instructions = await Disassemble(_process, startAddress, endAddress); // try again
            }
            return instructions == null ? originalInstructions : instructions;
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
            string cmd;
            string startAddrStr;
            string endAddrStr;
            int i = 0;
            Results results;
            do
            {
                startAddrStr = EngineUtils.AsAddr(startAddr, process.Is64BitArch);
                endAddrStr = EngineUtils.AsAddr(endAddr, process.Is64BitArch);
                cmd = "-data-disassemble -s " + startAddrStr + " -e " + endAddrStr + " -- 2";
                results = await process.CmdAsync(cmd, ResultClass.None);
                --startAddr;
                ++i;
            } while (results.ResultClass != ResultClass.done && i < process.MaxInstructionSize);
            
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
                file = process.EnsureProperPathSeparators(file);
            }
            string cmd = "-data-disassemble -f " + file + " -l " + line.ToString(CultureInfo.InvariantCulture) + " -n " + dwInstructions.ToString(CultureInfo.InvariantCulture) + " -- 1";
            Results results = await process.CmdAsync(cmd, ResultClass.None);
            if (results.ResultClass != ResultClass.done)
            {
                return null;
            }

            return DecodeSourceAnnotatedDisassemblyInstructions(process, results.Find<ResultListValue>("asm_insns").FindAll<TupleValue>("src_and_asm_line"));
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
                inst.CodeBytes = items[i].TryFindString("opcodes");
                inst.Line = 0;
                inst.File = null;
                instructions[i] = inst;
            }
            return instructions;
        }
        private static IEnumerable<DisasmInstruction> DecodeSourceAnnotatedDisassemblyInstructions(DebuggedProcess process, TupleValue[] items)
        {
            foreach (var item in items)
            {
                uint line = item.FindUint("line");
                string file = process.GetMappedFileFromTuple(item);
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
                    disassemblyData.CodeBytes = asm_item.TryFindString("opcodes");
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
