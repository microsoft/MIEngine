// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DebugEngineHost;
using Microsoft.VisualStudio.Debugger.Interop;
using System;
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
        private enum_BP_PASSCOUNT_STYLE _passCountStyle;
        private uint _passCountValue;

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
        internal ulong Addr { get { return _bp.Addr; } }
        internal string Number { get { return _bp.Number; } }
        internal AD7PendingBreakpoint PendingBreakpoint { get { return _pendingBreakpoint; } }
        internal bool IsDataBreakpoint { get { return PendingBreakpoint.IsDataBreakpoint; } }
        internal bool HasPassCount { get { return _passCountStyle != enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_NONE; } }

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
            if (Delete() == Constants.S_OK)
            {
                if (this.IsDataBreakpoint)
                {
                    lock (_engine.DebuggedProcess.DataBreakpointVariables)
                    {
                        string addressId = _pendingBreakpoint.AddressId;
                        if (addressId != null)
                        {
                            Debug.Assert(_engine.DebuggedProcess.DataBreakpointVariables.Contains(addressId));
                            _engine.DebuggedProcess.DataBreakpointVariables.Remove(addressId);
                        }
                    }
                }
                return Constants.S_OK;
            }
            return Constants.E_FAIL;
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
            if (this.IsDataBreakpoint)
            {
                lock (_engine.DebuggedProcess.DataBreakpointVariables)
                {
                    string addressId = _pendingBreakpoint.AddressId;
                    if (addressId != null)
                    {
                        bool InDataBreakpointVariables = _engine.DebuggedProcess.DataBreakpointVariables.Contains(addressId);
                        if (Enabled && !InDataBreakpointVariables)
                        {
                            _engine.DebuggedProcess.DataBreakpointVariables.Add(addressId);
                        }
                        else if (!Enabled && InDataBreakpointVariables)
                        {
                            _engine.DebuggedProcess.DataBreakpointVariables.Remove(addressId);
                        }
                    }
                }
            }
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

        // Returns the number of times this breakpoint has been hit.
        int IDebugBoundBreakpoint2.GetHitCount(out uint pdwHitCount)
        {
            pdwHitCount = _bp.HitCount;
            return Constants.S_OK;
        }

        int IDebugBoundBreakpoint2.SetCondition(BP_CONDITION bpCondition)
        {
            return ((IDebugPendingBreakpoint2)_pendingBreakpoint).SetCondition(bpCondition);  // setting on the pending break will set the condition
        }

        // Called by the debugger when the user resets a breakpoint's hit count.
        int IDebugBoundBreakpoint2.SetHitCount(uint dwHitCount)
        {
            _bp.SetHitCount(dwHitCount);
            _pendingBreakpoint?.RecomputeBreakAfter(dwHitCount);

            return Constants.S_OK;
        }

        /// <summary>
        /// Syncs the hit count from GDB's "times" field using a delta
        /// to preserve any user-initiated hit count reset.
        /// </summary>
        internal void SetHitCount(uint hitCount)
        {
            _bp.SetGdbHitCount(hitCount);
        }

        // This is used to specify the breakpoint hit count condition.
        int IDebugBoundBreakpoint2.SetPassCount(BP_PASSCOUNT bpPassCount)
        {
            _passCountStyle = bpPassCount.stylePassCount;
            _passCountValue = bpPassCount.dwPassCount;
            return Constants.S_OK;
        }

        #endregion

        internal uint HitCount => _bp.HitCount;

        internal void IncrementHitCount()
        {
            _bp.IncrementHitCount();
        }

        /// <summary>
        /// Evaluates whether the debugger should break at this breakpoint based on the
        /// current hit count and the configured pass count condition.
        /// Must be called after IncrementHitCount.
        /// </summary>
        internal bool ShouldBreak()
        {
            uint hitCount = _bp.HitCount;
            switch (_passCountStyle)
            {
                case enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_NONE:
                    return true;
                case enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_EQUAL:
                    return hitCount == _passCountValue;
                case enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_EQUAL_OR_GREATER:
                    return hitCount >= _passCountValue;
                case enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_MOD:
                    return _passCountValue != 0 && (hitCount % _passCountValue) == 0;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Re-sends -break-after to GDB after a pass count breakpoint fires.
        /// MOD: skips passCount-1 hits. EQUAL: clears the ignore count.
        /// </summary>
        internal async Task RearmBreakAfterAsync()
        {
            uint ignoreCount;
            switch (_passCountStyle)
            {
                case enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_MOD:
                    if (_passCountValue == 0) return;
                    ignoreCount = _passCountValue - 1;
                    break;
                case enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_EQUAL:
                    ignoreCount = 0;
                    break;
                default:
                    return;
            }

            PendingBreakpoint bp = _pendingBreakpoint?.PendingBreakpoint;
            if (bp != null && _engine?.DebuggedProcess != null)
            {
                await bp.SetBreakAfterAsync(ignoreCount, _engine.DebuggedProcess);
            }
        }

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
