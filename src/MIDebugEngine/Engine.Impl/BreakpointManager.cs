// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using MICore;

namespace Microsoft.MIDebugEngine
{
    // This class manages breakpoints for the engine. 
    internal class BreakpointManager
    {
        private AD7Engine _engine;
        private System.Collections.Generic.List<AD7PendingBreakpoint> _pendingBreakpoints;
        public const ulong INVALID_ADDRESS = 0xffffffffffffffff;

        public BreakpointManager(AD7Engine engine)
        {
            _engine = engine;
            _pendingBreakpoints = new System.Collections.Generic.List<AD7PendingBreakpoint>();
        }

        private List<AD7PendingBreakpoint> CodeBreakpoints
        {
            get
            {
                lock (_pendingBreakpoints)
                {
                    return _pendingBreakpoints.FindAll((b) => !b.IsDataBreakpoint).ToList();
                }
            }
        }

        private List<AD7PendingBreakpoint> DataBreakpoints
        {
            get
            {
                lock (_pendingBreakpoints)
                {
                    return _pendingBreakpoints.FindAll((b) => b.IsDataBreakpoint).ToList();
                }
            }
        }

        /// <summary>
        /// Handle a modified breakpoint event. Only handles address changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public async Task BreakpointModified(object sender, EventArgs args)
        {
            MICore.Debugger.ResultEventArgs res = args as MICore.Debugger.ResultEventArgs;
            MICore.ResultValue bkpt = res.Results.Find("bkpt");
            string bkptId = null;
            //
            // =breakpoint-modified,
            //    bkpt ={number="2",type="breakpoint",disp="keep",enabled="y",addr="<MULTIPLE>",times="0",original-location="main.cpp:220"},
            //          { number="2.1",enabled="y",addr="0x9c2149a9",func="Foo::bar<int>(int)",file="main.cpp",fullname="C:\\\\...\\\\main.cpp",line="220",thread-groups=["i1"]},
            //          { number="2.2",enabled="y",addr="0x9c2149f2",func="Foo::bar<float>(float)",file="main.cpp",fullname="C:\\\\...\\\\main.cpp",line="220",thread-groups=["i1"]}
            // note: the ".x" part of the breakpoint number never appears in stopping events, that is, when executing at one of these addresses 
            //       the stopping event delivered contains bkptno="2"
            if (bkpt is MICore.ValueListValue)
            {
                MICore.ValueListValue list = bkpt as MICore.ValueListValue;
                bkptId = list.Content[0].FindString("number"); // 0 is the "<MULTIPLE>" entry
            }
            else
            {
                bkptId = bkpt.FindString("number");
            }
            AD7PendingBreakpoint pending = CodeBreakpoints.FirstOrDefault((p) => { return p.BreakpointId == bkptId; });
            if (pending == null)
            {
                return;
            }

            string warning = bkpt.TryFindString("warning");
            if (!string.IsNullOrEmpty(warning))
            {
                pending.SetError(new AD7ErrorBreakpoint(pending, warning), true);
            }
            else
            {
                var bindList = await pending.PendingBreakpoint.BindAddresses(bkpt);
                RebindAddresses(pending, bindList);
            }
        }

        public async Task BindAsync()
        {
            var breakpointsToBind = CodeBreakpoints.Where((b) => b.PendingBreakpoint != null && b.PendingBreakpoint.IsPending);

            if (breakpointsToBind != null)
            {
                foreach (var b in breakpointsToBind)
                {
                    var bpList = await b.PendingBreakpoint.SyncBreakpoint(_engine.DebuggedProcess);
                    RebindAddresses(b, bpList);
                }
            }
        }

        private void RebindAddresses(AD7PendingBreakpoint pending, List<BoundBreakpoint> boundList)
        {
            if (boundList.Count == 0)
            {
                return;
            }
            var bkpt = Array.Find(pending.EnumBoundBreakpoints(), (b) => b.Addr == 0);
            int i = 0;
            if (bkpt != null)
            {
                bkpt.UpdateAddr(boundList[0].Addr);     // replace <MULTIPLE> placeholder address
                i = 1;
            }
            for (; i < boundList.Count; ++i)
            {
                pending.AddBoundBreakpoint(boundList[i]);
            }
        }

        // A helper method used to construct a new pending breakpoint.
        public void CreatePendingBreakpoint(IDebugBreakpointRequest2 pBPRequest, out IDebugPendingBreakpoint2 ppPendingBP)
        {
            AD7PendingBreakpoint pendingBreakpoint = new AD7PendingBreakpoint(pBPRequest, _engine, this);
            ppPendingBP = (IDebugPendingBreakpoint2)pendingBreakpoint;
            lock (_pendingBreakpoints)
            {
                _pendingBreakpoints.Add(pendingBreakpoint);
            }
        }

        // Called from the engine's detach method to remove the debugger's breakpoint instructions.
        public void ClearBoundBreakpoints()
        {
            lock (_pendingBreakpoints)
            {
                foreach (AD7PendingBreakpoint pendingBreakpoint in _pendingBreakpoints)
                {
                    pendingBreakpoint.ClearBoundBreakpoints();
                }
            }
        }

