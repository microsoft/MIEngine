﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using MICore;
using System.Globalization;
using System.IO;

namespace Microsoft.MIDebugEngine
{
    internal enum MIBreakpointState
    {
        Single,     // bound to a single address
        Multiple,   // bound to multiple addresses
        Pending     // not yet bound to an address
    }

    internal class PendingBreakpoint
    {
        internal readonly string Number;      // how mi tracks this

        internal AD7PendingBreakpoint AD7breakpoint { get; private set; }     // UI level breakpoint object
        private MIBreakpointState _breakState;    // how MI reported this breakpoint address

        public bool IsMultiple { get { return _breakState == MIBreakpointState.Multiple; } }
        public bool IsPending { get { return _breakState == MIBreakpointState.Pending; } }
        public DebuggedProcess DebuggedProcess { get { return AD7breakpoint.DebuggedProcess; } }

        internal PendingBreakpoint(AD7PendingBreakpoint pbreak, string number, MIBreakpointState state)
        {
            AD7breakpoint = pbreak;
            Number = number;
            _breakState = state;
        }

        internal class BindResult
        {
            public PendingBreakpoint PendingBreakpoint;
            public readonly string ErrorMessage;
            private List<BoundBreakpoint> _boundBreakpoints;

            public BindResult(PendingBreakpoint bp)
            {
                PendingBreakpoint = bp;
                _boundBreakpoints = new List<BoundBreakpoint>();
            }

            public BindResult(PendingBreakpoint bp, BoundBreakpoint boundBreakpoint)
                : this(bp)
            {
                _boundBreakpoints.Add(boundBreakpoint);
            }

            public BindResult(PendingBreakpoint bp, string errorMessage)
            {
                PendingBreakpoint = bp;
                ErrorMessage = errorMessage;
            }

            public BindResult(string errorMessage)
                : this(null, errorMessage)
            {
            }

            /// <summary>
            /// [Optional] bound breakpoints
            /// </summary>
            public List<BoundBreakpoint> BoundBreakpoints
            {
                get { return _boundBreakpoints; }
            }
        }

        private static MIBreakpointState StringToBreakpointState(string addr)
        {
            switch (addr)
            {
                case "<MULTIPLE>": return MIBreakpointState.Multiple;
                case "<PENDING>": return MIBreakpointState.Pending;
                case "0xffffffffffffffff": return MIBreakpointState.Pending;    // lldb-mi returns an invalid address for pending breakpoints
                default: return MIBreakpointState.Single;
            }
        }

        internal static async Task<BindResult> Bind(string functionName, DebuggedProcess process, string condition, bool enabled, AD7PendingBreakpoint pbreak)
        {
            process.VerifyNotDebuggingCoreDump();

            return await EvalBindResult(await process.MICommandFactory.BreakInsert(functionName, condition, enabled, ResultClass.None), pbreak);
        }

        internal static async Task<BindResult> Bind(ulong codeAddress, DebuggedProcess process, string condition, bool enabled, AD7PendingBreakpoint pbreak)
        {
            process.VerifyNotDebuggingCoreDump();

            return await EvalBindResult(await process.MICommandFactory.BreakInsert(codeAddress, condition, enabled, ResultClass.None), pbreak);
        }

        internal static async Task<BindResult> Bind(string address, uint size, DebuggedProcess process, string condition, AD7PendingBreakpoint pbreak)
        {
            process.VerifyNotDebuggingCoreDump();

            return EvalBindWatchResult(await process.MICommandFactory.BreakWatch(address, size, ResultClass.None), pbreak, address, size);
        }

        internal static async Task<BindResult> Bind(string documentName, uint line, uint column, DebuggedProcess process, string condition, bool enabled, IEnumerable<Checksum> checksums, AD7PendingBreakpoint pbreak)
        {
            process.VerifyNotDebuggingCoreDump();

            string compilerSrcName;
            if (!process.MapCurrentSrcToCompileTimeSrc(documentName, out compilerSrcName))
            {
                compilerSrcName = Path.GetFileName(documentName);   
            }
            return await EvalBindResult(await process.MICommandFactory.BreakInsert(compilerSrcName, process.UseUnixSymbolPaths, line, condition, enabled, checksums, ResultClass.None), pbreak);
        }

