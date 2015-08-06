// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MICore;
using System.Diagnostics;

namespace Microsoft.MIDebugEngine
{
    // This class represents a pending breakpoint which is an abstract representation of a breakpoint before it is bound.
    // When a user creates a new breakpoint, the pending breakpoint is created and is later bound. The bound breakpoints
    // become children of the pending breakpoint.
    internal class AD7PendingBreakpoint : IDebugPendingBreakpoint2
    {
        // The breakpoint request that resulted in this pending breakpoint being created.
        private IDebugBreakpointRequest2 _pBPRequest;
        private BP_REQUEST_INFO _bpRequestInfo;
        private AD7Engine _engine;
        private BreakpointManager _bpManager;
        private PendingBreakpoint _bp; // non-null indicates MI breakpoint has been created
        private AD7ErrorBreakpoint _BPError;

        private List<AD7BoundBreakpoint> _boundBreakpoints;

        private bool _enabled;
        private bool _deleted;
        private bool _pendingDelete;

        internal string BreakpointId
        {
            get { return _bp == null ? string.Empty : _bp.Number; }
        }

        internal bool Enabled { get { return _enabled; } }
        internal bool Deleted { get { return _deleted; } }
        internal bool PendingDelete { get { return _pendingDelete; } }

        internal PendingBreakpoint PendingBreakpoint { get { return _bp; } }

        public AD7PendingBreakpoint(IDebugBreakpointRequest2 pBPRequest, AD7Engine engine, BreakpointManager bpManager)
        {
            _pBPRequest = pBPRequest;
            BP_REQUEST_INFO[] requestInfo = new BP_REQUEST_INFO[1];
            EngineUtils.CheckOk(_pBPRequest.GetRequestInfo(enum_BPREQI_FIELDS.BPREQI_BPLOCATION | enum_BPREQI_FIELDS.BPREQI_CONDITION | enum_BPREQI_FIELDS.BPREQI_PASSCOUNT, requestInfo));
            _bpRequestInfo = requestInfo[0];

            _engine = engine;
            _bpManager = bpManager;
            _boundBreakpoints = new List<AD7BoundBreakpoint>();

            _enabled = true;
            _deleted = false;
            _pendingDelete = false;

            _bp = null;    // no underlying breakpoint created yet
            _BPError = null;
        }

        private bool VerifyCondition(BP_CONDITION request)
        {
            switch (request.styleCondition)
            {
                case enum_BP_COND_STYLE.BP_COND_NONE:
                    return true;
                case enum_BP_COND_STYLE.BP_COND_WHEN_TRUE:
                    return request.bstrCondition != null;
                default:
                    return false;
            }
        }

