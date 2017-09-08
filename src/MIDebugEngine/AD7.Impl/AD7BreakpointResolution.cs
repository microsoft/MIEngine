// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.DebugEngineHost;

namespace Microsoft.MIDebugEngine
{
    // This class represents the information that describes a bound breakpoint.
    internal class AD7BreakpointResolution : IDebugBreakpointResolution2
    {
        private AD7Engine _engine;
        internal ulong Addr { get; set; }
        private AD7DocumentContext _documentContext;
        private string _functionName;
        private enum_BP_TYPE _breakType;

        public AD7BreakpointResolution(AD7Engine engine, bool isDataBreakpoint, ulong address, /*optional*/ string functionName, /*optional*/ AD7DocumentContext documentContext)
        {
            _engine = engine;
            Addr = address;
            _documentContext = documentContext;
            _functionName = functionName;
            _breakType = isDataBreakpoint ? enum_BP_TYPE.BPT_DATA : enum_BP_TYPE.BPT_CODE;
        }

        #region IDebugBreakpointResolution2 Members

        // Gets the type of the breakpoint represented by this resolution. 
        int IDebugBreakpointResolution2.GetBreakpointType(enum_BP_TYPE[] pBPType)
        {
            pBPType[0] = _breakType;
            return Constants.S_OK;
        }

        // Gets the breakpoint resolution information that describes this breakpoint.
        int IDebugBreakpointResolution2.GetResolutionInfo(enum_BPRESI_FIELDS dwFields, BP_RESOLUTION_INFO[] pBPResolutionInfo)
        {
            if ((dwFields & enum_BPRESI_FIELDS.BPRESI_BPRESLOCATION) != 0)
            {
                BP_RESOLUTION_LOCATION location = new BP_RESOLUTION_LOCATION();
                location.bpType = (uint)_breakType;
                if (_breakType == enum_BP_TYPE.BPT_CODE)
                {
                    // The debugger will not QI the IDebugCodeContext2 interface returned here. We must pass the pointer
                    // to IDebugCodeContext2 and not IUnknown.
                    AD7MemoryAddress codeContext = new AD7MemoryAddress(_engine, Addr, _functionName);
                    codeContext.SetDocumentContext(_documentContext);
                    location.unionmember1 = HostMarshal.RegisterCodeContext(codeContext);
                    pBPResolutionInfo[0].bpResLocation = location;
                    pBPResolutionInfo[0].dwFields |= enum_BPRESI_FIELDS.BPRESI_BPRESLOCATION;
                }
                else if (_breakType == enum_BP_TYPE.BPT_DATA)
                {
                    location.unionmember1 = HostMarshal.GetIntPtrForDataBreakpointAddress(EngineUtils.AsAddr(Addr, _engine.DebuggedProcess.Is64BitArch));
                    pBPResolutionInfo[0].bpResLocation = location;
                    pBPResolutionInfo[0].dwFields |= enum_BPRESI_FIELDS.BPRESI_BPRESLOCATION;
                }
            }

            if ((dwFields & enum_BPRESI_FIELDS.BPRESI_PROGRAM) != 0)
            {
                pBPResolutionInfo[0].pProgram = (IDebugProgram2)_engine;
                pBPResolutionInfo[0].dwFields |= enum_BPRESI_FIELDS.BPRESI_PROGRAM;
            }

            return Constants.S_OK;
        }

        #endregion
    }

    internal class AD7ErrorBreakpointResolution : IDebugErrorBreakpointResolution2
    {
        public AD7ErrorBreakpointResolution(string msg, enum_BP_ERROR_TYPE errorType = enum_BP_ERROR_TYPE.BPET_GENERAL_WARNING)
        {
            _message = msg;
            _errorType = errorType;
        }

        #region IDebugErrorBreakpointResolution2 Members

        private string _message;
        private enum_BP_ERROR_TYPE _errorType;

        int IDebugErrorBreakpointResolution2.GetBreakpointType(enum_BP_TYPE[] pBPType)
        {
            pBPType[0] = enum_BP_TYPE.BPT_CODE;

            return Constants.S_OK;
        }

        int IDebugErrorBreakpointResolution2.GetResolutionInfo(enum_BPERESI_FIELDS dwFields, BP_ERROR_RESOLUTION_INFO[] info)
        {
            if ((dwFields & enum_BPERESI_FIELDS.BPERESI_BPRESLOCATION) != 0)
            {
            }
            if ((dwFields & enum_BPERESI_FIELDS.BPERESI_PROGRAM) != 0)
            {
            }
            if ((dwFields & enum_BPERESI_FIELDS.BPERESI_THREAD) != 0)
            {
            }
            if ((dwFields & enum_BPERESI_FIELDS.BPERESI_MESSAGE) != 0)
            {
                info[0].dwFields |= enum_BPERESI_FIELDS.BPERESI_MESSAGE;
                info[0].bstrMessage = _message;
            }
            if ((dwFields & enum_BPERESI_FIELDS.BPERESI_TYPE) != 0)
            {
                info[0].dwFields |= enum_BPERESI_FIELDS.BPERESI_TYPE;
                info[0].dwType = _errorType;
            }

            return Constants.S_OK;
        }

        #endregion
    }
}