        private static async Task<BindResult> EvalBindResult(Results bindResult, AD7PendingBreakpoint pbreak)
        {
            string errormsg = "Unknown error";
            if (bindResult.ResultClass == ResultClass.error)
            {
                if (bindResult.Contains("msg"))
                {
                    errormsg = bindResult.FindString("msg");
                }
                if (String.IsNullOrWhiteSpace(errormsg))
                {
                    errormsg = "Unknown error";
                }
                return new BindResult(errormsg);
            }
            else if (bindResult.ResultClass != ResultClass.done)
            {
                return new BindResult(errormsg);
            }
            TupleValue bkpt = null;
            ValueListValue list = null;
            if (bindResult.Contains("bkpt"))
            {
                ResultValue b = bindResult.Find("bkpt");
                if (b is TupleValue)
                {
                    bkpt = b as TupleValue;
                    // The locations field is present if the breakpoint has multiple locations.
                    if (b.TryFind("locations", out var locations) && locations is ValueListValue)
                    {
                        list = (ValueListValue) locations;
                    }
                }
            }
            else
            {
                // If the source file is not found, "done" is the result without a binding
                // (the error is sent via an "&" string and hence lost)
                return new BindResult(errormsg);
            }
            string bkptType = bkpt.FindString("type");

            // gdb reports breakpoint type "hw breakpoint" for `-break-insert -h` command
            Debug.Assert(bkptType == "breakpoint" || bkptType == "hw breakpoint");

            string number = bkpt.FindString("number");
            string warning = bkpt.TryFindString("warning");
            string addr = bkpt.TryFindString("addr");

            PendingBreakpoint bp;
            if (!string.IsNullOrEmpty(warning))
            {
                Debug.Assert(string.IsNullOrEmpty(addr));
                return new BindResult(new PendingBreakpoint(pbreak, number, MIBreakpointState.Pending), warning);
            }
            bp = new PendingBreakpoint(pbreak, number, StringToBreakpointState(addr));
            if (list == null)   // single breakpoint
            {
                BoundBreakpoint bbp = await bp.GetBoundBreakpoint(bkpt);

                if (bbp == null)
                {
                    return new BindResult(bp, MICoreResources.Status_BreakpointPending);
                }
                return new BindResult(bp, bbp);
            }
            else   // <MULTIPLE> with list of addresses
            {
                BindResult res = new BindResult(bp);
                foreach (var t in list.Content)
                {
                    BoundBreakpoint bbp = await bp.GetBoundBreakpoint(t as TupleValue);
                    res.BoundBreakpoints.Add(bbp);
                }
                return res;
            }
        }

        private static BindResult EvalBindWatchResult(Results bindResult, AD7PendingBreakpoint pbreak, string address, uint size)
        {
            string errormsg = "Unknown error";
            if (bindResult.ResultClass == ResultClass.error)
            {
                if (bindResult.Contains("msg"))
                {
                    errormsg = bindResult.FindString("msg");
                }
                if (String.IsNullOrWhiteSpace(errormsg))
                {
                    errormsg = "Unknown error";
                }
                return new BindResult(errormsg);
            }
            else if (bindResult.ResultClass != ResultClass.done)
            {
                return new BindResult(errormsg);
            }
            TupleValue bkpt = null;
            if (bindResult.Contains("wpt"))
            {
                ResultValue b = bindResult.Find("wpt");
                if (b is TupleValue)
                {
                    bkpt = b as TupleValue;
                }
            }
            else
            {
                return new BindResult(errormsg);
            }

            string number = bkpt.FindString("number");

            PendingBreakpoint bp = new PendingBreakpoint(pbreak, number, MIBreakpointState.Single);
            BoundBreakpoint bbp = new BoundBreakpoint(bp, MICore.Debugger.ParseAddr(address), size, number);
            return new BindResult(bp, bbp);
        }


        /// <summary>
        /// Decode the mi results and create a bound breakpoint from it. 
        /// </summary>
        /// <param name="bkpt">breakpoint description</param>
        /// <returns>null if breakpoint is pending</returns>
        private async Task<BoundBreakpoint> GetBoundBreakpoint(TupleValue bkpt)
        {
            string addrString = bkpt.TryFindString("addr");
            MIBreakpointState state = StringToBreakpointState(addrString);
            if (state == MIBreakpointState.Multiple)
            {
                // MI gives no way to find the set of addresses a breakpoint is bound to. So bind the breakpoint to address zero until hit. 
                // When the breakpoint is hit can rebind to actual address.
                bkpt.Content.RemoveAll((keyval) => { return keyval.Name == "addr"; });
                bkpt.Content.Add(new NamedResultValue("addr", new ConstValue("0")));
            }
            else if (state == MIBreakpointState.Pending)
            {
                return null;
            }
            else if (!string.IsNullOrEmpty(addrString) && bkpt.FindAddr("addr") == BreakpointManager.INVALID_ADDRESS)
            {
                return null;
            }

            var bbp = new BoundBreakpoint(this, bkpt);

            // On GDB, the returned line information is from the pending breakpoint instead of the bound breakpoint. 
            // Check the address mapping to make sure the line info is correct. 
            if (this.DebuggedProcess.MICommandFactory.Mode == MIMode.Gdb && !string.IsNullOrEmpty(bbp.CompiledFileName))
            {
                bbp.Line =  await this.DebuggedProcess.LineForStartAddress(bbp.CompiledFileName, bbp.Addr);
            }

            return bbp;
        }

        internal async Task<List<BoundBreakpoint>> SyncBreakpoint(DebuggedProcess process)
        {
            ResultValue bkpt = await process.MICommandFactory.BreakInfo(Number);
            return await BindAddresses(bkpt);
        }