        private bool CanBind()
        {
            // The sample engine only supports breakpoints on a file and line number. No other types of breakpoints are supported.
            if (_deleted || _bpRequestInfo.bpLocation.bpLocationType != (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE)
            {
                _BPError = new AD7ErrorBreakpoint(this, ResourceStrings.UnsupportedBreakpoint, enum_BP_ERROR_TYPE.BPET_GENERAL_ERROR);
                return false;
            }
            if ((_bpRequestInfo.dwFields & enum_BPREQI_FIELDS.BPREQI_CONDITION) != 0)
            {
                if (!VerifyCondition(_bpRequestInfo.bpCondition))
                {
                    _BPError = new AD7ErrorBreakpoint(this, ResourceStrings.UnsupportedConditionalBreakpoint, enum_BP_ERROR_TYPE.BPET_GENERAL_ERROR);
                    return false;
                }
            }
            if ((_bpRequestInfo.dwFields & enum_BPREQI_FIELDS.BPREQI_PASSCOUNT) != 0)
            {
                _BPError = new AD7ErrorBreakpoint(this, ResourceStrings.UnsupportedPassCountBreakpoint, enum_BP_ERROR_TYPE.BPET_GENERAL_ERROR);
                return false;
            }

            return true;
        }

        // Get the document context for this pending breakpoint. A document context is a abstract representation of a source file 
        // location.
        public AD7DocumentContext GetDocumentContext(ulong address, string functionName)
        {
            IDebugDocumentPosition2 docPosition = (IDebugDocumentPosition2)(Marshal.GetObjectForIUnknown(_bpRequestInfo.bpLocation.unionmember2));
            string documentName;
            EngineUtils.CheckOk(docPosition.GetFileName(out documentName));

            // Get the location in the document that the breakpoint is in.
            TEXT_POSITION[] startPosition = new TEXT_POSITION[1];
            TEXT_POSITION[] endPosition = new TEXT_POSITION[1];
            EngineUtils.CheckOk(docPosition.GetRange(startPosition, endPosition));

            AD7MemoryAddress codeContext = new AD7MemoryAddress(_engine, address, functionName);

            return new AD7DocumentContext(new MITextPosition(documentName, startPosition[0], startPosition[0]), codeContext);
        }

        // Remove all of the bound breakpoints for this pending breakpoint
        public void ClearBoundBreakpoints()
        {
            lock (_boundBreakpoints)
            {
                for (int i = _boundBreakpoints.Count - 1; i >= 0; i--)
                {
                    _boundBreakpoints[i].Delete();
                }
                _boundBreakpoints.Clear();
            }
        }

        #region IDebugPendingBreakpoint2 Members

        // Binds this pending breakpoint to one or more code locations.
        int IDebugPendingBreakpoint2.Bind()
        {
            try
            {
                if (CanBind())
                {
                    Task bindTask = null;

                    _engine.DebuggedProcess.WorkerThread.RunOperation(() =>
                    {
                        bindTask = _engine.DebuggedProcess.AddInternalBreakAction(this.BindAsync);
                    });

                    bindTask.Wait(250); //wait a quarter of a second

                    if (!bindTask.IsCompleted)
                    {
                        //send a low severity warning bp. This will allow the UI to respond quickly, and if the mi debugger doesn't end up binding, this warning will get 
                        //replaced by the real mi debugger error text
                        _BPError = new AD7ErrorBreakpoint(this, ResourceStrings.LongBind, enum_BP_ERROR_TYPE.BPET_SEV_LOW | enum_BP_ERROR_TYPE.BPET_TYPE_WARNING);
                        _engine.Callback.OnBreakpointError(_BPError);
                        return Constants.S_FALSE;
                    }
                    else
                    {
                        return Constants.S_OK;
                    }
                }
                else
                {
                    // The breakpoint could not be bound. This may occur for many reasons such as an invalid location, an invalid expression, etc...
                    _engine.Callback.OnBreakpointError(_BPError);
                    return Constants.S_FALSE;
                }
            }
            catch (MIException e)
            {
                return e.HResult;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        internal async Task BindAsync()
        {
            if (CanBind())
            {
                string documentName = null;
                TEXT_POSITION[] startPosition = new TEXT_POSITION[1];
                TEXT_POSITION[] endPosition = new TEXT_POSITION[1];
                string condition = null;

                lock (_boundBreakpoints)
                {
                    if (_bp != null)   // already bound
                    {
                        Debug.Fail("Breakpoint already bound");
                        return;
                    }
                    IDebugDocumentPosition2 docPosition = (IDebugDocumentPosition2)(Marshal.GetObjectForIUnknown(_bpRequestInfo.bpLocation.unionmember2));

                    // Get the name of the document that the breakpoint was put in
                    EngineUtils.CheckOk(docPosition.GetFileName(out documentName));

                    // Get the location in the document that the breakpoint is in.
                    EngineUtils.CheckOk(docPosition.GetRange(startPosition, endPosition));
                    if ((_bpRequestInfo.dwFields & enum_BPREQI_FIELDS.BPREQI_CONDITION) != 0
                        && _bpRequestInfo.bpCondition.styleCondition == enum_BP_COND_STYLE.BP_COND_WHEN_TRUE)
                    {
                        condition = _bpRequestInfo.bpCondition.bstrCondition;
                    }
                }

                // Bind all breakpoints that match this source and line number.
                PendingBreakpoint.BindResult bindResult = await PendingBreakpoint.Bind(documentName, startPosition[0].dwLine + 1, startPosition[0].dwColumn, _engine.DebuggedProcess, condition, this);

                lock (_boundBreakpoints)
                {
                    if (bindResult.PendingBreakpoint != null)
                    {
                        _bp = bindResult.PendingBreakpoint;    // an MI breakpoint object exists: TODO: lock?
                    }
                    if (bindResult.BoundBreakpoints == null || bindResult.BoundBreakpoints.Count == 0)
                    {
                        _BPError = new AD7ErrorBreakpoint(this, bindResult.ErrorMessage);
                        _engine.Callback.OnBreakpointError(_BPError);
                    }
                    else
                    {
                        Debug.Assert(_bp != null);
                        foreach (BoundBreakpoint bp in bindResult.BoundBreakpoints)
                        {
                            AddBoundBreakpoint(bp);
                        }
                    }
                }
            }
        }

        internal AD7BoundBreakpoint AddBoundBreakpoint(BoundBreakpoint bp)
        {
            lock (_boundBreakpoints)
            {
                if (_boundBreakpoints.Find((b) => b.Addr == bp.Addr) != null)
                {
                    return null;   // already bound to this breakpoint
                }
                AD7BreakpointResolution breakpointResolution = new AD7BreakpointResolution(_engine, bp.Addr, bp.FunctionName, bp.DocumentContext(_engine));
                AD7BoundBreakpoint boundBreakpoint = new AD7BoundBreakpoint(_engine, bp.Addr, this, breakpointResolution, bp);

                //check can bind one last time. If the pending breakpoint was deleted before now, we need to clean up gdb side
                if (CanBind())
                {
                    _boundBreakpoints.Add(boundBreakpoint);
                    PendingBreakpoint.AddedBoundBreakpoint();
                    _engine.Callback.OnBreakpointBound(boundBreakpoint);
                }
                else
                {
                    boundBreakpoint.Delete();
                }
                return boundBreakpoint;
            }
        }

        // Determines whether this pending breakpoint can bind to a code location.
        int IDebugPendingBreakpoint2.CanBind(out IEnumDebugErrorBreakpoints2 ppErrorEnum)
        {
            ppErrorEnum = null;

            if (!CanBind())
            {
                // Called to determine if a pending breakpoint can be bound. 
                // The breakpoint may not be bound for many reasons such as an invalid location, an invalid expression, etc...
                // The sample engine does not support this, but a real world engine will want to return a valid enumeration of IDebugErrorBreakpoint2.
                // The debugger will then display information about why the breakpoint did not bind to the user.
                ppErrorEnum = null;
                return Constants.S_FALSE;
            }

            return Constants.S_OK;
        }

        // Deletes this pending breakpoint and all breakpoints bound from it.
        int IDebugPendingBreakpoint2.Delete()
        {
            lock (_boundBreakpoints)
            {
                for (int i = _boundBreakpoints.Count - 1; i >= 0; i--)
                {
                    _boundBreakpoints[i].Delete();
                }
                _deleted = true;
                if (_engine.DebuggedProcess.ProcessState != ProcessState.Stopped)
                {
                    _pendingDelete = true;
                }
                else if (_bp != null)
                {
                    _bp.Delete(_engine.DebuggedProcess);
                    _bp = null;
                }
            }

            return Constants.S_OK;
        }

        internal async Task DeletePendingDelete()
        {
            Debug.Assert(PendingDelete, "Breakpoint is not marked for deletion");
            PendingBreakpoint bp = null;
            lock (_boundBreakpoints)
            {
                bp = _bp;
                _bp = null;
            }
            if (bp != null)
            {
                await bp.DeleteAsync(_engine.DebuggedProcess);
            }
        }

        // Toggles the enabled state of this pending breakpoint.
        int IDebugPendingBreakpoint2.Enable(int fEnable)
        {
            lock (_boundBreakpoints)
            {
                _enabled = fEnable == 0 ? false : true;
                if (_bp != null)
                {
                    _bp.Enable(_enabled, _engine.DebuggedProcess);
                }
            }

            return Constants.S_OK;
        }

        // Enumerates all breakpoints bound from this pending breakpoint
        int IDebugPendingBreakpoint2.EnumBoundBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            lock (_boundBreakpoints)
            {
                IDebugBoundBreakpoint2[] boundBreakpoints = _boundBreakpoints.ToArray();
                ppEnum = new AD7BoundBreakpointsEnum(boundBreakpoints);
            }
            return Constants.S_OK;
        }

        internal AD7BoundBreakpoint[] EnumBoundBreakpoints()
        {
            AD7BoundBreakpoint[] bplist = null;
            lock (_boundBreakpoints)
            {
                bplist = _boundBreakpoints.ToArray();
            }
            return bplist;
        }

        // Enumerates all error breakpoints that resulted from this pending breakpoint.
        int IDebugPendingBreakpoint2.EnumErrorBreakpoints(enum_BP_ERROR_TYPE bpErrorType, out IEnumDebugErrorBreakpoints2 ppEnum)
        {
            // Called when a pending breakpoint could not be bound. This may occur for many reasons such as an invalid location, an invalid expression, etc...
            // The sample engine does not support this, but a real world engine will want to send an instance of IDebugBreakpointErrorEvent2 to the
            // UI and return a valid enumeration of IDebugErrorBreakpoint2 from IDebugPendingBreakpoint2::EnumErrorBreakpoints. The debugger will then
            // display information about why the breakpoint did not bind to the user.
            if ((_BPError != null) && ((bpErrorType & enum_BP_ERROR_TYPE.BPET_TYPE_ERROR) != 0))
            {
                IDebugErrorBreakpoint2[] errlist = new IDebugErrorBreakpoint2[1];
                errlist[0] = _BPError;
                ppEnum = new AD7ErrorBreakpointsEnum(errlist);
                return Constants.S_OK;
            }
            else
            {
                ppEnum = null;
                return Constants.S_FALSE;
            }
        }

        // Gets the breakpoint request that was used to create this pending breakpoint
        int IDebugPendingBreakpoint2.GetBreakpointRequest(out IDebugBreakpointRequest2 ppBPRequest)
        {
            ppBPRequest = _pBPRequest;
            return Constants.S_OK;
        }

        // Gets the state of this pending breakpoint.
        int IDebugPendingBreakpoint2.GetState(PENDING_BP_STATE_INFO[] pState)
        {
            if (_deleted)
            {
                pState[0].state = enum_PENDING_BP_STATE.PBPS_DELETED;
            }
            else if (_enabled)
            {
                pState[0].state = enum_PENDING_BP_STATE.PBPS_ENABLED;
            }
            else if (!_enabled)
            {
                pState[0].state = enum_PENDING_BP_STATE.PBPS_DISABLED;
            }

            return Constants.S_OK;
        }

        int IDebugPendingBreakpoint2.SetCondition(BP_CONDITION bpCondition)
        {
            PendingBreakpoint bp = null;
            lock (_boundBreakpoints)
            {
                if (!VerifyCondition(bpCondition))
                {
                    _BPError = new AD7ErrorBreakpoint(this, ResourceStrings.UnsupportedConditionalBreakpoint, enum_BP_ERROR_TYPE.BPET_GENERAL_ERROR);
                    _engine.Callback.OnBreakpointError(_BPError);
                    return Constants.E_FAIL;
                }
                if ((_bpRequestInfo.dwFields & enum_BPREQI_FIELDS.BPREQI_CONDITION) != 0
                    && _bpRequestInfo.bpCondition.styleCondition == bpCondition.styleCondition
                    && _bpRequestInfo.bpCondition.bstrCondition == bpCondition.bstrCondition)
                {
                    return Constants.S_OK;  // this condition was already set
                }
                _bpRequestInfo.bpCondition = bpCondition;
                _bpRequestInfo.dwFields |= enum_BPREQI_FIELDS.BPREQI_CONDITION;
                if (_bp != null)
                {
                    bp = _bp;
                }
            }
            if (bp != null)
            {
                _engine.DebuggedProcess.WorkerThread.RunOperation(() =>
                {
                    _engine.DebuggedProcess.AddInternalBreakAction(
                        () => bp.SetConditionAsync(bpCondition.bstrCondition, _engine.DebuggedProcess)
                            );
                });
            }
            return Constants.S_OK;
        }

        // The sample engine does not support pass counts on breakpoints.
        int IDebugPendingBreakpoint2.SetPassCount(BP_PASSCOUNT bpPassCount)
        {
            if (bpPassCount.stylePassCount != enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_NONE)
            {
                _BPError = new AD7ErrorBreakpoint(this, ResourceStrings.UnsupportedPassCountBreakpoint, enum_BP_ERROR_TYPE.BPET_GENERAL_ERROR);
                _engine.Callback.OnBreakpointError(_BPError);
                return Constants.E_FAIL;
            }
            return Constants.S_OK;
        }

        // Toggles the virtualized state of this pending breakpoint. When a pending breakpoint is virtualized, 
        // the debug engine will attempt to bind it every time new code loads into the program.
        // The sample engine will does not support this.
        int IDebugPendingBreakpoint2.Virtualize(int fVirtualize)
        {
            return Constants.S_OK;
        }

        #endregion

        internal async Task DisableForFuncEvalAsync()
        {
            if (_enabled && _bp != null)
            {
                await _bp.EnableAsync(false, _engine.DebuggedProcess);
            }
        }

        internal async Task EnableAfterFuncEvalAsync()
        {
            if (_enabled && _bp != null)
            {
                await _bp.EnableAsync(true, _engine.DebuggedProcess);
            }
        }
    }

    internal class AD7ErrorBreakpoint : IDebugErrorBreakpoint2
    {
        private AD7PendingBreakpoint _pending;
        private string _error;
        private enum_BP_ERROR_TYPE _errorType;

        public AD7ErrorBreakpoint(AD7PendingBreakpoint pending, string errormsg, enum_BP_ERROR_TYPE errorType = enum_BP_ERROR_TYPE.BPET_GENERAL_WARNING)
        {
            _pending = pending;
            _error = errormsg;
            _errorType = errorType;
        }

        #region IDebugErrorBreakpoint2 Members

        public int GetBreakpointResolution(out IDebugErrorBreakpointResolution2 ppErrorResolution)
        {
            ppErrorResolution = new AD7ErrorBreakpointResolution(_error, _errorType);
            return Constants.S_OK;
        }

        public int GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBreakpoint)
        {
            ppPendingBreakpoint = _pending;
            return Constants.S_OK;
        }

        #endregion
    }
}
