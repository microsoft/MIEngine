// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Collections.Generic;
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

        /// <summary>
        /// Handle a modified breakpoint event. Only handles address changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void BreakpointModified(object sender, EventArgs args)
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
            AD7PendingBreakpoint pending = _pendingBreakpoints.Find((p) => { return p.BreakpointId == bkptId; });
            if (pending == null)
            {
                return;
            }
            var bindList = pending.PendingBreakpoint.BindAddresses(bkpt);
            RebindAddresses(pending, bindList);
        }

        public async Task BindAsync()
        {
            List<AD7PendingBreakpoint> breakpointsToBind;
            lock (_pendingBreakpoints)
            {
                breakpointsToBind = _pendingBreakpoints.FindAll((b) => b.PendingBreakpoint != null && b.PendingBreakpoint.IsPending);
            }

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
            _pendingBreakpoints.Add(pendingBreakpoint);
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
            AD7PendingBreakpoint pending = _pendingBreakpoints.Find((p) => { return p.BreakpointId == bkptno; });
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
                    bbp = pending.AddBoundBreakpoint(new BoundBreakpoint(pending.PendingBreakpoint, addr, frame));
                }
            }
            return pending;
        }

        public AD7BoundBreakpoint FindHitBreakpoint(string bkptno, ulong addr, /*OPTIONAL*/ TupleValue frame, out bool fContinue)
        {
            fContinue = false;
            lock (_pendingBreakpoints)
            {
                AD7BoundBreakpoint bbp;
                AD7PendingBreakpoint pending = BindToAddress(bkptno, addr, frame, out bbp);
                if (pending == null)
                {
                    return null;
                }
                fContinue = true;
                if (!pending.Enabled || pending.Deleted || pending.PendingDelete)
                {
                    return null;
                }
                if (!bbp.Enabled || bbp.Deleted)
                {
                    return null;
                }
                fContinue = false;
                return bbp;
            }
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
