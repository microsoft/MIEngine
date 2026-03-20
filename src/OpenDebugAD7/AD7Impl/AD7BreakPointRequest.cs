// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DebugEngineHost;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace OpenDebugAD7.AD7Impl
{
    internal sealed class AD7BreakPointRequest : IDebugBreakpointRequest2, IDebugBreakpointChecksumRequest2
    {
        private static uint s_nextBreakpointId = 0;

        static public uint GetNextBreakpointId()
        {
            return ++s_nextBreakpointId;
        }

        public string Condition { get; private set; }

        public string HitCondition { get; private set; }

        public AD7DocumentPosition DocumentPosition { get; private set; }

        public AD7FunctionPosition FunctionPosition { get; private set; }

        public IDebugMemoryContext2 MemoryContext {  get; private set; }

        public string DataAddress { get; private set; }
        public uint DataSize { get; private set; }

        // Used for Releasing the MemoryContext.
        // Caller of AD7BreakPointRequest(MemoryContext) is required to
        // release it with HostMarshal.ReleaseCodeContextId
        public IntPtr MemoryContextIntPtr { get; private set;  }

        // Unique identifier for breakpoint when communicating with VSCode
        public uint Id { get; } = GetNextBreakpointId();

        // Bind result from IDebugBreakpointErrorEvent2 or IDebugBreakpointBoundEvent2
        public Breakpoint BindResult { get; set; }

        public AD7BreakPointRequest(SessionConfiguration config, string path, int line, string condition, string hitCondition)
        {
            DocumentPosition = new AD7DocumentPosition(config, path, line);
            Condition = condition;
            HitCondition = hitCondition;
        }

        public AD7BreakPointRequest(string functionName)
        {
            FunctionPosition = new AD7FunctionPosition(functionName);
        }

        // Data breakpoint constructor
        public AD7BreakPointRequest(string address, uint size)
        {
            DataAddress = address;
            DataSize = size;
        }

        public AD7BreakPointRequest(IDebugMemoryContext2 memoryContext)
        {
            MemoryContext = memoryContext;
        }

        public int GetLocationType(enum_BP_LOCATION_TYPE[] pBPLocationType)
        {
            if (DocumentPosition != null)
            {
                pBPLocationType[0] = enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE;
            }
            else if (FunctionPosition != null)
            {
                pBPLocationType[0] = enum_BP_LOCATION_TYPE.BPLT_CODE_FUNC_OFFSET;
            }
            else if (MemoryContext != null)
            {
                pBPLocationType[0] = enum_BP_LOCATION_TYPE.BPLT_CODE_CONTEXT;
            }
            else if (DataAddress != null)
            {
                pBPLocationType[0] = enum_BP_LOCATION_TYPE.BPLT_DATA_STRING;
            }
            return 0;
        }

        public int GetRequestInfo(enum_BPREQI_FIELDS dwFields, BP_REQUEST_INFO[] pBPRequestInfo)
        {
            pBPRequestInfo[0].dwFields = enum_BPREQI_FIELDS.BPREQI_BPLOCATION;

            if ((dwFields & enum_BPREQI_FIELDS.BPREQI_BPLOCATION) != 0)
            {
                if (DocumentPosition != null)
                {
                    pBPRequestInfo[0].bpLocation.bpLocationType = (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FILE_LINE;
                    pBPRequestInfo[0].bpLocation.unionmember2 = HostMarshal.RegisterDocumentPosition(DocumentPosition);
                }
                else if (FunctionPosition != null)
                {
                    pBPRequestInfo[0].bpLocation.bpLocationType = (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_FUNC_OFFSET;
                    pBPRequestInfo[0].bpLocation.unionmember2 = HostMarshal.RegisterFunctionPosition(FunctionPosition);
                }
                else if (MemoryContext != null)
                {
                    pBPRequestInfo[0].bpLocation.bpLocationType = (uint)enum_BP_LOCATION_TYPE.BPLT_CODE_CONTEXT;
                    MemoryContextIntPtr = HostMarshal.RegisterCodeContext(MemoryContext as IDebugCodeContext2);
                    pBPRequestInfo[0].bpLocation.unionmember1 = MemoryContextIntPtr;
                }
                else if (DataAddress != null)
                {
                    pBPRequestInfo[0].bpLocation.bpLocationType = (uint)enum_BP_LOCATION_TYPE.BPLT_DATA_STRING;
                    pBPRequestInfo[0].bpLocation.unionmember3 = HostMarshal.GetIntPtrForDataBreakpointAddress(DataAddress);
                    pBPRequestInfo[0].bpLocation.unionmember4 = (IntPtr)DataSize;
                }
            }
            if ((dwFields & enum_BPREQI_FIELDS.BPREQI_CONDITION) != 0 && !string.IsNullOrWhiteSpace(Condition))
            {
                // VSCode only support when true condition for now
                pBPRequestInfo[0].dwFields |= enum_BPREQI_FIELDS.BPREQI_CONDITION;
                pBPRequestInfo[0].bpCondition.bstrCondition = Condition;
                pBPRequestInfo[0].bpCondition.styleCondition = enum_BP_COND_STYLE.BP_COND_WHEN_TRUE;
            }
            if ((dwFields & enum_BPREQI_FIELDS.BPREQI_PASSCOUNT) != 0
                && !string.IsNullOrWhiteSpace(HitCondition)
                && TryParseHitCondition(out enum_BP_PASSCOUNT_STYLE style, out uint passCount))
            {
                pBPRequestInfo[0].dwFields |= enum_BPREQI_FIELDS.BPREQI_PASSCOUNT;
                pBPRequestInfo[0].bpPassCount.stylePassCount = style;
                pBPRequestInfo[0].bpPassCount.dwPassCount = passCount;
            }
            return 0;
        }

        public int IsChecksumEnabled(out int fChecksumEnabled)
        {
            if (DocumentPosition != null)
            {
                return DocumentPosition.IsChecksumEnabled(out fChecksumEnabled);
            }
            else
            {
                fChecksumEnabled = 0;
                return HRConstants.S_OK;
            }
        }

        public int GetChecksum(ref Guid guidAlgorithm, CHECKSUM_DATA[] checksumData)
        {
            if (DocumentPosition != null)
            {
                return DocumentPosition.GetChecksum(ref guidAlgorithm, checksumData);
            }
            else
            {
                checksumData[0].ByteCount = 0;
                checksumData[0].pBytes = IntPtr.Zero;
                return HRConstants.E_FAIL;
            }
        }

        #region Tracepoints

        private string m_logMessage;
        private Tracepoint m_Tracepoint;

        public void ClearTracepoint()
        {
            m_logMessage = null;
            m_Tracepoint = null;
        }

        public bool SetLogMessage(string logMessage)
        {
            try
            {
                m_Tracepoint = Tracepoint.CreateTracepoint(logMessage);
                DebuggerTelemetry.ReportEvent(DebuggerTelemetry.TelemetryTracepointEventName);
            }
            catch (InvalidTracepointException e)
            {
                DebuggerTelemetry.ReportError(DebuggerTelemetry.TelemetryTracepointEventName, e.Message);
                return false;
            }
            m_logMessage = logMessage;
            return true;
        }

        public string LogMessage => m_logMessage;

        public bool HasTracepoint => !string.IsNullOrEmpty(m_logMessage) && m_Tracepoint != null;

        public Tracepoint Tracepoint => m_Tracepoint;

        #endregion

        #region Hit Conditions

        /// <summary>
        /// Updates the hit condition string so that subsequent <see cref="TryParseHitCondition"/>
        /// calls return the new pass count, and future comparisons see the updated value.
        /// </summary>
        internal void UpdateHitCondition(string hitCondition)
        {
            HitCondition = hitCondition;
        }

        /// <summary>
        /// Attempts to parse the hit condition string into a pass count style and value.
        /// Returns true if HitCondition is null/empty (no condition) or if it is a valid hit condition.
        /// </summary>
        internal bool TryParseHitCondition(out enum_BP_PASSCOUNT_STYLE style, out uint passCount)
        {
            style = enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_NONE;
            passCount = 0;

            if (string.IsNullOrWhiteSpace(HitCondition))
            {
                return true;
            }

            string hc = HitCondition.Trim();
            string numberPart = hc;

            if (hc.StartsWith(">="))
            {
                style = enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_EQUAL_OR_GREATER;
                numberPart = hc.Substring(2).Trim();
            }
            else if (hc.StartsWith("%"))
            {
                style = enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_MOD;
                numberPart = hc.Substring(1).Trim();
            }
            else
            {
                style = enum_BP_PASSCOUNT_STYLE.BP_PASSCOUNT_EQUAL;
            }

            return uint.TryParse(numberPart, out passCount);
        }

        /// <summary>
        /// Validates that the hit condition string can be parsed into a pass count.
        /// Returns true if HitCondition is null/empty (no condition) or if it is a valid hit condition.
        /// </summary>
        internal bool IsHitConditionValid => TryParseHitCondition(out _, out _);

        #endregion
    }
}
