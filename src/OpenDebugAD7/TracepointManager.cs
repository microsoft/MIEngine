// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Debugger.Interop.DAP;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OpenDebugAD7
{
    internal class TracepointManager
    {
        private readonly object _lock = new object();

        private readonly Regex regex;
        private readonly Dictionary<uint, string> m_breakpointLogMessages;
        private readonly enum_DEBUGPROP_INFO_FLAGS propertyInfoFlags;
        private readonly enum_EVALFLAGS flags;

        internal TracepointManager()
        {
            m_breakpointLogMessages = new Dictionary<uint, string>();

            // Matches strings that are in { } 
            // or upper case strings that begin with $.
            regex = new Regex("\\$[A-Z]+|{.*?}", RegexOptions.Compiled);

            propertyInfoFlags = enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME |
                (enum_DEBUGPROP_INFO_FLAGS)enum_DEBUGPROP_INFO_FLAGS110.DEBUGPROP110_INFO_FORCE_REAL_FUNCEVAL |
                (enum_DEBUGPROP_INFO_FLAGS)enum_DEBUGPROP_INFO_FLAGS110.DEBUGPROP110_INFO_NOSIDEEFFECTS;

            flags = enum_EVALFLAGS.EVAL_RETURNVALUE |
                enum_EVALFLAGS.EVAL_NOEVENTS |
                (enum_EVALFLAGS)enum_EVALFLAGS110.EVAL110_FORCE_REAL_FUNCEVAL |
                enum_EVALFLAGS.EVAL_NOSIDEEFFECTS;

        }

        internal void Add(uint breakpointId, string logMessage)
        {
            lock (_lock)
            {
                m_breakpointLogMessages.Add(breakpointId, logMessage);
            }
        }

        internal void Remove(uint breakpointId)
        {
            lock (_lock)
            {
                m_breakpointLogMessages.Remove(breakpointId);
            }
        }

        internal void Replace(uint breakpointId, string newLogMesssage)
        {
            lock (_lock)
            {
                m_breakpointLogMessages[breakpointId] = newLogMesssage;
            }
        }

        internal bool Contains(uint breakpointId)
        {
            lock (_lock)
            {
                return m_breakpointLogMessages.ContainsKey(breakpointId);
            }
        }

        internal bool TryGetValue(uint breakpointId, out string logMessage)
        {
            lock (_lock)
            {
                return m_breakpointLogMessages.TryGetValue(breakpointId, out logMessage);
            }
        }

        internal string GetLogMessage(uint breakpointId, IDebugThread2 pThread, uint radix)
        {
            string logMessage;
            if (!this.TryGetValue(breakpointId, out logMessage))
            {
                return string.Empty;
            }

            // There is strings to interpolate in the log message.
            if (regex.IsMatch(logMessage))
            {
                logMessage = GetInterpolatedLogMessage(logMessage, pThread, radix);
            }

            return logMessage;
        }

        private string GetInterpolatedLogMessage(string logMessage, IDebugThread2 pThread, uint radix)
        {
            if (pThread == null)
            {
                return logMessage;
            }

            // Get topFrame
            IEnumDebugFrameInfo2 frameInfoEnum;
            int hr = pThread.EnumFrameInfo(enum_FRAMEINFO_FLAGS.FIF_FRAME | enum_FRAMEINFO_FLAGS.FIF_FLAGS, Constants.EvaluationRadix, out frameInfoEnum);
            if (hr < 0)
            {
                return logMessage;
            }

            FRAMEINFO[] topFrame = new FRAMEINFO[1];
            uint fetched = 0;
            hr = frameInfoEnum.Next(1, topFrame, ref fetched);
            if (hr < 0 || fetched != 1 || topFrame[0].m_pFrame == null)
            {
                return logMessage;
            }

            HashSet<string> tokensUsed = new HashSet<string>();
            string interpolatedLogMessage = regex.Replace(logMessage, (match) =>
            {
                if (match.Success && !string.IsNullOrWhiteSpace(match.Value))
                {
                    char c = match.Value[0];
                    if (c == '$')
                    {
                        string token = match.Value.Substring(1);
                        switch (token)
                        {
                            case "FILEPOS":
                            case "FUNCTION":
                            case "ADDRESS":
                            case "TID":
                            case "TNAME":
                            case "PID":
                            case "PNAME":
                            case "CALLER":
                            case "CALLSTACK":
                            case "TICK":
                                tokensUsed.Add(token);
                                return InterpolateToken(token);
                            default:
                                return match.Value;
                        }
                    }
                    else
                    {
                        string expression = match.Value.Substring(1, match.Value.Length - 2);
                        try
                        {
                            return InterpolateVariable(expression, topFrame[0].m_pFrame, radix);
                        }
                        catch (AD7Exception e)
                        {
                            return e.ToString();
                        }
                    }
                }
                else
                {
                    return string.Empty;
                }
            });

            Dictionary<string, object> eventProperties = null;
            if (tokensUsed.Count > 0)
            {
                eventProperties = new Dictionary<string, object>();
                eventProperties.Add(DebuggerTelemetry.TelemetryTracepointTokens, tokensUsed);
            }
            DebuggerTelemetry.ReportEvent(DebuggerTelemetry.TelemetryTracepointEventName, eventProperties);

            return interpolatedLogMessage;
        }

        private string InterpolateToken(string token)
        {
            return string.Format(CultureInfo.CurrentCulture, "<Not Implemented: ${0}>", token);
        }

        private string InterpolateVariable(string variable, IDebugStackFrame2 topFrame, uint radix)
        {
            int hr = HRConstants.S_OK;
            string errorMessage = string.Format(CultureInfo.CurrentCulture, "<Evaluation Error: {{{0}}}>", variable);
            ErrorBuilder eb = new ErrorBuilder(() => errorMessage);

            IDebugExpressionContext2 expressionContext;
            hr = topFrame.GetExpressionContext(out expressionContext);
            eb.CheckHR(hr);

            IDebugExpression2 expressionObject;
            hr = expressionContext.ParseText(variable, enum_PARSEFLAGS.PARSE_EXPRESSION, radix, out expressionObject, out string _, out uint _);
            eb.CheckHR(hr);
            eb.CheckOutput(expressionObject);

            IDebugProperty2 property;
            if (expressionObject is IDebugExpressionDAP expressionDapObject)
            {
                DAPEvalFlags dapEvalFlags = DAPEvalFlags.CLIPBOARD_CONTEXT; // Use full string for tracepoints.
                hr = expressionDapObject.EvaluateSync(flags, dapEvalFlags, Constants.EvaluationTimeout, null, out property);
            }
            else
            {
                hr = expressionObject.EvaluateSync(flags, Constants.EvaluationTimeout, null, out property);
            }
            eb.CheckHR(hr);
            eb.CheckOutput(property);

            DEBUG_PROPERTY_INFO[] propertyInfo = new DEBUG_PROPERTY_INFO[1];
            hr = property.GetPropertyInfo(propertyInfoFlags, Constants.EvaluationRadix, Constants.EvaluationTimeout, null, 0, propertyInfo);
            eb.CheckHR(hr);

            // If the expression evaluation produces an error result and we are trying to get the expression for data tips
            // return a failure result so that VS code won't display the error message in data tips
            if ((propertyInfo[0].dwAttrib & enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR) == enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR)
            {
                return errorMessage;
            }

            return propertyInfo[0].bstrValue;
        }
    }
}
