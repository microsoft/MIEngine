// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Loyc.Collections;
using System;
using static System.FormattableString;
using System.Globalization;

namespace OpenDebugAD7
{
    internal class TextPositionTuple
    {
        public readonly Source Source;
        public readonly int Line;
        public readonly int Column;
        public static readonly TextPositionTuple Nil = new TextPositionTuple(null, 0, 0);
        private static BDictionary<ulong, DisassembledRange> DisasmRangesDict = new BDictionary<ulong, DisassembledRange>();
        internal struct DisassembledRange
        {
            internal int sourceReference; // for the IDE
            internal ulong endAddress;
            internal DisassemblyData[] dasmData;
        }
        private static int lastSourceReference;

        private TextPositionTuple(Source source, int line, int column)
        {
            this.Source = source;
            this.Line = line;
            this.Column = column;
        }

        public static TextPositionTuple GetTextPositionOfFrame(PathConverter converter, IDebugStackFrame2 frame)
        {
            IDebugDocumentContext2 documentContext;
            var source = new Source();
            if (frame.GetDocumentContext(out documentContext) == 0 &&
                documentContext != null)
            {
                var beginPosition = new TEXT_POSITION[1];
                var endPosition   = new TEXT_POSITION[1];
                documentContext.GetStatementRange(beginPosition, endPosition);

                string filePath;
                documentContext.GetName(enum_GETNAME_TYPE.GN_FILENAME, out filePath);

                string convertedFilePath = converter.ConvertDebuggerPathToClient(filePath);
                // save the filename as we need it in any case
                source.Name = Path.GetFileName(convertedFilePath);

                // we have debug info but do we actually have the source code?
                if (File.Exists(convertedFilePath))
                {
                    source.Path = convertedFilePath;
                    int srcline = converter.ConvertDebuggerLineToClient((int)beginPosition[0].dwLine);
                    int srccolumn = unchecked((int)(beginPosition[0].dwColumn + 1));

                    return new TextPositionTuple(source, srcline, srccolumn);
                }
            }

            // this frame is lacking source code
            source.Name = Path.ChangeExtension(source.Name, ".s") ?? "???";
            source.Path = null;
            source.Origin = "disassembly";
            ulong startAddress;

            IDebugCodeContext2 codeContext;
            if (frame.GetCodeContext(out codeContext) != HRConstants.S_OK)
                return null; // we couldn't even find the current instruction, something's really wrong

            var cinfos = new CONTEXT_INFO[1];
            codeContext.GetInfo(enum_CONTEXT_INFO_FIELDS.CIF_ALLFIELDS, cinfos);

            DisassembledRange disRange;
            startAddress = (ulong)Convert.ToInt64(cinfos[0].bstrAddress, 16);

            int idx = DisasmRangesDict.FindUpperBound(startAddress);
            bool foundIt = false;
            int column = 0;
            int line = 0;
            if (idx > 0)
            {
                var kv = DisasmRangesDict.TryGet(idx - 1).Value;
                if (startAddress >= kv.Key && startAddress <= kv.Value.endAddress)
                {
                    disRange = kv.Value;
                    source.SourceReference = disRange.sourceReference;
                    line = kv.Value.dasmData.FirstIndexWhere(d => (ulong)Convert.ToInt64(d.bstrAddress, 16) >= startAddress) ?? 0;
                    ++line;
                    column = 0;
                    foundIt = true;
                }
            }

            if (!foundIt)
            {
                IDebugThread2 thread;
                frame.GetThread(out thread);
                IDebugProgram2 program;
                thread.GetProgram(out program);
                IDebugDisassemblyStream2 disasmStream;
                program.GetDisassemblyStream(enum_DISASSEMBLY_STREAM_SCOPE.DSS_FUNCTION, codeContext, out disasmStream);
                uint instructionsRead;
                var dasmData = new DisassemblyData[100];
                if (disasmStream.Read(100, enum_DISASSEMBLY_STREAM_FIELDS.DSF_ALL, out instructionsRead, dasmData) == HRConstants.S_OK)
                {
                    System.Array.Resize(ref dasmData, (int)instructionsRead);
                    source.SourceReference = ++lastSourceReference;
                    DisasmRangesDict.Add(startAddress, new DisassembledRange() {
                        sourceReference = lastSourceReference,
                        endAddress = (ulong)Convert.ToInt64(dasmData.Last().bstrAddress, 16),
                        dasmData = dasmData });
                    line = 1;
                    column = 0;
                }
            }

            return new TextPositionTuple(source, line, column);
        }

        public static string GetSourceForRef(int srcRef)
        {
            foreach (var kv in DisasmRangesDict)
            {
                if (kv.Value.sourceReference == srcRef)
                {
                    return SourceForDasmData(kv.Value.dasmData);
                }
            }
            return null;
        }

        private static string SourceForDasmData(DisassemblyData[] dasmData)
        {
            string res = "";
            foreach (var d in dasmData)
            {
                if ((d.dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_ADDRESS) != 0)
                    res += d.bstrAddress + '\t';
                if ((d.dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_ADDRESSOFFSET) != 0)
                    res += d.bstrAddressOffset + '\t';
                if ((d.dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_CODEBYTES) != 0)
                    res += d.bstrCodeBytes.PadRight(25) + '\t';
                if ((d.dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPCODE) != 0)
                    res += d.bstrOpcode.PadRight(50) + '\t';
                if ((d.dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_OPERANDS) != 0)
                    res += d.bstrOperands.PadRight(20) + '\t';
                if ((d.dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_SYMBOL) != 0)
                    res += d.bstrSymbol + '\t';
                if ((d.dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_POSITION) != 0)
                    res += Invariant($"{d.posBeg.dwLine}:{d.posBeg.dwColumn}-{d.posEnd.dwLine}:{d.posEnd.dwColumn}") + '\t';
                if ((d.dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_DOCUMENTURL) != 0)
                    res += d.bstrDocumentUrl + '\t';
                if ((d.dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_BYTEOFFSET) != 0)
                    res += d.dwByteOffset.ToString(CultureInfo.InvariantCulture) + '\t';
                if ((d.dwFields & enum_DISASSEMBLY_STREAM_FIELDS.DSF_FLAGS) != 0)
                    res += d.dwFlags;
                res += '\n';
            }
            return res;
        }
    }
}
