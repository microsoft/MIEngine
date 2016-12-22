// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.MIDebugEngine
{
    // This class represents a breakpoint that has been bound to a location in the debuggee. It is a child of the pending breakpoint
    // that creates it. Unless the pending breakpoint only has one bound breakpoint, each bound breakpoint is displayed as a child of the
    // pending breakpoint in the breakpoints window. Otherwise, only one is displayed.
    internal class AD7BoundBreakpoint : IDebugBoundBreakpoint2
    {
        private AD7PendingBreakpoint _pendingBreakpoint;
        private AD7BreakpointResolution _breakpointResolution;
        private AD7Engine _engine;
        private BoundBreakpoint _bp;

        private bool _deleted;

        internal bool Enabled
        {
            get
            {
                return _bp.Enabled;
            }
            set
            {
                _bp.Enabled = value;
            }
        }

        internal bool Deleted { get { return _deleted; } }
        internal ulong Addr
        {
            get
            { return _bp.Addr; }
        }
        internal AD7PendingBreakpoint PendingBreakpoint { get { return _pendingBreakpoint; } }
        internal bool IsDataBreakpoint { get { return PendingBreakpoint.IsDataBreakpoint; } }

        public AD7BoundBreakpoint(AD7Engine engine, AD7PendingBreakpoint pendingBreakpoint, AD7BreakpointResolution breakpointResolution, BoundBreakpoint bp)
        {
            _engine = engine;
            _pendingBreakpoint = pendingBreakpoint;
            _breakpointResolution = breakpointResolution;
            _deleted = false;
            _bp = bp;
        }

        #region IDebugBoundBreakpoint2 Members

        // Called when the breakpoint is being deleted by the user.
        int IDebugBoundBreakpoint2.Delete()
        {
            return Delete();
        }

        //called by the AD7 Entry Point and by the Detach code path to clean up breakpoints on detach
        public int Delete()
        {
            if (!_deleted)
            {
                _deleted = true;
            }

            return Constants.S_OK;
        }

        // Called by the debugger UI when the user is enabling or disabling a breakpoint.
        int IDebugBoundBreakpoint2.Enable(int fEnable)
        {
            Enabled = fEnable == 0 ? false : true;
            return Constants.S_OK;
        }

        // Return the breakpoint resolution which describes how the breakpoint bound in the debuggee.
        int IDebugBoundBreakpoint2.GetBreakpointResolution(out IDebugBreakpointResolution2 ppBPResolution)
        {
            ppBPResolution = _breakpointResolution;
            return Constants.S_OK;
        }

        // Return the pending breakpoint for this bound breakpoint.
        int IDebugBoundBreakpoint2.GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBreakpoint)
        {
            ppPendingBreakpoint = _pendingBreakpoint;
            return Constants.S_OK;
        }

        // 
        int IDebugBoundBreakpoint2.GetState(enum_BP_STATE[] pState)
        {
            pState[0] = enum_BP_STATE.BPS_NONE;

            if (_deleted)
            {
                pState[0] = enum_BP_STATE.BPS_DELETED;
            }
            else if (Enabled)
            {
                pState[0] = enum_BP_STATE.BPS_ENABLED;
            }
            else if (!Enabled)
            {
                pState[0] = enum_BP_STATE.BPS_DISABLED;
            }

            return Constants.S_OK;
        }

        // The sample engine does not support hit counts on breakpoints. A real-world debugger will want to keep track 
        // of how many times a particular bound breakpoint has been hit and return it here.
        int IDebugBoundBreakpoint2.GetHitCount(out uint pdwHitCount)
        {
            pdwHitCount = _bp.HitCount;
            return Constants.S_OK;
        }

        int IDebugBoundBreakpoint2.SetCondition(BP_CONDITION bpCondition)
        {
            return ((IDebugPendingBreakpoint2)_pendingBreakpoint).SetCondition(bpCondition);  // setting on the pending break will set the condition
        }

        // The sample engine does not support hit counts on breakpoints. A real-world debugger will want to keep track 
        // of how many times a particular bound breakpoint has been hit. The debugger calls SetHitCount when the user 
        // resets a breakpoint's hit count.
        int IDebugBoundBreakpoint2.SetHitCount(uint dwHitCount)
        {
            throw new NotImplementedException();
        }

        // The sample engine does not support pass counts on breakpoints.
        // This is used to specify the breakpoint hit count condition.
        int IDebugBoundBreakpoint2.SetPassCount(BP_PASSCOUNT bpPassCount)
        {
            if (bpPassCount.stylePassCount != enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_NONE)
            {
                Delete();
                _engine.Callback.OnBreakpointUnbound(this, enum_BP_UNBOUND_REASON.BPUR_BREAKPOINT_ERROR);
                return Constants.E_FAIL;
            }
            return Constants.S_OK;
        }

        #endregion

        internal void UpdateAddr(ulong addr)
        {
            _bp.Addr = addr;
            _breakpointResolution.Addr = addr;
            if (!_deleted)
            {
                _engine.Callback.OnBreakpointBound(this);
            }
        }
    }
}