        internal async Task<List<BoundBreakpoint>> BindAddresses(ResultValue bkpt)
        {
            List<BoundBreakpoint> resultList = new List<BoundBreakpoint>();
            if (bkpt == null)
            {
                return resultList;
            }
            BoundBreakpoint bbp = null;

            // The locations field is present if the breakpoint has multiple locations.
            if (bkpt.TryFind("locations", out var locations) && locations is ValueListValue list)
            {
                foreach (var t in list.Content)
                {
                    bbp = await GetBoundBreakpoint(t as TupleValue);
                    if (bbp != null)
                        resultList.Add(bbp);
                }
            }
            else
            {
                _breakState = StringToBreakpointState(bkpt.TryFindString("addr"));
                bbp = await GetBoundBreakpoint(bkpt as TupleValue);
                if (bbp != null)
                    resultList.Add(bbp);
            }
            return resultList;
        }

        internal void AddedBoundBreakpoint()
        {
            if (_breakState == MIBreakpointState.Pending)
            {
                _breakState = MIBreakpointState.Single;
            }
        }

        internal async Task EnableAsync(bool bEnable, DebuggedProcess process)
        {
            await EnableInternal(bEnable, process);
        }

        private async Task EnableInternal(bool bEnable, DebuggedProcess process)
        {
            await process.MICommandFactory.BreakEnable(bEnable, Number);
        }

        internal void Delete(DebuggedProcess process)
        {
            process.WorkerThread.RunOperation(async () => await DeleteInternal(process));
        }

        internal async Task DeleteAsync(DebuggedProcess process)
        {
            await DeleteInternal(process);
        }

        private async Task DeleteInternal(DebuggedProcess process)
        {
            if (process.ProcessState != MICore.ProcessState.Exited)
            {
                await process.MICommandFactory.BreakDelete(Number);
            }
        }

        internal async Task SetConditionAsync(string expr, DebuggedProcess process)
        {
            if (process.ProcessState != MICore.ProcessState.Exited)
            {
                await process.MICommandFactory.BreakCondition(Number, expr);
            }
        }
    }

    internal class BoundBreakpoint
    {
        private PendingBreakpoint _parent;
        internal readonly string Number;
        internal ulong Addr { get; set; }
        /*OPTIONAL*/
        public string FunctionName { get; private set; }
        internal uint HitCount { get; private set; }
        internal bool Enabled { get; set; }
        internal bool IsDataBreakpoint { get { return _parent.AD7breakpoint.IsDataBreakpoint; } }
        private MITextPosition _textPosition;
        internal string CompiledFileName   { get; private set; }    // name as it appears in the debug symbols

        internal BoundBreakpoint(PendingBreakpoint parent, TupleValue bindinfo)
        {
            this.Addr = bindinfo.TryFindAddr("addr") ?? 0;
            this.FunctionName = bindinfo.TryFindString("func");
            this.Enabled = bindinfo.TryFindString("enabled") == "n" ? false : true;
            this.HitCount = 0;
            this.CompiledFileName = bindinfo.Contains("fullname") ? bindinfo.FindString("fullname") : bindinfo.TryFindString("file");
            this.Number = bindinfo.Contains("number") ? bindinfo.FindString("number") : parent.Number;
            _parent = parent;
            _textPosition = MITextPosition.TryParse(parent.DebuggedProcess, bindinfo);
        }

        internal BoundBreakpoint(PendingBreakpoint parent, ulong addr, /*optional*/ TupleValue frame, string bkptno)
        {
            Addr = addr;
            HitCount = 0;
            Enabled = true;
            this.Number = bkptno;
            _parent = parent;

            if (frame != null)
            {
                this.FunctionName = frame.TryFindString("func");
                _textPosition = MITextPosition.TryParse(parent.DebuggedProcess, frame);
            }
        }

        internal BoundBreakpoint(PendingBreakpoint parent, ulong addr, uint size, string bkptno)
        {
            Addr = addr;
            Enabled = true;
            this.Number = bkptno;
            _parent = parent;
        }

        internal AD7DocumentContext DocumentContext(AD7Engine engine)
        {
            if (_textPosition == null)
            {
                // get the document context from the original specification in the AD7 object
                return _parent.AD7breakpoint.GetDocumentContext(this.Addr, this.FunctionName);
            }

            return new AD7DocumentContext(_textPosition, new AD7MemoryAddress(engine, Addr, this.FunctionName));
        }

        /// <summary>
        /// Returns the start line of the breakpoint.
        /// NOTE: If set this overwrites any column or multiline information.
        /// </summary>
        internal uint Line
        {
            get
            {
                return _textPosition.BeginPosition.dwLine;
            }

            set
            {
                if (this.Line == value || value == 0)
                {
                    return;
                }
                _textPosition = new MITextPosition(_textPosition.FileName, value);
            }
        }
    }
}
