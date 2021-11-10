// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Linq;

namespace Microsoft.MIDebugEngine
{
    internal class AD7DisassemblyStream : IDebugDisassemblyStream2
    {
        private AD7Engine _engine;
        private ulong _addr;
        private enum_DISASSEMBLY_STREAM_SCOPE _scope;
        private string _pLastDocument;
        private int m_dwLastSourceLine;
        private bool m_lastSourceInfoStale;

        internal AD7DisassemblyStream(AD7Engine engine, enum_DISASSEMBLY_STREAM_SCOPE scope, IDebugCodeContext2 pCodeContext)
        {
            _engine = engine;
            _scope = scope;
            AD7MemoryAddress addr = pCodeContext as AD7MemoryAddress;
            _addr = addr.Address;
            _pLastDocument = null;
            m_dwLastSourceLine = -1;
            m_lastSourceInfoStale = true;
        }

        #region IDebugDisassemblyStream2 Members

        public int GetCodeContext(ulong uCodeLocationId, out IDebugCodeContext2 ppCodeContext)
        {
            ppCodeContext = new AD7MemoryAddress(_engine, uCodeLocationId, null);
            return Constants.S_OK;
        }

        public int GetCodeLocationId(IDebugCodeContext2 pCodeContext, out ulong puCodeLocationId)
        {
            AD7MemoryAddress addr = pCodeContext as AD7MemoryAddress;
            puCodeLocationId = addr.Address;
            return Constants.S_OK;
        }

        public int GetCurrentLocation(out ulong puCodeLocationId)
        {
            puCodeLocationId = _addr;
            return Constants.S_OK;
        }

        public int GetDocument(string bstrDocumentUrl, out IDebugDocument2 ppDocument)
        {
            // Mixed mode not yet
            ppDocument = null;
            return Constants.S_FALSE;
        }

        public int GetScope(enum_DISASSEMBLY_STREAM_SCOPE[] pdwScope)
        {
            pdwScope[0] = _scope;
            return Constants.S_OK;
        }

        public int GetSize(out ulong pnSize)
        {
            pnSize = 0xFFFFFFFF;
            return Constants.S_OK;
        }

        private DisassemblyData FetchBadInstruction(enum_DISASSEMBLY_STREAM_FIELDS dwFields)
        {
            DisassemblyData dis = new DisassemblyData();
            if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_ADDRESS) != 0)
            {
                dis.dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_ADDRESS;
                dis.bstrAddress = EngineUtils.AsAddr(_addr, _engine.DebuggedProcess.Is64BitArch);
            }