        private AD7PendingBreakpoint BindToAddress(string bkptno, ulong addr, /*OPTIONAL*/ TupleValue frame, out AD7BoundBreakpoint bbp)
        {
            bbp = null;
            AD7PendingBreakpoint pending = CodeBreakpoints.FirstOrDefault((p) => { return p.BreakpointId == bkptno; });
            if (pending == null)
            {
                return null;
            }
            // the breakpoint number is known, check to see if it is known to be bound to this address
            bbp = Array.Find(pending.EnumBoundBreakpoints(), (b) => b.Addr == addr);
            if (bbp == null)
            {
                // add this address as a bound breakpoint
                bbp = Array.Find(pending.EnumBoundBreakpoints(), (b) => b.Addr == 0);
                if (bbp != null)    // <MULTIPLE>
                {
                    bbp.UpdateAddr(addr);
                }
                else
                {
                    bbp = pending.AddBoundBreakpoint(new BoundBreakpoint(pending.PendingBreakpoint, addr, frame, bkptno));
                }
            }
            return pending;
        }

        // Return all bound breakpoints bound to an address including the one withe matching bptno. 
        // Note that there are still cases where gdb won't return the address for a breakpoint 
        // (breakpoint that binds to multiple locations) in which case the bktpno is the best match
        // the engine can do
        private AD7BoundBreakpoint[] FindBoundBreakpointsAtAddress(string bkptno, ulong addr, /*OPTIONAL*/ TupleValue frame)
        {
            // Add all bound bps whose address match
            List<AD7BoundBreakpoint> matchingBoundBreakpoints = new List<AD7BoundBreakpoint>();
            foreach (AD7PendingBreakpoint currPending in CodeBreakpoints)
            {
                matchingBoundBreakpoints.AddRange(Array.FindAll(currPending.EnumBoundBreakpoints(), (b) => b.Addr != 0 && b.Addr == addr));
            }

            // Include the bp whose bkptno matches
            AD7BoundBreakpoint bbp;
            BindToAddress(bkptno, addr, frame, out bbp);
            if (bbp != null)
            {
                matchingBoundBreakpoints.Add(bbp);
            }

            return matchingBoundBreakpoints.Distinct().ToArray();
        }


        public AD7BoundBreakpoint[] FindHitBreakpoints(string bkptno, ulong addr, /*OPTIONAL*/ TupleValue frame, out bool fContinue)
        {
            fContinue = false;
            List<AD7BoundBreakpoint> hitBoundBreakpoints = new List<AD7BoundBreakpoint>();
            // match based on address and bkptno since there are many
            // cases where gdb doesn't provide the breakpoint address (multple binds to the same location)
            // This will cause the engine to send a breakpoint event accounting for all known breakpoints
            // that are hit. 
            // Clrdbg does not support addresses on its breakpoints, so the bkptno code path is the only 
            // supported path in that case
            AD7BoundBreakpoint[] hitBps = FindBoundBreakpointsAtAddress(bkptno, addr, frame);

            foreach (AD7BoundBreakpoint currBoundBp in hitBps)
            {
                if (!currBoundBp.Enabled || currBoundBp.Deleted)
                {
                    continue;
                }

                if (!currBoundBp.PendingBreakpoint.Enabled || currBoundBp.PendingBreakpoint.Deleted || currBoundBp.PendingBreakpoint.PendingDelete)
                {
                    continue;
                }

                hitBoundBreakpoints.Add(currBoundBp);
            }

            fContinue = (hitBoundBreakpoints.Count == 0 && hitBps.Length != 0);
            return hitBoundBreakpoints.Count != 0 ? hitBoundBreakpoints.ToArray() : null;
        }

        public AD7BoundBreakpoint FindHitWatchpoint(string bkptno, out bool fContinue)
        {
            fContinue = false;
            var pending = DataBreakpoints.FirstOrDefault((b) => b.BreakpointId == bkptno);
            if (pending == null)
            {
                return null;
            }
            var bound = pending.EnumBoundBreakpoints().FirstOrDefault();
            if (bound == null)
            {
                return null;
            }
            //
            // return fContinue == true if this watchpoint has been disabled or deleted
            //
            fContinue = true;
            if (!bound.Enabled || bound.Deleted)
            {
                return null;
            }

            if (!pending.Enabled || pending.Deleted || pending.PendingDelete)
            {
                return null;
            }
            // should stop at this watchpoint
            fContinue = false;
            return bound;
        }

        public async Task DeleteBreakpointsPendingDeletion()
        {
            //push all of the pending breakpoints to delete into a new list so that we avoid iterator invalidation
            List<AD7PendingBreakpoint> breakpointsPendingDeletion = null;

            lock (_pendingBreakpoints)
            {
                breakpointsPendingDeletion = _pendingBreakpoints.FindAll((b) => b.PendingDelete);
                _pendingBreakpoints.RemoveAll((b) => b.PendingDelete);
            }

            if (breakpointsPendingDeletion != null)
            {
                foreach (var b in breakpointsPendingDeletion)
                {
                    await b.DeletePendingDelete();
                }
            }
        }

        internal async Task DisableBreakpointsForFuncEvalAsync()
        {
            foreach (var pending in _pendingBreakpoints)
            {
                await pending.DisableForFuncEvalAsync();
            }
        }

        internal async Task EnableAfterFuncEvalAsync()
        {
            foreach (var pending in _pendingBreakpoints)
            {
                await pending.EnableAfterFuncEvalAsync();
            }
        }
    }
}
