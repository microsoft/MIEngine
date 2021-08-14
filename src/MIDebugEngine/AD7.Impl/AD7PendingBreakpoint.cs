﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using Microsoft.DebugEngineHost;
using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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

        private string _documentName = null;
        private string _functionName = null;
        private TEXT_POSITION[] _startPosition = new TEXT_POSITION[1];
        private TEXT_POSITION[] _endPosition = new TEXT_POSITION[1];
        private string _condition = null;
        private string _address = null;
        private ulong _codeAddress = 0;
        private uint _size = 0;
        private IEnumerable<Checksum> _checksums = null;

        public DebuggedProcess DebuggedProcess { get { return _engine.DebuggedProcess; } }

        internal string BreakpointId
        {
            get { return _bp == null ? string.Empty : _bp.Number; }
        }
        internal string AddressId { get; private set; }

        internal bool Enabled { get { return _enabled; } }
        internal bool Deleted { get { return _deleted; } }

        /// <summary>
        /// Returns true if either this breakpoint is deleted, or if this is a hardware breakpoint that has been disabled.
        /// </summary>
        internal bool PendingDelete { get { return _pendingDelete; } }

        internal bool IsDataBreakpoint { get { return _bpRequestInfo.bpLocation.bpLocationType == (uint)enum_BP_LOCATION_TYPE.BPLT_DATA_STRING; } }

        internal bool IsHardwareBreakpoint { get { return _engine.DebuggedProcess.LaunchOptions.RequireHardwareBreakpoints; } }

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
            // The sample engine only supports breakpoints on a file and line number or function name. No other types of breakpoints are supported.
            if (_deleted ||
                (_bpRequestInfo.bpLocation.bpLocationType != (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE
                && _bpRequestInfo.bpLocation.bpLocationType != (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FUNC_OFFSET
                && _bpRequestInfo.bpLocation.bpLocationType != (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_CONTEXT
                && (_bpRequestInfo.bpLocation.bpLocationType != (uint)enum_BP_LOCATION_TYPE.BPLT_DATA_STRING || !_engine.DebuggedProcess.MICommandFactory.SupportsDataBreakpoints)))
            {
                SetError(new AD7ErrorBreakpoint(this, ResourceStrings.UnsupportedBreakpoint, enum_BP_ERROR_TYPE.BPET_GENERAL_ERROR));
                return false;
            }
            if ((_bpRequestInfo.dwFields & enum_BPREQI_FIELDS.BPREQI_CONDITION) != 0)
            {
                if (!VerifyCondition(_bpRequestInfo.bpCondition))
                {
                    SetError(new AD7ErrorBreakpoint(this, ResourceStrings.UnsupportedConditionalBreakpoint, enum_BP_ERROR_TYPE.BPET_GENERAL_ERROR));
                    return false;
                }
            }
            if ((_bpRequestInfo.dwFields & enum_BPREQI_FIELDS.BPREQI_PASSCOUNT) != 0)
            {
                this.SetError(new AD7ErrorBreakpoint(this, ResourceStrings.UnsupportedPassCountBreakpoint, enum_BP_ERROR_TYPE.BPET_GENERAL_ERROR));
                return false;
            }

            return true;
        }

        // Get the document context for this pending breakpoint. A document context is a abstract representation of a source file 
        // location.
        public AD7DocumentContext GetDocumentContext(ulong address, string functionName)
        {
            if ((enum_BP_LOCATION_TYPE)_bpRequestInfo.bpLocation.bpLocationType == enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE)
            {
                AD7MemoryAddress codeContext = new AD7MemoryAddress(_engine, address, functionName);

                return new AD7DocumentContext(new MITextPosition(_documentName, _startPosition[0], _startPosition[0]), codeContext);
            }
            else
            {
                return null;
            }
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

        public void SetError(AD7ErrorBreakpoint bpError, bool sendEvent = false)
        {
            _BPError = bpError;

            if (sendEvent)
            {
                _engine.Callback.OnBreakpointError(bpError);
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
                    // Make sure that HostMarshal calls happen on main thread instead of poll thread.
                    lock (_boundBreakpoints)
                    {
                        if (_bp != null)   // already bound
                        {
                            Debug.Fail("Breakpoint already bound");
                            return Constants.S_FALSE;
                        }
                        if ((_bpRequestInfo.dwFields & enum_BPREQI_FIELDS.BPREQI_CONDITION) != 0
                            && _bpRequestInfo.bpCondition.styleCondition == enum_BP_COND_STYLE.BP_COND_WHEN_TRUE)
                        {
                            _condition = _bpRequestInfo.bpCondition.bstrCondition;
                        }
                        if ((_bpRequestInfo.dwFields & enum_BPREQI_FIELDS.BPREQI_BPLOCATION) != 0)
                        {
                            switch ((enum_BP_LOCATION_TYPE)_bpRequestInfo.bpLocation.bpLocationType)
                            {
                                case enum_BP_LOCATION_TYPE.BPLT_CODE_FUNC_OFFSET:
                                    try
                                    {
                                        IDebugFunctionPosition2 functionPosition = HostMarshal.GetDebugFunctionPositionForIntPtr(_bpRequestInfo.bpLocation.unionmember2);
                                        EngineUtils.CheckOk(functionPosition.GetFunctionName(out _functionName));
                                    }
                                    finally
                                    {
                                        HostMarshal.Release(_bpRequestInfo.bpLocation.unionmember2);
                                    }
                                    break;
                                case enum_BP_LOCATION_TYPE.BPLT_CODE_CONTEXT:
                                    try
                                    {
                                        IDebugCodeContext2 codePosition = HostMarshal.GetDebugCodeContextForIntPtr(_bpRequestInfo.bpLocation.unionmember1);
                                        if (!(codePosition is AD7MemoryAddress))
                                        {
                                            goto default;   // context is not from this engine
                                        }
                                        _codeAddress = ((AD7MemoryAddress)codePosition).Address;
                                    }
                                    finally
                                    {
                                        HostMarshal.Release(_bpRequestInfo.bpLocation.unionmember1);
                                    }
                                    break;
                                case enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE:
                                    try
                                    {
                                        IDebugDocumentPosition2 docPosition = HostMarshal.GetDocumentPositionForIntPtr(_bpRequestInfo.bpLocation.unionmember2);
                                        // Get the name of the document that the breakpoint was put in
                                        EngineUtils.CheckOk(docPosition.GetFileName(out _documentName));

                                        // Get the location in the document that the breakpoint is in.
                                        EngineUtils.CheckOk(docPosition.GetRange(_startPosition, _endPosition));
                                    }
                                    finally
                                    {
                                        HostMarshal.Release(_bpRequestInfo.bpLocation.unionmember2);
                                    }

                                    // Get the document checksum
                                    if (_engine.DebuggedProcess.MICommandFactory.SupportsBreakpointChecksums())
                                    {
                                        try
                                        {
                                            _checksums = GetSHA1Checksums();
                                        }
                                        catch (Exception)
                                        {
                                            // If we fail to get a checksum there's nothing else we can do
                                        }
                                    }

                                    break;
                                case enum_BP_LOCATION_TYPE.BPLT_DATA_STRING:
                                    string address = HostMarshal.GetDataBreakpointStringForIntPtr(_bpRequestInfo.bpLocation.unionmember3);
                                    if (address.Contains(","))
                                    {
                                        this.AddressId = address;
                                        _address = address.Split(',')[0];
                                    }
                                    else
                                    {
                                        this.AddressId = null;
                                        _address = address;
                                    }
                                    _size = (uint)_bpRequestInfo.bpLocation.unionmember4;
                                    if (_condition != null)
                                    {
                                        goto default;   // mi has no conditions on watchpoints
                                    }
                                    break;

                                default:
                                    this.SetError(new AD7ErrorBreakpoint(this, ResourceStrings.UnsupportedBreakpoint), true);
                                    return Constants.S_FALSE;

                            }
                        }
                    }

                    if (!_enabled && IsHardwareBreakpoint) {
                        return Constants.S_OK;
                    }

                    return BindWithTimeout();
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
            catch (AggregateException e)
            {
                if (e.GetBaseException() is InvalidCoreDumpOperationException)
                    return AD7_HRESULT.E_CRASHDUMP_UNSUPPORTED;
                else
                    return EngineUtils.UnexpectedException(e);
            }
            catch (InvalidCoreDumpOperationException)
            {
                return AD7_HRESULT.E_CRASHDUMP_UNSUPPORTED;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        private int BindWithTimeout()
        {
            Task bindTask = null;
            _engine.DebuggedProcess.WorkerThread.RunOperation(() =>
            {
                bindTask = _engine.DebuggedProcess.AddInternalBreakAction(this.BindAsync);
            });

            bindTask.Wait(_engine.GetBPLongBindTimeout());
            if (!bindTask.IsCompleted)
            {
                //send a low severity warning bp. This will allow the UI to respond quickly, and if the mi debugger doesn't end up binding, this warning will get 
                //replaced by the real mi debugger error text
                this.SetError(new AD7ErrorBreakpoint(this, ResourceStrings.LongBind, enum_BP_ERROR_TYPE.BPET_SEV_LOW | enum_BP_ERROR_TYPE.BPET_TYPE_WARNING), true);
                return Constants.S_FALSE;
            }
            else
            {
                if ((enum_BP_LOCATION_TYPE)_bpRequestInfo.bpLocation.bpLocationType == enum_BP_LOCATION_TYPE.BPLT_DATA_STRING)
                {
                    lock (_engine.DebuggedProcess.DataBreakpointVariables)
                    {
                        string addressName = HostMarshal.GetDataBreakpointStringForIntPtr(_bpRequestInfo.bpLocation.unionmember3);
                        if (!_engine.DebuggedProcess.DataBreakpointVariables.Contains(addressName)) // might need to expand condition
                        {
                            _engine.DebuggedProcess.DataBreakpointVariables.Add(addressName);
                        }
                    }
                }
                return Constants.S_OK;
            }
        }

        internal async Task BindAsync()
        {
            if (IsHardwareBreakpoint)
            {
                // Flush pending deletes so the debugger knows how many hardware breakpoint registers are still occupied
                await _bpManager.DeleteBreakpointsPendingDeletion();
            }

            if (CanBind())
            {
                PendingBreakpoint.BindResult bindResult;
                // Bind all breakpoints that match this source and line number.
                if (_documentName != null)
                {
                    bindResult = await PendingBreakpoint.Bind(_documentName, _startPosition[0].dwLine + 1, _startPosition[0].dwColumn, _engine.DebuggedProcess, _condition, _enabled, _checksums, this);
                }
                else if (_functionName != null)
                {
                    bindResult = await PendingBreakpoint.Bind(_functionName, _engine.DebuggedProcess, _condition, _enabled, this);
                }
                else if (_codeAddress != 0)
                {
                    bindResult = await PendingBreakpoint.Bind(_codeAddress, _engine.DebuggedProcess, _condition, _enabled, this);
                }
                else
                {
                    bindResult = await PendingBreakpoint.Bind(_address, _size, _engine.DebuggedProcess, _condition, this);
                }

                lock (_boundBreakpoints)
                {
                    if (bindResult.PendingBreakpoint != null)
                    {
                        _bp = bindResult.PendingBreakpoint;    // an MI breakpoint object exists: TODO: lock?
                    }
                    if (bindResult.BoundBreakpoints == null || bindResult.BoundBreakpoints.Count == 0)
                    {
                        this.SetError(new AD7ErrorBreakpoint(this, bindResult.ErrorMessage), true);
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
                if (!IsDataBreakpoint && _boundBreakpoints.Find((b) => b.Addr == bp.Addr) != null)
                {
                    return null;   // already bound to this breakpoint
                }
                AD7BreakpointResolution breakpointResolution = new AD7BreakpointResolution(_engine, IsDataBreakpoint, bp.Addr, bp.FunctionName, bp.DocumentContext(_engine));
                AD7BoundBreakpoint boundBreakpoint = new AD7BoundBreakpoint(_engine, this, breakpointResolution, bp);
                //check can bind one last time. If the pending breakpoint was deleted before now, we need to clean up gdb side
                if (CanBind())
                {
                    foreach (var boundBp in _boundBreakpoints.Where((b) => b.Number.Equals(boundBreakpoint.Number, StringComparison.Ordinal)).ToList())
                    {
                        _engine.Callback.OnBreakpointUnbound(boundBp, enum_BP_UNBOUND_REASON.BPUR_BREAKPOINT_REBIND);
                        _boundBreakpoints.Remove(boundBp);
                    }

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
                // The debugger will display information about why the breakpoint did not bind to the user.
                ppErrorEnum = new AD7ErrorBreakpointsEnum(new[] { this._BPError });
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
                if (_engine.DebuggedProcess.ProcessState != ProcessState.Stopped && !_engine.DebuggedProcess.MICommandFactory.AllowCommandsWhileRunning())
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
                _pendingDelete = false;
            }
            if (bp != null)
            {
                await bp.DeleteAsync(_engine.DebuggedProcess);
            }
        }

        private int EnableUsingBindAndDelete(int fEnable)
        {
            bool newValue = fEnable == 0 ? false : true;

            if (_enabled == newValue)
            {
                return Constants.S_OK;
            }

            _enabled = newValue;

            if (_enabled)
            {
                return BindWithTimeout();
            }
            else
            {
                lock (_boundBreakpoints)
                {
                    foreach (var boundBp in _boundBreakpoints)
                    {
                        _engine.Callback.OnBreakpointUnbound(boundBp, enum_BP_UNBOUND_REASON.BPUR_UNKNOWN);
                    }
                    (this as IDebugPendingBreakpoint2).Delete();
                    _boundBreakpoints.Clear();
                    _BPError = null;
                     // this pending breakpoint is not actually deleted, just disabled, so override this flag
                    _deleted = false;
                }
            }

            return Constants.S_OK;
        }

        private int EnableUsingEnableAndDisable(int fEnable)
        {
            bool newValue = fEnable == 0 ? false : true;
            if (_enabled != newValue)
            {
                _enabled = newValue;
                PendingBreakpoint bp = _bp;
                if (bp != null)
                {
                    _engine.DebuggedProcess.WorkerThread.RunOperation(() =>
                    {
                        _engine.DebuggedProcess.AddInternalBreakAction(
                            () => bp.EnableAsync(_enabled, _engine.DebuggedProcess)
                        );
                    });
                }
            }

            return Constants.S_OK;
        }

        // Toggles the enabled state of this pending breakpoint.
        int IDebugPendingBreakpoint2.Enable(int fEnable) => IsHardwareBreakpoint ? EnableUsingBindAndDelete(fEnable) : EnableUsingEnableAndDisable(fEnable);

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
                if (!VerifyCondition(bpCondition) || IsDataBreakpoint)
                {
                    this.SetError(new AD7ErrorBreakpoint(this, ResourceStrings.UnsupportedConditionalBreakpoint, enum_BP_ERROR_TYPE.BPET_GENERAL_ERROR), true);
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
                this.SetError(new AD7ErrorBreakpoint(this, ResourceStrings.UnsupportedPassCountBreakpoint, enum_BP_ERROR_TYPE.BPET_GENERAL_ERROR), true);
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
            if (!_enabled && _bp != null)
            {
                await _bp.EnableAsync(true, _engine.DebuggedProcess);
            }
        }

        /// <summary>
        /// Get the checksums associated with this pending breakpoint in order to verify the source file
        /// </summary>
        /// <param name="hashAlgorithmId">The HashAlgorithmId to use to calculate checksums</param>
        /// <param name="checksums">Enumerable of the checksums obtained from the UI</param>
        /// <returns>
        /// S_OK on Success, Error Codes on failure
        /// </returns>
        private int GetChecksum(HashAlgorithmId hashAlgorithmId, out IEnumerable<Checksum> checksums)
        {
            checksums = Enumerable.Empty<Checksum>();

            IDebugBreakpointChecksumRequest2 checksumRequest = _pBPRequest as IDebugBreakpointChecksumRequest2;
            if (checksumRequest == null)
            {
                return Constants.E_NOTIMPL;
            }

            int hr = Constants.S_OK;

            int checksumEnabled;
            hr = checksumRequest.IsChecksumEnabled(out checksumEnabled);
            if (hr != Constants.S_OK || checksumEnabled == 0)
            {
                return Constants.E_NOTIMPL;
            }

            Guid guidAlgorithm = hashAlgorithmId.AD7GuidHashAlgorithm;
            uint checksumSize = hashAlgorithmId.HashSize;

            CHECKSUM_DATA[] checksumDataArr = new CHECKSUM_DATA[1];
            hr = checksumRequest.GetChecksum(ref guidAlgorithm, checksumDataArr);
            if (hr != Constants.S_OK)
            {
                return hr;
            }
            CHECKSUM_DATA checksumData = checksumDataArr[0];

            Debug.Assert(checksumData.ByteCount % checksumSize == 0);
            uint countChecksums = checksumData.ByteCount / checksumSize;
            if (countChecksums == 0)
            {
                return Constants.S_OK;
            }

            byte[] allChecksumBytes = new byte[checksumData.ByteCount];
            System.Runtime.InteropServices.Marshal.Copy(checksumData.pBytes, allChecksumBytes, 0, allChecksumBytes.Length);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(checksumData.pBytes);

            Checksum[] checksumArray = new Checksum[countChecksums];
            for (uint i = 0; i < countChecksums; i++)
            {
                checksumArray[i] = Checksum.FromBytes(hashAlgorithmId.MIHashAlgorithmName, allChecksumBytes.Skip((int)(i * checksumSize)).Take((int)checksumSize).ToArray());
            }

            checksums = checksumArray;

            return Constants.S_OK;
        }

        /// <summary>
        /// Get the SHA1Nomralized Checksums for the document associated with the breakpoint request.
        /// If the normalized guid is not recognized by the UI, it will attempt to obtain just the SHA1 checksum
        /// </summary>
        /// <returns>One or more checksums on succes, empty enumerable on any failures</returns>
        private IEnumerable<Checksum> GetSHA1Checksums()
        {
            IEnumerable<Checksum> checksums = Enumerable.Empty<Checksum>();
            int hr = GetChecksum(HashAlgorithmId.SHA1Normalized, out checksums);

            // VS IDE does not understand the normalized guids and will return E_FAIL for normalized
            // Try to get a checksum for SHA1
            if (hr == Constants.E_FAIL)
            {
                hr = GetChecksum(HashAlgorithmId.SHA1, out checksums);
            }

            if (hr != Constants.S_OK)
            {
                return Enumerable.Empty<Checksum>();
            }

            return checksums;
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