            if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_CODELOCATIONID) != 0)
            {
                dis.dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_CODELOCATIONID;
                dis.uCodeLocationId = _addr;
            }

            if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_SYMBOL) != 0)
            {
                dis.dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_SYMBOL;
                dis.bstrSymbol = string.Empty;
            }

            if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPCODE) != 0)
            {
                dis.dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPCODE;
                dis.bstrOpcode = "??";
            }

            return dis;
        }

        public int Read(uint dwInstructions, enum_DISASSEMBLY_STREAM_FIELDS dwFields, out uint pdwInstructionsRead, DisassemblyData[] prgDisassembly)
        {
            uint iOp = 0;

            IEnumerable<DisasmInstruction> instructions = null;
            _engine.DebuggedProcess.WorkerThread.RunOperation(async () =>
            {
                instructions = await _engine.DebuggedProcess.Disassembly.FetchInstructions(_addr, (int)dwInstructions);
            });
            if (instructions == null || (instructions.First().Addr - _addr > dwInstructions))
            {
                // bad address range, return '??'
                for (iOp = 0; iOp < dwInstructions; _addr++, ++iOp)
                {
                    prgDisassembly[iOp] = FetchBadInstruction(dwFields);
                }
                pdwInstructionsRead = iOp;
                return Constants.S_OK;
            }

            // return '??' for bad addresses at start of range
            for (iOp = 0; _addr < instructions.First().Addr; _addr++, iOp++)
            {
                prgDisassembly[iOp] = FetchBadInstruction(dwFields);
            }

            IEnumerator<DisasmInstruction> instructionEnumerator = instructions.GetEnumerator();
            for (int idx = 0; idx < dwInstructions; idx++)
            {
                instructionEnumerator.MoveNext();
                DisasmInstruction instruction = instructionEnumerator.Current;

                if (iOp >= dwInstructions)
                {
                    break;
                }
                _addr = instruction.Addr;

                if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_ADDRESS) != 0)
                {
                    prgDisassembly[iOp].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_ADDRESS;
                    prgDisassembly[iOp].bstrAddress = instruction.AddressString;
                }

                if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_CODELOCATIONID) != 0)
                {
                    prgDisassembly[iOp].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_CODELOCATIONID;
                    prgDisassembly[iOp].uCodeLocationId = instruction.Addr;
                }

                if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_SYMBOL) != 0)
                {
                    if (instruction.Offset == 0)
                    {
                        prgDisassembly[iOp].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_SYMBOL;
                        prgDisassembly[iOp].bstrSymbol = instruction.Symbol ?? string.Empty;
                    }
                }

                if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPCODE) != 0)
                {
                    prgDisassembly[iOp].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPCODE;
                    prgDisassembly[iOp].bstrOpcode = instruction.Opcode;
                }

                if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_CODEBYTES) != 0)
                {
                    if (!string.IsNullOrWhiteSpace(instruction.CodeBytes))
                    {
                        prgDisassembly[iOp].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_CODEBYTES;
                        prgDisassembly[iOp].bstrCodeBytes = instruction.CodeBytes;
                    }
                }

                if (idx == 0 && m_lastSourceInfoStale)
                {
                    m_lastSourceInfoStale = false;

                    ulong previousInstructionAddress = instruction.Addr - 1;

                    if (previousInstructionAddress >= 0 && previousInstructionAddress < (ulong)prgDisassembly.Length)
                    {
                        DisassemblyData previousInstruction = prgDisassembly[previousInstructionAddress];
                        m_dwLastSourceLine = (int)previousInstruction.posBeg.dwLine - 1;
                        _pLastDocument = previousInstruction.bstrDocumentUrl;
                    }
                }

                if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_POSITION) != 0)
                {
                    uint startLine = instruction.Line - 1;

                    prgDisassembly[iOp].posBeg = new TEXT_POSITION()
                    {
                        dwLine = startLine,
                        dwColumn = 0
                    };
                    prgDisassembly[iOp].posEnd = new TEXT_POSITION()
                    {
                        dwLine = instruction.Line,
                        dwColumn = 0
                    };

                    prgDisassembly[iOp].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_POSITION;

                    if (m_dwLastSourceLine != instruction.Line)
                    {
                        prgDisassembly[iOp].dwFlags |= enum_DISASSEMBLY_FLAGS.DF_HASSOURCE;
                    }
                }

                bool newDocument = string.IsNullOrEmpty(_pLastDocument) || !_pLastDocument.Equals(instruction.File, StringComparison.Ordinal);

                if (!string.IsNullOrWhiteSpace(instruction.File))
                {
                    if ((dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_DOCUMENTURL) != 0)
                    {
                        if (newDocument || idx == 0)
                        {
                            prgDisassembly[iOp].dwFields |= enum_DISASSEMBLY_STREAM_FIELDS.DSF_DOCUMENTURL;
                            prgDisassembly[iOp].bstrDocumentUrl = string.Concat("file://", instruction.File);
                        }
                    }
                }

                m_dwLastSourceLine = (int)instruction.Line;
                if (newDocument)
                {
                    prgDisassembly[iOp].dwFlags |= enum_DISASSEMBLY_FLAGS.DF_DOCUMENTCHANGE;
                    _pLastDocument = instruction.File;
                }

                iOp++;
            };

            if (iOp < dwInstructions)
            {
                // Didn't get enough instructions. Must have run out of valid memory address range.
                Tuple<ulong, ulong> range = new Tuple<ulong, ulong>(0,0);
                _engine.DebuggedProcess.WorkerThread.RunOperation(async () =>
                {
                    range = await _engine.DebuggedProcess.FindValidMemoryRange(_addr, 10, 0);
                });
                // return '??' for bad addresses at end of range
                for (_addr = range.Item2; iOp < dwInstructions; _addr++, iOp++)
                {
                    prgDisassembly[iOp] = FetchBadInstruction(dwFields);
                }
            }
            pdwInstructionsRead = iOp;

            return pdwInstructionsRead != 0 ? Constants.S_OK : Constants.S_FALSE;
        }

        private int SeekForward(long iInstructions)
        {
            ICollection<DisasmInstruction> instructions = null;
            _engine.DebuggedProcess.WorkerThread.RunOperation(async () =>
            {
                instructions = await _engine.DebuggedProcess.Disassembly.FetchInstructions(_addr, (int)iInstructions+1);
            });
            if (instructions == null)
            {
                // bad address range, no instructions. 
                _addr = (ulong)((long)_addr + iInstructions);  // forward iInstructions bytes
                return Constants.S_OK;
            }
            _addr = instructions.Last().Addr;
            if (instructions.Count < iInstructions)
            {
                // not enough instructions were fetched; forward one byte for each missing instruction
                _addr += (ulong)(iInstructions - instructions.Count);   // TODO: length of last instruction is unknown and not accounted for
            }
            return Constants.S_OK;
        }

        private int SeekBack(long iInstructions)
        {
            _engine.DebuggedProcess.WorkerThread.RunOperation(async () =>
            {
                _addr = await _engine.DebuggedProcess.Disassembly.SeekBack(_addr, (int)iInstructions);
            });
            return Constants.S_OK;
        }

        public int Seek(enum_SEEK_START dwSeekStart, IDebugCodeContext2 pCodeContext, ulong uCodeLocationId, long iInstructions)
        {
            _pLastDocument = null;
            m_lastSourceInfoStale = true;
            m_dwLastSourceLine = -1;

            if (dwSeekStart == enum_SEEK_START.SEEK_START_CODECONTEXT)
            {
                AD7MemoryAddress addr = pCodeContext as AD7MemoryAddress;
                _addr = addr.Address;
            }
            else if (dwSeekStart == enum_SEEK_START.SEEK_START_CODELOCID)
            {
                _addr = uCodeLocationId;
            }

            if (iInstructions >= 0)
            {
                return SeekForward(iInstructions);
            }
            else
            {
                return SeekBack(-iInstructions);
            }
        }
        #endregion
    }
}
