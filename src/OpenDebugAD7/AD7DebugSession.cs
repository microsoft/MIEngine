// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DebugEngineHost;
using Microsoft.DebugEngineHost.VSCode;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Debugger.Interop.DAP;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenDebug;
using OpenDebug.CustomProtocolObjects;
using OpenDebugAD7.AD7Impl;
using ProtocolMessages = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace OpenDebugAD7
{
    internal sealed class AD7DebugSession : DebugAdapterBase, IDebugPortNotify2, IDebugEventCallback2
    {
        // This is a general purpose lock. Don't hold it across long operations.
        private readonly object m_lock = new object();

        private IDebugProcess2 m_process;
        private string m_processName;
        private int m_processId = Constants.InvalidProcessId;
        private IDebugEngineLaunch2 m_engineLaunch;
        private IDebugEngine2 m_engine;
        private EngineConfiguration m_engineConfiguration;
        private AD7Port m_port;
        private ClientId m_clientId;
        private DebugSettingsCallback m_settingsCallback;

        private readonly DebugEventLogger m_logger;
        private readonly Dictionary<string, Dictionary<int, IDebugPendingBreakpoint2>> m_breakpoints;

        private readonly ConcurrentDictionary<int, IDebugCodeContext2> m_gotoCodeContexts = new ConcurrentDictionary<int, IDebugCodeContext2>();
        private int m_nextContextId = 1;

        private Dictionary<string, IDebugPendingBreakpoint2> m_functionBreakpoints;
        private Dictionary<ulong, IDebugPendingBreakpoint2> m_instructionBreakpoints;
        private Dictionary<string, IDebugPendingBreakpoint2> m_dataBreakpoints;

        private List<string> m_exceptionBreakpoints;
        private readonly HandleCollection<IDebugStackFrame2> m_frameHandles;

        private IDebugProgram2 m_program;
        private readonly Dictionary<int, IDebugThread2> m_threads = new Dictionary<int, IDebugThread2>();

        private ManualResetEvent m_disconnectedOrTerminated;
        private int m_firstStoppingEvent;
        private uint m_breakCounter = 0;
        private bool m_isAttach;
        private bool m_isStopped = false;
        private bool m_isStepping = false;

        private readonly TaskCompletionSource<object> m_configurationDoneTCS = new TaskCompletionSource<object>();

        private readonly SessionConfiguration m_sessionConfig = new SessionConfiguration();

        private PathConverter m_pathConverter = new PathConverter();

        private VariableManager m_variableManager;

        private static Guid s_guidFilterAllLocalsPlusArgs = new Guid("939729a8-4cb0-4647-9831-7ff465240d5f");
        private static Guid s_guidFilterRegisters = new Guid("223ae797-bd09-4f28-8241-2763bdc5f713");

        private int m_nextModuleHandle = 1;
        private readonly Dictionary<IDebugModule2, int> m_moduleMap = new Dictionary<IDebugModule2, int>();

        private object RegisterDebugModule(IDebugModule2 debugModule)
        {
            Debug.Assert(!m_moduleMap.ContainsKey(debugModule));
            lock (m_moduleMap)
            {
                int moduleHandle = m_nextModuleHandle;
                m_moduleMap[debugModule] = moduleHandle;
                m_nextModuleHandle++;
                return moduleHandle;
            }
        }
        private int? ReleaseDebugModule(IDebugModule2 debugModule)
        {
            lock (m_moduleMap)
            {
                if (m_moduleMap.TryGetValue(debugModule, out int moduleId))
                {
                    m_moduleMap.Remove(debugModule);
                    return moduleId;
                } else {
                    Debug.Fail("Trying to unload a module that has not been registered.");
                    return null;
                }
            }
        }

        #region Constructor

        public AD7DebugSession(Stream debugAdapterStdIn, Stream debugAdapterStdOut, List<LoggingCategory> loggingCategories)
        {
            // This initializes this.Protocol with the streams
            base.InitializeProtocolClient(debugAdapterStdIn, debugAdapterStdOut);
            Debug.Assert(Protocol != null, "InitializeProtocolClient should have initialized this.Protocol");

            RegisterAD7EventCallbacks();
            m_logger = new DebugEventLogger(Protocol.SendEvent, loggingCategories);

            // Register message logger
            Protocol.LogMessage += m_logger.TraceLogger_EventHandler;

            m_frameHandles = new HandleCollection<IDebugStackFrame2>();
            m_breakpoints = new Dictionary<string, Dictionary<int, IDebugPendingBreakpoint2>>();
            m_functionBreakpoints = new Dictionary<string, IDebugPendingBreakpoint2>();
            m_instructionBreakpoints = new Dictionary<ulong, IDebugPendingBreakpoint2>();
            m_dataBreakpoints = new Dictionary<string, IDebugPendingBreakpoint2>();
            m_exceptionBreakpoints = new List<string>();
            m_variableManager = new VariableManager();
        }

        #endregion

        #region Utility

        private void SendTelemetryEvent(string eventName, KeyValuePair<string, object>[] eventProperties)
        {
            Dictionary<string, object> propertiesDictionary = null;
            if (eventProperties != null)
            {
                propertiesDictionary = new Dictionary<string, object>();
                foreach (var pair in eventProperties)
                {
                    propertiesDictionary[pair.Key] = pair.Value;
                }
            }

            m_logger.Write(LoggingCategory.Telemetry, eventName, propertiesDictionary);
        }

        private static string GetFrameworkVersionAttributeValue()
        {
            var attribute = typeof(object).Assembly.GetCustomAttribute(typeof(System.Reflection.AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;
            if (attribute == null)
                return string.Empty;

            return attribute.Version;
        }

        private ProtocolException CreateProtocolExceptionAndLogTelemetry(string telemetryEventName, int error, string message)
        {
            DebuggerTelemetry.ReportError(telemetryEventName, error);
            return new ProtocolException(message, new Message(error, message));
        }

        private bool ValidateProgramPath(ref string program, string miMode)
        {
            // Make sure the slashes go in the correct direction
            char directorySeparatorChar = Path.DirectorySeparatorChar;
            char wrongSlashChar = directorySeparatorChar == '\\' ? '/' : '\\';
            if (program.Contains(wrongSlashChar, StringComparison.Ordinal))
            {
                program = program.Replace(wrongSlashChar, directorySeparatorChar);
            }

            program = m_pathConverter.ConvertLaunchPathForVsCode(program);
            if (!File.Exists(program))
            {
                // On macOS, check to see if we are trying to debug an app bundle (.app).
                // 'app bundles' contain various resources and executables in a folder.
                // LLDB understands how to target these bundles.
                if (Utilities.IsOSX() && program.EndsWith(".app", StringComparison.OrdinalIgnoreCase) && miMode?.Equals("lldb", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Directory.Exists(program);
                }

                // On Windows, check if we are just missing a '.exe' from the file name. This way we can use the same
                // launch.json on all platforms.
                if (Utilities.IsWindows())
                {
                    if (!program.EndsWith(".", StringComparison.OrdinalIgnoreCase) && !program.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        string programWithExe = program + ".exe";
                        if (File.Exists(programWithExe))
                        {
                            program = programWithExe;
                            return true;
                        }
                    }
                }
                return false;
            }
            return true;
        }

        private void SetCommonDebugSettings(Dictionary<string, JToken> args)
        {
            // Save the Just My Code setting. We will set it once the engine is created.
            m_sessionConfig.JustMyCode = args.GetValueAsBool("justMyCode").GetValueOrDefault(m_sessionConfig.JustMyCode);
            m_sessionConfig.RequireExactSource = args.GetValueAsBool("requireExactSource").GetValueOrDefault(m_sessionConfig.RequireExactSource);
            m_sessionConfig.EnableStepFiltering = args.GetValueAsBool("enableStepFiltering").GetValueOrDefault(m_sessionConfig.EnableStepFiltering);

            JObject logging = args.GetValueAsObject("logging");

            if (logging != null)
            {
                m_logger.SetLoggingConfiguration(LoggingCategory.Exception, logging.GetValueAsBool("exceptions").GetValueOrDefault(true));
                m_logger.SetLoggingConfiguration(LoggingCategory.Module, logging.GetValueAsBool("moduleLoad").GetValueOrDefault(true));
                m_logger.SetLoggingConfiguration(LoggingCategory.StdOut, logging.GetValueAsBool("programOutput").GetValueOrDefault(true));
                m_logger.SetLoggingConfiguration(LoggingCategory.StdErr, logging.GetValueAsBool("programOutput").GetValueOrDefault(true));

                bool? engineLogging = logging.GetValueAsBool("engineLogging");
                if (engineLogging.HasValue)
                {
                    m_logger.SetLoggingConfiguration(LoggingCategory.EngineLogging, engineLogging.Value);
                    HostLogger.EnableHostLogging();
                    HostLogger.Instance.LogCallback = s => m_logger.WriteLine(LoggingCategory.EngineLogging, s);
                }

                bool? trace = logging.GetValueAsBool("trace");
                bool? traceResponse = logging.GetValueAsBool("traceResponse");
                if (trace.HasValue || traceResponse.HasValue)
                {
                    m_logger.SetLoggingConfiguration(LoggingCategory.AdapterTrace, (trace.GetValueOrDefault(false)) || (traceResponse.GetValueOrDefault(false)));
                }

                if (traceResponse.HasValue)
                {
                    m_logger.SetLoggingConfiguration(LoggingCategory.AdapterResponse, traceResponse.Value);
                }
            }
        }

        private void SetCommonMISettings(Dictionary<string, JToken> args)
        {
            string miMode = args.GetValueAsString("MIMode");

            // If MIMode is not provided, set default to GDB. 
            if (string.IsNullOrEmpty(miMode))
            {
                args["MIMode"] = "gdb";
            }
            else
            {
                // If lldb and there is no miDebuggerPath, set it.
                bool hasMiDebuggerPath = args.ContainsKey("miDebuggerPath") && !string.IsNullOrEmpty(args["miDebuggerPath"].ToString());
                if (miMode == "lldb" && !hasMiDebuggerPath)
                {
                    args["miDebuggerPath"] = MILaunchOptions.GetLLDBMIPath();
                }
            }
        }

        private ProtocolException VerifyLocalProcessId(string processId, string telemetryEventName, out int pid)
        {
            ProtocolException protocolException = VerifyProcessId(processId, telemetryEventName, out pid);

            if (protocolException != null)
            {
                return protocolException;
            }

            try
            {
                Process.GetProcessById(pid);
            }
            catch (ArgumentException)
            {
                return CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1006, string.Format(CultureInfo.CurrentCulture, "attach: no process with the given id:{0} found", pid));
            }

            return null;
        }

        private ProtocolException VerifyProcessId(string processId, string telemetryEventName, out int pid)
        {
            if (!int.TryParse(processId, out pid))
            {
                return CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1005, "attach: unable to parse the process id");
            }

            if (pid == 0)
            {
                return CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1008, "attach: launch.json must be configured. Change 'processId' to the process you want to debug.");
            }

            return null;
        }

        private IList<Tracepoint> GetTracepoints(IDebugBreakpointEvent2 debugEvent)
        {
            IList<Tracepoint> tracepoints = new List<Tracepoint>();

            if (debugEvent != null)
            {
                debugEvent.EnumBreakpoints(out IEnumDebugBoundBreakpoints2 pBoundBreakpoints);
                IDebugBoundBreakpoint2[] boundBp = new IDebugBoundBreakpoint2[1];

                uint numReturned = 0;
                while (pBoundBreakpoints.Next(1, boundBp, ref numReturned) == HRConstants.S_OK && numReturned == 1)
                {
                    if (boundBp[0].GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBreakpoint) == HRConstants.S_OK &&
                        ppPendingBreakpoint.GetBreakpointRequest(out IDebugBreakpointRequest2 ppBPRequest) == HRConstants.S_OK &&
                        ppBPRequest is AD7BreakPointRequest ad7BreakpointRequest &&
                        ad7BreakpointRequest.HasTracepoint)
                    {
                        tracepoints.Add(ad7BreakpointRequest.Tracepoint);
                    }
                }
            }

            return tracepoints;
        }

        public StoppedEvent.ReasonValue GetStoppedEventReason(IDebugBreakpointEvent2 breakpointEvent)
        {
            StoppedEvent.ReasonValue reason = StoppedEvent.ReasonValue.Breakpoint;

            if (breakpointEvent != null)
            {
                if (breakpointEvent.EnumBreakpoints(out IEnumDebugBoundBreakpoints2 enumBreakpoints) == HRConstants.S_OK && 
                    enumBreakpoints.GetCount(out uint bpCount) == HRConstants.S_OK && 
                    bpCount > 0)
                {

                    bool allInstructionBreakpoints = true;

                    IDebugBoundBreakpoint2[] boundBp = new IDebugBoundBreakpoint2[1];
                    uint fetched = 0;
                    while (enumBreakpoints.Next(1, boundBp, ref fetched) == HRConstants.S_OK)
                    {

                        if (boundBp[0].GetPendingBreakpoint(out IDebugPendingBreakpoint2 pendingBreakpoint) == HRConstants.S_OK)
                        {
                            if (pendingBreakpoint.GetBreakpointRequest(out IDebugBreakpointRequest2 breakpointRequest) == HRConstants.S_OK)
                            {
                                AD7BreakPointRequest request = breakpointRequest as AD7BreakPointRequest;

                                if (breakpointRequest != null && request.MemoryContext == null)
                                {
                                    allInstructionBreakpoints = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (allInstructionBreakpoints)
                    {
                        reason = StoppedEvent.ReasonValue.InstructionBreakpoint;
                    }
                }
            }

            return reason;
        }

        private static long FileTimeToPosix(FILETIME ft)
        {
            long date = ((long)ft.dwHighDateTime << 32) + ft.dwLowDateTime;
            // removes the diff between 1970 and 1601
            // 100-nanoseconds = milliseconds * 10000
            date -= 11644473600000L * 10000;

            // converts back from 100-nanoseconds to seconds
            return date / 10000000;
        }

        private ulong ResolveInstructionReference(string memoryReference, int? offset)
        {
            ulong address;

            if (memoryReference.StartsWith("0x", StringComparison.Ordinal))
            {
                address = Convert.ToUInt64(memoryReference.Substring(2), 16);
            }
            else
            {
                address = Convert.ToUInt64(memoryReference, 10);
            }

            if (offset.HasValue && offset.Value != 0)
            {
                if (offset < 0)
                {
                    address += (ulong)offset.Value;
                }
                else
                {
                    address -= (ulong)-offset.Value;
                }
            }

            return address;
        }

        private int GetMemoryContext(string memoryReference, int? offset, out IDebugMemoryContext2 memoryContext, out ulong address)
        {
            memoryContext = null;

            address = ResolveInstructionReference(memoryReference, offset);

            int hr = HRConstants.E_NOTIMPL; // Engine does not support IDebugMemoryBytesDAP

            if (m_engine is IDebugMemoryBytesDAP debugMemoryBytesDAPEngine)
            {
                hr = debugMemoryBytesDAPEngine.CreateMemoryContext(address, out memoryContext);
            }

            return hr;
        }

#endregion

#region AD7EventHandlers helper methods

        public void BeforeContinue()
        {
            m_isStepping = false;
            m_isStopped = false;
            m_variableManager.Reset();
            m_frameHandles.Reset();
            m_gotoCodeContexts.Clear();
        }

        public void Stopped(IDebugThread2 thread)
        {
            Debug.Assert(m_variableManager.IsEmpty(), "Why do we have variable handles?");
            Debug.Assert(m_frameHandles.IsEmpty, "Why do we have frame handles?");
            m_isStopped = true;
        }

        internal void FireStoppedEvent(IDebugThread2 thread, StoppedEvent.ReasonValue reason, string text = null)
        {
            Stopped(thread);

            // Switch to another thread as engines may not expect to be called back on their event thread
            ThreadPool.QueueUserWorkItem((o) =>
            {
                IEnumDebugFrameInfo2 frameInfoEnum;
                thread.EnumFrameInfo(enum_FRAMEINFO_FLAGS.FIF_FRAME | enum_FRAMEINFO_FLAGS.FIF_FLAGS, Constants.EvaluationRadix, out frameInfoEnum);

                TextPositionTuple textPosition = TextPositionTuple.Nil;
                if (frameInfoEnum != null)
                {
                    while (true)
                    {
                        FRAMEINFO[] frameInfoArray = new FRAMEINFO[1];
                        uint cFetched = 0;
                        frameInfoEnum.Next(1, frameInfoArray, ref cFetched);
                        if (cFetched != 1)
                        {
                            break;
                        }

                        if (AD7Utils.IsAnnotatedFrame(ref frameInfoArray[0]))
                        {
                            continue;
                        }

                        textPosition = TextPositionTuple.GetTextPositionOfFrame(m_pathConverter, frameInfoArray[0].m_pFrame) ?? TextPositionTuple.Nil;
                        break;
                    }
                }

                lock (m_lock)
                {
                    m_breakCounter++;
                }
                Protocol.SendEvent(new OpenDebugStoppedEvent()
                {
                    Reason = reason,
                    Text = text,
                    ThreadId = thread.Id(),
                    // Additional Breakpoint Information for Testing/Logging
                    Source = textPosition.Source,
                    Line = textPosition.Line,
                    Column = textPosition.Column,
                });
            });

            if (Interlocked.Exchange(ref m_firstStoppingEvent, 1) == 0)
            {
                m_logger.WriteLine(LoggingCategory.DebuggerStatus, AD7Resources.DebugConsoleStartMessage);
            }
        }

        private void SendDebugCompletedTelemetry()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            lock (m_lock)
            {
                properties.Add(DebuggerTelemetry.TelemetryBreakCounter, m_breakCounter);
            }
            DebuggerTelemetry.ReportEvent(DebuggerTelemetry.TelemetryDebugCompletedEventName, properties);
        }

        private static IEnumerable<IDebugBoundBreakpoint2> GetBoundBreakpoints(IDebugBreakpointBoundEvent2 breakpointBoundEvent)
        {
            int hr;
            IEnumDebugBoundBreakpoints2 boundBreakpointsEnum;
            hr = breakpointBoundEvent.EnumBoundBreakpoints(out boundBreakpointsEnum);
            if (hr != HRConstants.S_OK)
            {
                return Enumerable.Empty<IDebugBoundBreakpoint2>();
            }

            uint bufferSize;
            hr = boundBreakpointsEnum.GetCount(out bufferSize);
            if (hr != HRConstants.S_OK)
            {
                return Enumerable.Empty<IDebugBoundBreakpoint2>();
            }

            IDebugBoundBreakpoint2[] boundBreakpoints = new IDebugBoundBreakpoint2[bufferSize];
            uint fetched = 0;
            hr = boundBreakpointsEnum.Next(bufferSize, boundBreakpoints, ref fetched);
            if (hr != HRConstants.S_OK || fetched != bufferSize)
            {
                return Enumerable.Empty<IDebugBoundBreakpoint2>();
            }

            return boundBreakpoints;
        }

        private int? GetBoundBreakpointLineNumber(IDebugBoundBreakpoint2 boundBreakpoint)
        {
            int hr;
            IDebugBreakpointResolution2 breakpointResolution;
            hr = boundBreakpoint.GetBreakpointResolution(out breakpointResolution);
            if (hr != HRConstants.S_OK)
            {
                return null;
            }

            BP_RESOLUTION_INFO[] resolutionInfo = new BP_RESOLUTION_INFO[1];
            hr = breakpointResolution.GetResolutionInfo(enum_BPRESI_FIELDS.BPRESI_BPRESLOCATION, resolutionInfo);
            if (hr != HRConstants.S_OK)
            {
                return null;
            }

            BP_RESOLUTION_LOCATION location = resolutionInfo[0].bpResLocation;
            enum_BP_TYPE bpType = (enum_BP_TYPE)location.bpType;
            if (bpType != enum_BP_TYPE.BPT_CODE || location.unionmember1 == IntPtr.Zero)
            {
                return null;
            }

            IDebugCodeContext2 codeContext;
            try
            {
                codeContext = HostMarshal.GetDebugCodeContextForIntPtr(location.unionmember1);
                HostMarshal.ReleaseCodeContextId(location.unionmember1);
                location.unionmember1 = IntPtr.Zero;
            }
            catch (ArgumentException)
            {
                return null;
            }
            IDebugDocumentContext2 docContext;
            hr = codeContext.GetDocumentContext(out docContext);
            if (hr != HRConstants.S_OK)
            {
                return null;
            }

            // VSTS 237376: Shared library compiled without symbols will still bind a bp, but not have a docContext
            if (null == docContext)
            {
                return null;
            }

            TEXT_POSITION[] begin = new TEXT_POSITION[1];
            TEXT_POSITION[] end = new TEXT_POSITION[1];
            hr = docContext.GetStatementRange(begin, end);
            if (hr != HRConstants.S_OK)
            {
                return null;
            }

            return m_pathConverter.ConvertDebuggerLineToClient((int)begin[0].dwLine);
        }

        private enum MessagePrefix
        {
            None,
            Warning,
            Error
        };

        private class CurrentLaunchState
        {
            public Tuple<MessagePrefix, string> CurrentError { get; set; }
        }

        private CurrentLaunchState m_currentLaunchState;

        private void SendMessageEvent(MessagePrefix prefix, string text)
        {
            string prefixString = string.Empty;
            LoggingCategory category = LoggingCategory.DebuggerStatus;
            switch (prefix)
            {
                case MessagePrefix.Warning:
                    prefixString = AD7Resources.Prefix_Warning;
                    category = LoggingCategory.DebuggerError;
                    break;
                case MessagePrefix.Error:
                    prefixString = AD7Resources.Prefix_Error;
                    category = LoggingCategory.DebuggerError;
                    break;
            }

            m_logger.WriteLine(category, prefixString + text);
        }

        private VariablesResponse VariablesFromFrame(VariableScope vref, uint radix)
        {
            var frame = vref.StackFrame;
            var category = vref.Category;

            var response = new VariablesResponse();

            Guid filter = Guid.Empty;
            switch (category)
            {
            case VariableCategory.Locals:
                filter = s_guidFilterAllLocalsPlusArgs;
                break;
            case VariableCategory.Registers:
                filter = s_guidFilterRegisters;
                break;
            }

            uint n;
            IEnumDebugPropertyInfo2 varEnum;
            if (frame.EnumProperties(GetDefaultPropertyInfoFlags(), radix, ref filter, 0, out n, out varEnum) == HRConstants.S_OK)
            {
                var props = new DEBUG_PROPERTY_INFO[1];
                uint nProps;
                while (varEnum.Next(1, props, out nProps) == HRConstants.S_OK)
                {
                    response.Variables.Add(m_variableManager.CreateVariable(props[0].pProperty, GetDefaultPropertyInfoFlags()));
                    m_variableManager.AddFrameVariable(frame, props[0]);
                }
            }

            return response;
        }

        public enum_DEBUGPROP_INFO_FLAGS GetDefaultPropertyInfoFlags()
        {
            enum_DEBUGPROP_INFO_FLAGS flags =
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_STANDARD |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME |
                (enum_DEBUGPROP_INFO_FLAGS)enum_DEBUGPROP_INFO_FLAGS110.DEBUGPROP110_INFO_FORCE_REAL_FUNCEVAL;

            if (m_sessionConfig.JustMyCode)
            {
                flags |= (enum_DEBUGPROP_INFO_FLAGS)enum_DEBUGPROP_INFO_FLAGS110.DEBUGPROP110_INFO_NO_NONPUBLIC_MEMBERS;
            }

            return flags;
        }

        private void SetExceptionCategory(ExceptionSettings.CategoryConfiguration category, enum_EXCEPTION_STATE state)
        {
            var exceptionInfo = new EXCEPTION_INFO[1];
            exceptionInfo[0].dwState = state;
            exceptionInfo[0].guidType = category.Id;
            exceptionInfo[0].bstrExceptionName = category.Name;

            m_engine.SetException(exceptionInfo);
        }

        private void SetCategoryGuidExceptions(Guid categoryId, enum_EXCEPTION_STATE state)
        {
            ExceptionSettings.CategoryConfiguration category = m_engineConfiguration.ExceptionSettings.Categories.FirstOrDefault(x => x.Id == categoryId);

            if (category != null)
            {
                var exceptionInfo = new EXCEPTION_INFO[1];
                exceptionInfo[0].dwState = state;
                exceptionInfo[0].guidType = categoryId;
                exceptionInfo[0].bstrExceptionName = category.Name;

                m_engine.SetException(exceptionInfo);
            }
            else
            {
                Debug.Fail(categoryId + " is a referencing a non-existant category. This should have been caught in ExceptionSettings.ValidateExceptionFilters.");
            }
        }

        private void StepInternal(int threadId, enum_STEPKIND stepKind, SteppingGranularity granularity, string errorMessage)
        {
            // If we are already running ignore additional step requests
            if (!m_isStopped)
                return;

            IDebugThread2 thread = null;
            lock (m_threads)
            {
                if (!m_threads.TryGetValue(threadId, out thread))
                {
                    throw new AD7Exception(errorMessage);
                }
            }

            BeforeContinue();
            ErrorBuilder builder = new ErrorBuilder(() => errorMessage);
            m_isStepping = true;

            enum_STEPUNIT stepUnit = enum_STEPUNIT.STEP_STATEMENT;
            switch (granularity)
            {
                case SteppingGranularity.Statement:
                default:
                    break;
                case SteppingGranularity.Line:
                    stepUnit = enum_STEPUNIT.STEP_LINE;
                    break;
                case SteppingGranularity.Instruction:
                    stepUnit = enum_STEPUNIT.STEP_INSTRUCTION;
                    break;
            }
            try
            {
                builder.CheckHR(m_program.Step(thread, stepKind, stepUnit));
            }
            catch (AD7Exception)
            {
                m_isStopped = true;
                throw;
            }
        }

        private enum ClientId
        {
            Unknown,
            VisualStudio,
            VsCode,
            LiveshareServerHost
        };

        private bool IsClientVS
        {
            get
            {
                return m_clientId == ClientId.VisualStudio || m_clientId == ClientId.LiveshareServerHost;
            }
        }

#endregion

#region DebugAdapterBase

        protected override void HandleInitializeRequestAsync(IRequestResponder<InitializeArguments, InitializeResponse> responder)
        {
            InitializeArguments arguments = responder.Arguments;

            m_engineConfiguration = EngineConfiguration.TryGet(arguments.AdapterID);

            m_engine = (IDebugEngine2)m_engineConfiguration.LoadEngine();

            TypeInfo engineType = m_engine.GetType().GetTypeInfo();
            HostTelemetry.InitializeTelemetry(SendTelemetryEvent, engineType, m_engineConfiguration.AdapterId);
            DebuggerTelemetry.InitializeTelemetry(Protocol.SendEvent, engineType, typeof(Host).GetTypeInfo(), m_engineConfiguration.AdapterId);

            HostOutputWindow.InitializeLaunchErrorCallback((error) => m_logger.WriteLine(LoggingCategory.DebuggerError, error));

            m_engineLaunch = (IDebugEngineLaunch2)m_engine;
            m_engine.SetRegistryRoot(m_engineConfiguration.AdapterId);
            m_port = new AD7Port(this);
            m_disconnectedOrTerminated = new ManualResetEvent(false);
            m_firstStoppingEvent = 0;

            if (m_engine is IDebugEngine110 engine110)
            {
                // MIEngine generally gets the radix from IDebugSettingsCallback110 rather than using the radix passed to individual
                //  APIs.  To support this mechanism outside of VS, provide a fake settings callback here that we can use to control
                //  the radix.
                m_settingsCallback = new DebugSettingsCallback();
                engine110.SetMainThreadSettingsCallback110(m_settingsCallback);
            }

            m_pathConverter.ClientLinesStartAt1 = arguments.LinesStartAt1.GetValueOrDefault(true);

            // Default is that they are URIs
            m_pathConverter.ClientPathsAreURI = !(arguments.PathFormat.GetValueOrDefault(InitializeArguments.PathFormatValue.Unknown) == InitializeArguments.PathFormatValue.Path);

            string clientId = responder.Arguments.ClientID;
            if (clientId == "visualstudio")
            {
                m_clientId = ClientId.VisualStudio;
            }
            else if (clientId == "vscode")
            {
                m_clientId = ClientId.VsCode;
            }
            else if (clientId == "liveshare-server-host")
            {
                m_clientId = ClientId.LiveshareServerHost;
            }
            else
            {
                m_clientId = ClientId.Unknown;
            }

            // If the UI supports RunInTerminal, then register the callback.
            // NOTE: Currently we don't support using the RunInTerminal request with VS or Windows Codespaces.
            //       This is because: (1) they don't support 'Integrated' terminal, and (2) for MIEngine, we don't ship WindowsDebugLauncher.exe.
            if (!IsClientVS && arguments.SupportsRunInTerminalRequest.GetValueOrDefault(false))
            {
                HostRunInTerminal.RegisterRunInTerminalCallback((title, cwd, useExternalConsole, commandArgs, env, success, error) =>
                {
                    RunInTerminalRequest request = new RunInTerminalRequest()
                    {
                        Arguments = commandArgs.ToList<string>(),
                        Kind = useExternalConsole ? RunInTerminalArguments.KindValue.External : RunInTerminalArguments.KindValue.Integrated,
                        Title = title,
                        Cwd = cwd,
                        Env = env
                    };

                    Protocol.SendClientRequest(
                        request,
                        (args, responseBody) =>
                        {
                            // responseBody can be null
                            success(responseBody?.ProcessId);
                        },
                        (args, exception) =>
                        {
                            new OutputEvent() { Category = OutputEvent.CategoryValue.Stderr, Output = exception.ToString() };
                            Protocol.SendEvent(new TerminatedEvent());
                            error(exception.ToString());
                        });
                });
            }

            List<ColumnDescriptor> additionalModuleColumns = null;

            if (IsClientVS)
            {
                additionalModuleColumns = new List<ColumnDescriptor>();
                additionalModuleColumns.Add(new ColumnDescriptor(){
                    AttributeName = "vsLoadAddress",
                    Label = "Load Address",
                    Format = "string",
                    Type = ColumnDescriptor.TypeValue.String
                });
                additionalModuleColumns.Add(new ColumnDescriptor(){
                    AttributeName = "vsPreferredLoadAddress",
                    Label = "Preferred Load Address",
                    Format = "string",
                    Type = ColumnDescriptor.TypeValue.String
                });
                additionalModuleColumns.Add(new ColumnDescriptor(){
                    AttributeName = "vsModuleSize",
                    Label = "Module Size",
                    Format = "string",
                    Type = ColumnDescriptor.TypeValue.Number
                });
                additionalModuleColumns.Add(new ColumnDescriptor(){
                    AttributeName = "vsLoadOrder",
                    Label = "Order",
                    Format = "string",
                    Type = ColumnDescriptor.TypeValue.Number
                });
                additionalModuleColumns.Add(new ColumnDescriptor(){
                    AttributeName = "vsTimestampUTC",
                    Label = "Timestamp",
                    Format = "string",
                    Type = ColumnDescriptor.TypeValue.UnixTimestampUTC
                });
                additionalModuleColumns.Add(new ColumnDescriptor(){
                    AttributeName = "vsIs64Bit",
                    Label = "64-bit",
                    Format = "string",
                    Type = ColumnDescriptor.TypeValue.Boolean
                });
            }

            // -catch-throw is not supported in lldb-mi
            List<ExceptionBreakpointsFilter> filters = new List<ExceptionBreakpointsFilter>();
            if (!Utilities.IsOSX())
            {
                filters = m_engineConfiguration.ExceptionSettings.ExceptionBreakpointFilters.Select(item => new ExceptionBreakpointsFilter() { Default = item.@default, Filter = item.filter, Label = item.label, SupportsCondition = item.supportsCondition, ConditionDescription = item.conditionDescription }).ToList();
            }

            InitializeResponse initializeResponse = new InitializeResponse()
            {
                SupportsConfigurationDoneRequest = true,
                SupportsCompletionsRequest = m_engine is IDebugProgramDAP,
                SupportsEvaluateForHovers = true,
                SupportsSetVariable = true,
                SupportsFunctionBreakpoints = m_engineConfiguration.FunctionBP,
                SupportsConditionalBreakpoints = m_engineConfiguration.ConditionalBP,
                SupportsDataBreakpoints = m_engineConfiguration.DataBP,
                ExceptionBreakpointFilters = filters,
                SupportsExceptionFilterOptions = filters.Any(),
                SupportsClipboardContext = m_engineConfiguration.ClipboardContext,
                SupportsLogPoints = true,
                SupportsReadMemoryRequest = m_engine is IDebugMemoryBytesDAP, // TODO: Read from configuration or query engine for capabilities.
                SupportsModulesRequest = true,
                AdditionalModuleColumns = additionalModuleColumns,
                SupportsGotoTargetsRequest = true,
                SupportsDisassembleRequest = m_engine is IDebugMemoryBytesDAP,
                SupportsValueFormattingOptions = true,
                SupportsSteppingGranularity = true,
                SupportsInstructionBreakpoints = m_engine is IDebugMemoryBytesDAP
            };

            responder.SetResponse(initializeResponse);
        }

        protected override void HandleLaunchRequestAsync(IRequestResponder<LaunchArguments> responder)
        {
            const string telemetryEventName = DebuggerTelemetry.TelemetryLaunchEventName;

            int hr;
            DateTime launchStartTime = DateTime.Now;

            string mimode = responder.Arguments.ConfigurationProperties.GetValueAsString("MIMode");
            string program = responder.Arguments.ConfigurationProperties.GetValueAsString("program")?.Trim();
            if (string.IsNullOrEmpty(program))
            {
                responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1001, "launch: property 'program' is missing or empty"));
                return;
            }

            // If program is still in the default state, raise error
            if (program.EndsWith(">", StringComparison.Ordinal) && program.Contains('<', StringComparison.Ordinal))
            {
                responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1001, "launch: launch.json must be configured. Change 'program' to the path to the executable file that you would like to debug."));
                return;
            }

            // Should not have a pid in launch
            if (responder.Arguments.ConfigurationProperties.ContainsKey("processId"))
            {
                responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1001, "launch: The parameter: 'processId' should not be specified on Launch. Please use request type: 'attach'"));
                return;
            }

            JToken pipeTransport = responder.Arguments.ConfigurationProperties.GetValueAsObject("pipeTransport");
            string miDebuggerServerAddress = responder.Arguments.ConfigurationProperties.GetValueAsString("miDebuggerServerAddress");

            // Pipe trasport can talk to remote machines so paths and files should not be checked in this case.
            bool skipFilesystemChecks = (pipeTransport != null || miDebuggerServerAddress != null);

            // For a remote scenario, we assume whatever input user has provided is correct.
            // The target remote could be any OS, so we don't try to change anything.
            if (!skipFilesystemChecks)
            {
                if (!ValidateProgramPath(ref program, mimode))
                {
                    responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1002, String.Format(CultureInfo.CurrentCulture, "launch: program '{0}' does not exist", program)));
                    return;
                }
            }

            string workingDirectory = responder.Arguments.ConfigurationProperties.GetValueAsString("cwd");
            if (string.IsNullOrEmpty(workingDirectory))
            {
                responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1003, "launch: property 'cwd' is missing or empty"));
                return;
            }

            if (!skipFilesystemChecks)
            {
                workingDirectory = m_pathConverter.ConvertLaunchPathForVsCode(workingDirectory);
                if (!Directory.Exists(workingDirectory))
                {
                    responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1004, String.Format(CultureInfo.CurrentCulture, "launch: workingDirectory '{0}' does not exist", workingDirectory)));
                    return;
                }
            }

            SetCommonDebugSettings(responder.Arguments.ConfigurationProperties);

            bool success = false;
            try
            {
                lock (m_lock)
                {
                    Debug.Assert(m_currentLaunchState == null, "Concurrent launches??");
                    m_currentLaunchState = new CurrentLaunchState();
                }
                var eb = new ErrorBuilder(() => AD7Resources.Error_Scenario_Launch);

                // Don't convert the workingDirectory string if we are a pipeTransport connection. We are assuming that the user has the correct directory separaters for their target OS
                string workingDirectoryString = pipeTransport != null ? workingDirectory : m_pathConverter.ConvertClientPathToDebugger(workingDirectory);

                m_sessionConfig.StopAtEntrypoint = responder.Arguments.ConfigurationProperties.GetValueAsBool("stopAtEntry").GetValueOrDefault(false);

                m_processId = Constants.InvalidProcessId;
                m_processName = program;

                enum_LAUNCH_FLAGS flags = enum_LAUNCH_FLAGS.LAUNCH_DEBUG;
                if (responder.Arguments.NoDebug.GetValueOrDefault(false))
                {
                    flags = enum_LAUNCH_FLAGS.LAUNCH_NODEBUG;
                }

                SetCommonMISettings(responder.Arguments.ConfigurationProperties);

                string launchJson = JsonConvert.SerializeObject(responder.Arguments.ConfigurationProperties);

                // Then attach
                hr = m_engineLaunch.LaunchSuspended(null,
                    m_port,
                    program,
                    null,
                    null,
                    null,
                    launchJson,
                    flags,
                    0,
                    0,
                    0,
                    this,
                    out m_process);
                if (hr != HRConstants.S_OK)
                {
                    // If the engine raised a message via an error event, fire that instead
                    if (hr == HRConstants.E_ABORT)
                    {
                        string message;
                        lock (m_lock)
                        {
                            message = m_currentLaunchState?.CurrentError?.Item2;
                            m_currentLaunchState = null;
                        }
                        if (message != null)
                        {
                            responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1005, message));
                            return;
                        }
                    }

                    eb.ThrowHR(hr);
                }

                hr = m_engineLaunch.ResumeProcess(m_process);
                if (hr < 0)
                {
                    // try to terminate the process if we can
                    try
                    {
                        m_engineLaunch.TerminateProcess(m_process);
                    }
                    catch
                    {
                        // Ignore failures since we are already dealing with an error
                    }

                    eb.ThrowHR(hr);
                }

                var properties = new Dictionary<string, object>(StringComparer.Ordinal);

                if (flags.HasFlag(enum_LAUNCH_FLAGS.LAUNCH_NODEBUG))
                {
                    properties.Add(DebuggerTelemetry.TelemetryIsNoDebug, true);
                }

                properties.Add(DebuggerTelemetry.TelemetryMIMode, mimode);
                properties.Add(DebuggerTelemetry.TelemetryFrameworkVersion, GetFrameworkVersionAttributeValue());

                DebuggerTelemetry.ReportTimedEvent(telemetryEventName, DateTime.Now - launchStartTime, properties);

                success = true;
            }
            catch (Exception e)
            {
                // Instead of failing to launch with the exception, try and wrap it better so that the information is useful for the user.
                responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1007, string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_ExceptionOccured, e.InnerException?.ToString() ?? e.ToString())));
                return;
            }
            finally
            {
                // Clear _currentLaunchState
                CurrentLaunchState currentLaunchState;
                lock (m_lock)
                {
                    currentLaunchState = m_currentLaunchState;
                    m_currentLaunchState = null;
                }

                if (!success)
                {
                    m_process = null;
                }

                // If we had an error event that we didn't wind up returning as an exception, raise it as an event
                Tuple<MessagePrefix, string> currentError = currentLaunchState?.CurrentError;
                if (currentError != null)
                {
                    SendMessageEvent(currentError.Item1, currentError.Item2);
                }
            }

            responder.SetResponse(new LaunchResponse());
        }

        protected override void HandleAttachRequestAsync(IRequestResponder<AttachArguments> responder)
        {
            const string telemetryEventName = DebuggerTelemetry.TelemetryAttachEventName;

            // ProcessId can be either a string or an int. We attempt to parse as int, if that does not exist we attempt to parse as a string.
            string processId = responder.Arguments.ConfigurationProperties.GetValueAsInt("processId")?.ToString(CultureInfo.InvariantCulture) ?? responder.Arguments.ConfigurationProperties.GetValueAsString("processId");
            string miDebuggerServerAddress = responder.Arguments.ConfigurationProperties.GetValueAsString("miDebuggerServerAddress");
            DateTime attachStartTime = DateTime.Now;
            JObject pipeTransport = responder.Arguments.ConfigurationProperties.GetValueAsObject("pipeTransport");
            bool isPipeTransport = (pipeTransport != null);
            bool isLocal = string.IsNullOrEmpty(miDebuggerServerAddress) && !isPipeTransport;
            string mimode = responder.Arguments.ConfigurationProperties.GetValueAsString("MIMode");

            if (isLocal)
            {
                if (string.IsNullOrEmpty(processId))
                {
                    responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1001, "attach: property 'processId' needs to be specified"));
                    return;
                }
            }
            else
            {
                string propertyCausingRemote = !string.IsNullOrEmpty(miDebuggerServerAddress) ? "miDebuggerServerAddress" : "pipeTransport";

                if (!string.IsNullOrEmpty(miDebuggerServerAddress) && !string.IsNullOrEmpty(processId))
                {
                    responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1002, "attach: 'processId' cannot be used with " + propertyCausingRemote));
                    return;
                }
                else if (isPipeTransport && (string.IsNullOrEmpty(processId) || string.IsNullOrEmpty(pipeTransport.GetValueAsString("debuggerPath"))))
                {
                    responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1001, "attach: properties 'processId' and 'debuggerPath' needs to be specified with " + propertyCausingRemote));
                    return;
                }
            }

            int pid = 0;

            ProtocolException protocolException = isLocal ? VerifyLocalProcessId(processId, telemetryEventName, out pid) : VerifyProcessId(processId, telemetryEventName, out pid);

            if (protocolException != null)
            {
                responder.SetError(protocolException);
                return;
            }

            SetCommonDebugSettings(responder.Arguments.ConfigurationProperties);

            string program = responder.Arguments.ConfigurationProperties.GetValueAsString("program");
            string executable = null;
            bool success = false;
            try
            {
                lock (m_lock)
                {
                    Debug.Assert(m_currentLaunchState == null, "Concurrent launches??");
                    m_currentLaunchState = new CurrentLaunchState();
                }
                var eb = new ErrorBuilder(() => AD7Resources.Error_Scenario_Attach);

                if (isPipeTransport)
                {
                    if (string.IsNullOrEmpty(pipeTransport.GetValueAsString("debuggerPath")))
                    {
                        responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1011, "debuggerPath is required for attachTransport."));
                        return;
                    }

                    if (string.IsNullOrEmpty(program))
                    {
                        responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1009, "attach: property 'program' is missing or empty"));
                        return;
                    }
                    else
                    {
                        executable = program;
                    }
                    m_isAttach = true;
                }
                else
                {
                    if (string.IsNullOrEmpty(program))
                    {
                        responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1009, "attach: property 'program' is missing or empty"));
                        return;
                    }

                    if (!ValidateProgramPath(ref program, mimode))
                    {
                        responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1010, String.Format(CultureInfo.CurrentCulture, "attach: program path '{0}' does not exist", program)));
                        return;
                    }

                    executable = program;
                    m_isAttach = true;
                }

                if (int.TryParse(processId, NumberStyles.None, CultureInfo.InvariantCulture, out m_processId))
                {
                    m_processId = Constants.InvalidProcessId;
                }
                m_processName = program ?? string.Empty;

                SetCommonMISettings(responder.Arguments.ConfigurationProperties);

                string launchJson = JsonConvert.SerializeObject(responder.Arguments.ConfigurationProperties);

                // attach
                int hr = m_engineLaunch.LaunchSuspended(null, m_port, executable, null, null, null, launchJson, 0, 0, 0, 0, this, out m_process);

                if (hr != HRConstants.S_OK)
                {
                    // If the engine raised a message via an error event, fire that instead
                    if (hr == HRConstants.E_ABORT)
                    {
                        string message;
                        lock (m_lock)
                        {
                            message = m_currentLaunchState?.CurrentError?.Item2;
                            m_currentLaunchState = null;
                        }
                        if (message != null)
                        {
                            responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1012, message));
                            return;
                        }
                    }

                    eb.ThrowHR(hr);
                }

                hr = m_engineLaunch.ResumeProcess(m_process);
                if (hr < 0)
                {
                    // try to terminate the process if we can
                    try
                    {
                        m_engineLaunch.TerminateProcess(m_process);
                    }
                    catch
                    {
                        // Ignore failures since we are already dealing with an error
                    }

                    eb.ThrowHR(hr);
                }

                var properties = new Dictionary<string, object>(StringComparer.Ordinal);
                properties.Add(DebuggerTelemetry.TelemetryMIMode, mimode);
                properties.Add(DebuggerTelemetry.TelemetryFrameworkVersion, GetFrameworkVersionAttributeValue());

                DebuggerTelemetry.ReportTimedEvent(telemetryEventName, DateTime.Now - attachStartTime, properties);
                success = true;

                responder.SetResponse(new AttachResponse());
            }
            catch (Exception e)
            {
                responder.SetError(CreateProtocolExceptionAndLogTelemetry(telemetryEventName, 1007, string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_ExceptionOccured, e.InnerException?.ToString() ?? e.ToString())));
            }
            finally
            {
                // Clear _currentLaunchState
                CurrentLaunchState currentLaunchState;
                lock (m_lock)
                {
                    currentLaunchState = m_currentLaunchState;
                    m_currentLaunchState = null;
                }

                // If we had an error event that we didn't wind up returning as an exception, raise it as an event
                Tuple<MessagePrefix, string> currentError = currentLaunchState?.CurrentError;
                if (currentError != null)
                {
                    SendMessageEvent(currentError.Item1, currentError.Item2);
                }

                if (!success)
                {
                    m_process = null;
                    this.Protocol.Stop();
                }
            }
        }

        protected override void HandleDisconnectRequestAsync(IRequestResponder<DisconnectArguments> responder)
        {
            int hr;

            // If we are waiting to continue program create, stop waiting
            m_configurationDoneTCS.TrySetResult(null);

            if (m_process != null)
            {
                string errorReason = null;
                try
                {
                    // Detach if it is attach or TerminateDebuggee is set to false
                    bool shouldDetach = m_isAttach || !responder.Arguments.TerminateDebuggee.GetValueOrDefault(true);
                    hr = shouldDetach ? m_program.Detach() : m_engineLaunch.TerminateProcess(m_process);

                    if (hr < 0)
                    {
                        errorReason = ErrorBuilder.GetErrorDescription(hr);
                    }
                    else
                    {
                        // wait for termination event
                        if (!m_disconnectedOrTerminated.WaitOne(Constants.DisconnectTimeout))
                        {
                            errorReason = AD7Resources.MissingDebuggerTerminationEvent;
                        }
                    }
                }
                catch (Exception e)
                {
                    errorReason = Utilities.GetExceptionDescription(e);
                }

                // VS Code ignores the result of Disconnect. So send an output event instead.
                if (errorReason != null)
                {
                    string message = string.Format(CultureInfo.CurrentCulture, AD7Resources.Warning_Scenario_TerminateProcess, m_processName, errorReason);
                    m_logger.WriteLine(LoggingCategory.DebuggerError, message);
                }
            }

            responder.SetResponse(new DisconnectResponse());

            // Disconnect should terminate protocol
            this.Protocol.Stop();
        }

        protected override void HandleConfigurationDoneRequestAsync(IRequestResponder<ConfigurationDoneArguments> responder)
        {
            // If we are waiting to continue program create, mark that we have now finished initializing settings
            m_configurationDoneTCS.TrySetResult(null);

            responder.SetResponse(new ConfigurationDoneResponse());
        }

        protected override void HandleContinueRequestAsync(IRequestResponder<ContinueArguments, ContinueResponse> responder)
        {
            int threadId = responder.Arguments.ThreadId;

            // Sometimes we can get a threadId of 0. Make sure we don't look it up in this case, otherwise we will crash.
            IDebugThread2 thread = null;
            lock (m_threads)
            {
                if (threadId != 0 && !m_threads.TryGetValue(threadId, out thread))
                {
                    // We do not accept nonzero unknown threadIds.
                    Debug.Fail("Unknown threadId passed to Continue!");
                    return;
                }
            }

            BeforeContinue();
            ErrorBuilder builder = new ErrorBuilder(() => AD7Resources.Error_Scenario_Continue);

            bool succeeded = false;
            try
            {
                builder.CheckHR(m_program.Continue(thread));
                succeeded = true;
                responder.SetResponse(new ContinueResponse());
            }
            catch (AD7Exception e)
            {
                responder.SetError(new ProtocolException(e.Message));
            }
            finally
            {
                if (!succeeded)
                {
                    m_isStopped = true;
                }
            }
        }

        protected override void HandleStepInRequestAsync(IRequestResponder<StepInArguments> responder)
        {
            try
            {
                var granularity = responder.Arguments.Granularity.GetValueOrDefault();
                StepInternal(responder.Arguments.ThreadId, enum_STEPKIND.STEP_INTO, granularity, AD7Resources.Error_Scenario_Step_In);
                responder.SetResponse(new StepInResponse());
            }
            catch (AD7Exception e)
            {
                responder.SetError(new ProtocolException(e.Message));
            }
        }

        protected override void HandleNextRequestAsync(IRequestResponder<NextArguments> responder)
        {
            try
            {
                var granularity = responder.Arguments.Granularity.GetValueOrDefault();
                StepInternal(responder.Arguments.ThreadId, enum_STEPKIND.STEP_OVER, granularity, AD7Resources.Error_Scenario_Step_Next);
                responder.SetResponse(new NextResponse());
            }
            catch (AD7Exception e)
            {
                responder.SetError(new ProtocolException(e.Message));
            }
        }

        protected override void HandleStepOutRequestAsync(IRequestResponder<StepOutArguments> responder)
        {
            try
            {
                var granularity = responder.Arguments.Granularity.GetValueOrDefault();
                StepInternal(responder.Arguments.ThreadId, enum_STEPKIND.STEP_OUT, granularity, AD7Resources.Error_Scenario_Step_Out);
                responder.SetResponse(new StepOutResponse());
            }
            catch (AD7Exception e)
            {
                responder.SetError(new ProtocolException(e.Message));
            }
        }

        protected override void HandlePauseRequestAsync(IRequestResponder<PauseArguments> responder)
        {
            // TODO: wait for break event
            m_program.CauseBreak();
            responder.SetResponse(new PauseResponse());
        }

        protected override void HandleGotoRequestAsync(IRequestResponder<GotoArguments> responder)
        {
            responder.SetError(new ProtocolException(AD7Resources.Error_NotImplementedSetNextStatement));
        }

        protected override void HandleGotoTargetsRequestAsync(IRequestResponder<GotoTargetsArguments, GotoTargetsResponse> responder)
        {
            var response = new GotoTargetsResponse();

            var source = responder.Arguments.Source;

            // Virtual documents don't have paths
            if (source.Path == null)
            {
                responder.SetResponse(response);
                return;
            }

            try
            {
                string convertedPath = m_pathConverter.ConvertClientPathToDebugger(source.Path);
                int line = m_pathConverter.ConvertClientLineToDebugger(responder.Arguments.Line);
                var docPos = new AD7DocumentPosition(m_sessionConfig, convertedPath, line);

                var targets = new List<GotoTarget>();

                IEnumDebugCodeContexts2 codeContextsEnum;
                if (m_program.EnumCodeContexts(docPos, out codeContextsEnum) == HRConstants.S_OK)
                {
                    var codeContexts = new IDebugCodeContext2[1];
                    uint nProps = 0;
                    while (codeContextsEnum.Next(1, codeContexts, ref nProps) == HRConstants.S_OK)
                    {
                        var codeContext = codeContexts[0];

                        string contextName;
                        codeContext.GetName(out contextName);

                        line = responder.Arguments.Line;
                        IDebugDocumentContext2 documentContext;
                        if (codeContext.GetDocumentContext(out documentContext) == HRConstants.S_OK)
                        {
                            var startPos = new TEXT_POSITION[1];
                            var endPos = new TEXT_POSITION[1];
                            if (documentContext.GetStatementRange(startPos, endPos) == HRConstants.S_OK)
                                line = m_pathConverter.ConvertDebuggerLineToClient((int)startPos[0].dwLine);
                        }

                        string instructionPointerReference = null;
                        CONTEXT_INFO[] contextInfo = new CONTEXT_INFO[1];
                        if (codeContext.GetInfo(enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS, contextInfo) == HRConstants.S_OK &&
                            contextInfo[0].dwFields.HasFlag(enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS))
                        {
                            instructionPointerReference = contextInfo[0].bstrAddress;
                        }

                        int codeContextId = m_nextContextId++;
                        m_gotoCodeContexts.TryAdd(codeContextId, codeContext);

                        targets.Add(new GotoTarget()
                        {
                            Id = codeContextId,
                            Label = contextName,
                            Line = line,
                            InstructionPointerReference = instructionPointerReference
                        });
                    }
                }

                response.Targets = targets;
            }
            catch (AD7Exception e)
            {
                responder.SetError(new ProtocolException(e.Message));
                return;
            }

            responder.SetResponse(response);
        }

        protected override void HandleStackTraceRequestAsync(IRequestResponder<StackTraceArguments, StackTraceResponse> responder)
        {
            int threadReference = responder.Arguments.ThreadId;
            int startFrame = responder.Arguments.StartFrame.GetValueOrDefault(0);
            int levels = responder.Arguments.Levels.GetValueOrDefault(0);

            StackTraceResponse response = new StackTraceResponse()
            {
                TotalFrames = 0
            };

            // Make sure we are stopped and receiving valid input or else return an empty stack trace
            if (m_isStopped && startFrame >= 0 && levels >= 0)
            {
                ThreadFrameEnumInfo frameEnumInfo = null;
                IDebugThread2 thread;
                lock (m_threads)
                {
                    if (m_threads.TryGetValue(threadReference, out thread))
                    {
                        enum_FRAMEINFO_FLAGS flags = enum_FRAMEINFO_FLAGS.FIF_FUNCNAME | // need a function name
                                                        enum_FRAMEINFO_FLAGS.FIF_FRAME | // need a frame object
                                                        enum_FRAMEINFO_FLAGS.FIF_FLAGS |
                                                        enum_FRAMEINFO_FLAGS.FIF_DEBUG_MODULEP;

                        uint radix = Constants.EvaluationRadix;

                        if (responder.Arguments.Format != null)
                        {
                            StackFrameFormat format = responder.Arguments.Format;

                            if (format.Hex == true)
                            {
                                radix = 16;
                            }

                            if (format.Line == true)
                            {
                                flags |= enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_LINES;
                            }

                            if (format.Module == true)
                            {
                                flags |= enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE;
                            }

                            if (format.Parameters == true)
                            {
                                flags |= enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS;
                            }

                            if (format.ParameterNames == true)
                            {
                                flags |= enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_NAMES;
                            }

                            if (format.ParameterTypes == true)
                            {
                                flags |= enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_TYPES;
                            }

                            if (format.ParameterValues == true)
                            {
                                flags |= enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_VALUES;
                            }
                        }
                        else
                        {
                            // No formatting flags provided in the request - use the default format, which includes the module name and argument names / types
                            flags |= enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE |
                                        enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS |
                                        enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_TYPES |
                                        enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_NAMES;
                        }

                        if (m_settingsCallback != null)
                        {
                            // MIEngine generally gets the radix from IDebugSettingsCallback110 rather than using the radix passed
                            m_settingsCallback.Radix = radix;
                        }

                        ErrorBuilder eb = new ErrorBuilder(() => AD7Resources.Error_Scenario_StackTrace);

                        try
                        {
                            eb.CheckHR(thread.EnumFrameInfo(flags, radix, out IEnumDebugFrameInfo2 frameEnum));
                            eb.CheckHR(frameEnum.GetCount(out uint totalFrames));

                            frameEnumInfo = new ThreadFrameEnumInfo(frameEnum, totalFrames);
                        }
                        catch (AD7Exception ex)
                        {
                            responder.SetError(new ProtocolException(ex.Message, ex));
                            return;
                        }
                    }
                    else
                    {
                        // Invalid thread specified
                        responder.SetError(new ProtocolException(String.Format(CultureInfo.CurrentCulture, AD7Resources.Error_PropertyInvalid, StackTraceRequest.RequestType, "threadId")));
                        return;
                    }
                }

                if (startFrame < frameEnumInfo.TotalFrames)
                {
                    if (startFrame != frameEnumInfo.CurrentPosition)
                    {
                        frameEnumInfo.FrameEnum.Reset();
                        frameEnumInfo.CurrentPosition = (uint)startFrame;

                        if (startFrame > 0)
                        {
                            frameEnumInfo.FrameEnum.Skip((uint)startFrame);
                        }
                    }

                    if (levels == 0)
                    {
                        // take the rest of the stack frames
                        levels = (int)frameEnumInfo.TotalFrames - startFrame;
                    }
                    else
                    {
                        levels = Math.Min((int)frameEnumInfo.TotalFrames - startFrame, levels);
                    }

                    FRAMEINFO[] frameInfoArray = new FRAMEINFO[levels];
                    uint framesFetched = 0;
                    frameEnumInfo.FrameEnum.Next((uint)frameInfoArray.Length, frameInfoArray, ref framesFetched);
                    frameEnumInfo.CurrentPosition += framesFetched;

                    for (int i = 0; i < framesFetched; i++)
                    {
                        // TODO: annotated frames?
                        var frameInfo = frameInfoArray[i];
                        IDebugStackFrame2 frame = frameInfo.m_pFrame;

                        int frameReference = 0;
                        TextPositionTuple textPosition = TextPositionTuple.Nil;
                        IDebugCodeContext2 memoryAddress = null;

                        if (frame != null)
                        {
                            frameReference = m_frameHandles.Create(frame);
                            textPosition = TextPositionTuple.GetTextPositionOfFrame(m_pathConverter, frame) ?? TextPositionTuple.Nil;
                            frame.GetCodeContext(out memoryAddress);
                        }

                        int? moduleId = null;
                        IDebugModule2 module = frameInfo.m_pModule;
                        if (module != null)
                        {
                            lock (m_moduleMap)
                            {
                                if (m_moduleMap.TryGetValue(module, out int mapModuleId))
                                {
                                    moduleId = mapModuleId;
                                }
                            }
                        }

                        string instructionPointerReference = null;
                        var contextInfo = new CONTEXT_INFO[1];
                        if (memoryAddress?.GetInfo(enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS, contextInfo) == HRConstants.S_OK && contextInfo[0].dwFields.HasFlag(enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS))
                        {
                            instructionPointerReference = contextInfo[0].bstrAddress;
                        }

                        response.StackFrames.Add(new ProtocolMessages.StackFrame()
                        {
                            Id = frameReference,
                            Name = frameInfo.m_bstrFuncName,
                            Source = textPosition.Source,
                            Line = textPosition.Line,
                            Column = textPosition.Column,
                            ModuleId = moduleId,
                            InstructionPointerReference = instructionPointerReference
                        });
                    }

                    response.TotalFrames = (int)frameEnumInfo.TotalFrames;
                }
            }

            responder.SetResponse(response);
        }

        protected override void HandleScopesRequestAsync(IRequestResponder<ScopesArguments, ScopesResponse> responder)
        {
            int frameReference = responder.Arguments.FrameId;
            ScopesResponse response = new ScopesResponse();

            // if we are not stopped return empty scopes
            if (!m_isStopped)
            {
                responder.SetError(new ProtocolException(AD7Resources.Error_TargetNotStopped, new Message(1105, AD7Resources.Error_TargetNotStopped)));
                return;
            }

            IDebugStackFrame2 frame;
            if (!m_frameHandles.TryGet(frameReference, out frame))
            {
                responder.SetError(new ProtocolException(AD7Resources.Error_StackFrameNotFound));
                return;
            }

            response.Scopes.Add(new Scope()
            {
                Name = AD7Resources.Locals_Scope_Name,
                VariablesReference = m_variableManager.Create(new VariableScope() { StackFrame = frame, Category = VariableCategory.Locals }),
                PresentationHint = Scope.PresentationHintValue.Locals,
                Expensive = false
            });

            // registers should always be present
            // and it's too expensive to read all values just to add the scope
            response.Scopes.Add(new Scope()
            {
                Name = AD7Resources.Registers_Scope_Name,
                VariablesReference = m_variableManager.Create(new VariableScope() { StackFrame = frame, Category = VariableCategory.Registers }),
                PresentationHint = Scope.PresentationHintValue.Registers,
                Expensive = true
            });

            responder.SetResponse(response);
        }

        protected override void HandleVariablesRequestAsync(IRequestResponder<VariablesArguments, VariablesResponse> responder)
        {
            int reference = responder.Arguments.VariablesReference;
            VariablesResponse response = new VariablesResponse();

            // if we are not stopped return empty variables
            if (!m_isStopped)
            {
                responder.SetError(new ProtocolException(AD7Resources.Error_TargetNotStopped, new Message(1105, AD7Resources.Error_TargetNotStopped)));
                return;
            }

            uint radix = Constants.EvaluationRadix;

            if (responder.Arguments.Format != null)
            {
                ValueFormat format = responder.Arguments.Format;

                if (format.Hex == true)
                {
                    radix = 16;
                }
            }

            if (m_settingsCallback != null)
            {
                // MIEngine generally gets the radix from IDebugSettingsCallback110 rather than using the radix passed
                m_settingsCallback.Radix = radix;
            }

            Object container;
            if (!m_variableManager.TryGet(reference, out container))
            {
                responder.SetResponse(response);
                return;
            }
            if (container is VariableScope variableScope)
            {
                response = VariablesFromFrame(variableScope, radix);
                responder.SetResponse(response);
                return;
            }

            if (!(container is VariableEvaluationData variableEvaluationData))
            {
                Debug.Assert(false, "Unexpected type in _variableHandles collection");
                responder.SetResponse(response);
                return;
            }

            Guid empty = Guid.Empty;
            IDebugProperty2 property = variableEvaluationData.DebugProperty;
            if (property.EnumChildren(variableEvaluationData.propertyInfoFlags, radix, ref empty, enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ALL, null, Constants.EvaluationTimeout, out IEnumDebugPropertyInfo2 childEnum) == 0)
            {
                uint count;
                childEnum.GetCount(out count);
                if (count > 0)
                {
                    DEBUG_PROPERTY_INFO[] childProperties = new DEBUG_PROPERTY_INFO[count];
                    childEnum.Next(count, childProperties, out count);

                    if (count > 1)
                    {
                        // Ensure that items with duplicate names such as multiple anonymous unions will display in VS Code
                        var variablesDictionary = new Dictionary<string, Variable>();
                        for (uint c = 0; c < count; c++)
                        {
                            string memoryReference = AD7Utils.GetMemoryReferenceFromIDebugProperty(childProperties[c].pProperty);
                            var variable = m_variableManager.CreateVariable(ref childProperties[c], variableEvaluationData.propertyInfoFlags, memoryReference);
                            m_variableManager.AddChildVariable(reference, childProperties[c]);
                            int uniqueCounter = 2;
                            string variableName = variable.Name;
                            string variableNameFormat = "{0} #{1}";
                            while (variablesDictionary.ContainsKey(variableName))
                            {
                                variableName = String.Format(CultureInfo.InvariantCulture, variableNameFormat, variable.Name, uniqueCounter++);
                            }

                            variable.Name = variableName;
                            variablesDictionary[variableName] = variable;
                        }

                        response.Variables.AddRange(variablesDictionary.Values);
                    }
                    else
                    {
                        string memoryReference = AD7Utils.GetMemoryReferenceFromIDebugProperty(childProperties[0].pProperty);
                        // Shortcut when no duplicate can exist
                        response.Variables.Add(m_variableManager.CreateVariable(ref childProperties[0], variableEvaluationData.propertyInfoFlags, memoryReference));
                    }
                }
            }
            responder.SetResponse(response);
        }

        protected override void HandleSetVariableRequestAsync(IRequestResponder<SetVariableArguments, SetVariableResponse> responder)
        {
            string name = responder.Arguments.Name;
            string value = responder.Arguments.Value;
            int reference = responder.Arguments.VariablesReference;

            // if we are not stopped don't try to set
            if (!m_isStopped)
            {
                responder.SetError(new ProtocolException(AD7Resources.Error_TargetNotStopped, new Message(1105, AD7Resources.Error_TargetNotStopped)));
                return;
            }

            object container;
            if (!m_variableManager.TryGet(reference, out container))
            {
                responder.SetError(new ProtocolException(AD7Resources.Error_VariableNotFound, new Message(1106, AD7Resources.Error_VariableNotFound)));
                return;
            }

            enum_DEBUGPROP_INFO_FLAGS flags = GetDefaultPropertyInfoFlags();
            IDebugProperty2 property = null;
            IEnumDebugPropertyInfo2 varEnum = null;
            int hr = HRConstants.E_FAIL;
            if (container is VariableScope variableScope)
            {
                Guid filter = Guid.Empty;
                switch (variableScope.Category)
                {
                case VariableCategory.Locals:
                    filter = s_guidFilterAllLocalsPlusArgs;
                    break;
                case VariableCategory.Registers:
                    filter = s_guidFilterRegisters;
                    break;
                }

                hr = variableScope.StackFrame.EnumProperties(
                    flags,
                    Constants.EvaluationRadix,
                    ref filter,
                    Constants.EvaluationTimeout,
                    out _,
                    out varEnum);
            }
            else if (container is VariableEvaluationData)
            {
                IDebugProperty2 debugProperty = ((VariableEvaluationData)container).DebugProperty;
                if (debugProperty == null)
                {
                    responder.SetError(new ProtocolException(AD7Resources.Error_VariableNotFound, new Message(1106, AD7Resources.Error_VariableNotFound)));
                    return;
                }

                hr = debugProperty.EnumChildren(
                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP | enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME | enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB,
                    Constants.EvaluationRadix,
                    ref s_guidFilterAllLocalsPlusArgs,
                    enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ALL,
                    name,
                    Constants.EvaluationTimeout,
                    out varEnum);
            }

            if (hr == HRConstants.S_OK && varEnum != null)
            {
                DEBUG_PROPERTY_INFO[] props = new DEBUG_PROPERTY_INFO[1];
                uint nProps;
                while (varEnum.Next(1, props, out nProps) == HRConstants.S_OK)
                {
                    if (props[0].bstrName == name)
                    {
                        // Make sure we can assign to this variable.
                        if (props[0].dwAttrib.HasFlag(enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_READONLY))
                        {
                            string message = string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_VariableIsReadonly, name);
                            responder.SetError(new ProtocolException(message, new Message(1107, message)));
                            return;
                        }

                        property = props[0].pProperty;
                        break;
                    }
                }
            }

            if (property == null)
            {
                responder.SetError(new ProtocolException(AD7Resources.Error_VariableNotFound, new Message(1106, AD7Resources.Error_VariableNotFound)));
                return;
            }

            string error = null;
            if (property is IDebugProperty3)
            {
                hr = ((IDebugProperty3)property).SetValueAsStringWithError(value, Constants.EvaluationRadix, Constants.EvaluationTimeout, out error);
            }
            else
            {
                hr = property.SetValueAsString(value, Constants.EvaluationRadix, Constants.EvaluationTimeout);
            }

            if (hr != HRConstants.S_OK)
            {
                string message = error ?? AD7Resources.Error_SetVariableFailed;
                responder.SetError(new ProtocolException(message, new Message(1107, message)));
                return;
            }

            responder.SetResponse(new SetVariableResponse
            {
                Value = m_variableManager.CreateVariable(property, flags).Value
            });
        }

        /// <summary>
        /// Currently unsupported. This message can be received when we return a source file that doesn't exist (such as a library within gdb).
        /// See github issue: microsoft/vscode-cpptools#3662
        /// </summary>
        protected override void HandleSourceRequestAsync(IRequestResponder<SourceArguments, SourceResponse> responder)
        {
            responder.SetError(new ProtocolException("'SourceRequest' not supported."));
        }

        protected override void HandleThreadsRequestAsync(IRequestResponder<ThreadsArguments, ThreadsResponse> responder)
        {
            ThreadsResponse response = new ThreadsResponse();

            // Make a copy of the threads list
            Dictionary<int, IDebugThread2> threads;
            lock (m_threads)
            {
                threads = new Dictionary<int, IDebugThread2>(m_threads);
            }

            // iterate over the collection asking the engine for the name
            foreach (var pair in threads)
            {
                string name;
                pair.Value.GetName(out name);
                response.Threads.Add(new OpenDebugThread(pair.Key, name));
            }

            responder.SetResponse(response);
        }

        private ProtocolMessages.Module ConvertToModule(in IDebugModule2 module, int moduleId)
        {
            var debugModuleInfos = new MODULE_INFO[1];
            if (module.GetInfo(enum_MODULE_INFO_FIELDS.MIF_ALLFIELDS, debugModuleInfos) == HRConstants.S_OK)
            {
                var debugModuleInfo = debugModuleInfos[0];

                var path = debugModuleInfo.m_bstrUrl;
                var vsTimestampUTC = (debugModuleInfo.dwValidFields & enum_MODULE_INFO_FIELDS.MIF_TIMESTAMP) != 0 ? FileTimeToPosix(debugModuleInfo.m_TimeStamp).ToString(CultureInfo.InvariantCulture) : null;
                var version = debugModuleInfo.m_bstrVersion;
                var vsLoadAddress = debugModuleInfo.m_addrLoadAddress.ToString(CultureInfo.InvariantCulture);
                var vsPreferredLoadAddress = debugModuleInfo.m_addrPreferredLoadAddress.ToString(CultureInfo.InvariantCulture);
                var vsModuleSize = (int)debugModuleInfo.m_dwSize;
                var vsLoadOrder = (int)debugModuleInfo.m_dwLoadOrder;
                var symbolFilePath = debugModuleInfo.m_bstrUrlSymbolLocation;
                var symbolStatus = debugModuleInfo.m_bstrDebugMessage;
                var vsIs64Bit = (debugModuleInfo.m_dwModuleFlags & enum_MODULE_FLAGS.MODULE_FLAG_64BIT) != 0;

                ProtocolMessages.Module mod = new ProtocolMessages.Module(moduleId, debugModuleInfo.m_bstrName)
                {
                    Path = path, VsTimestampUTC = vsTimestampUTC, Version = version, VsLoadAddress = vsLoadAddress, VsPreferredLoadAddress = vsPreferredLoadAddress,
                    VsModuleSize = vsModuleSize, VsLoadOrder = vsLoadOrder, SymbolFilePath = symbolFilePath, SymbolStatus = symbolStatus, VsIs64Bit = vsIs64Bit
                };

                // IsOptimized and IsUserCode are not set by gdb
                if((debugModuleInfo.m_dwModuleFlags & enum_MODULE_FLAGS.MODULE_FLAG_OPTIMIZED) != 0)
                {
                    mod.IsOptimized = true;
                } else if ((debugModuleInfo.m_dwModuleFlags & enum_MODULE_FLAGS.MODULE_FLAG_UNOPTIMIZED) != 0)
                {
                    mod.IsOptimized = false;
                }
                if (module is IDebugModule3 module3 && module3.IsUserCode(out int isUserCode) == HRConstants.S_OK)
                {
                    if (isUserCode == 0)
                    {
                        mod.IsUserCode = false;
                    }
                    else
                    {
                        mod.IsUserCode = true;
                    }
                }

                return mod;
            }
            return null;
        }

        protected override void HandleModulesRequestAsync(IRequestResponder<ModulesArguments, ModulesResponse> responder)
        {
            var response = new ModulesResponse();
            IEnumDebugModules2 enumDebugModules;
            if (m_program.EnumModules(out enumDebugModules) == HRConstants.S_OK)
            {
                var debugModules = new IDebugModule2[1];
                uint numReturned = 0;
                while (enumDebugModules.Next(1, debugModules, ref numReturned) == HRConstants.S_OK && numReturned == 1)
                {
                    IDebugModule2 module = debugModules[0];
                    int moduleId;
                    lock (m_moduleMap)
                    {
                        if (!m_moduleMap.TryGetValue(module, out moduleId))
                        {
                            Debug.Fail("Missing ModuleLoadEvent?");
                            continue;
                        }
                    }
                    var mod = ConvertToModule(module, moduleId);
                    response.Modules.Add(mod);
                }
            }
            responder.SetResponse(response);
        }

        protected override void HandleDisassembleRequestAsync(IRequestResponder<DisassembleArguments, DisassembleResponse> responder)
        {
            DisassembleResponse response = new DisassembleResponse();
            DisassembleArguments disassembleArguments = responder.Arguments;
            Debug.Assert(!string.IsNullOrEmpty(disassembleArguments.MemoryReference));
            try
            {
                ErrorBuilder eb = new ErrorBuilder(() => AD7Resources.Error_Scenario_Disassemble);

                eb.CheckHR(GetMemoryContext(disassembleArguments.MemoryReference, disassembleArguments.Offset, out IDebugMemoryContext2 memoryContext, out ulong address));
                IDebugCodeContext2 codeContext = memoryContext as IDebugCodeContext2;
                if (codeContext == null)
                {
                    eb.CheckHR(HRConstants.E_NOTIMPL);
                }

                eb.CheckHR(m_program.GetDisassemblyStream(enum_DISASSEMBLY_STREAM_SCOPE.DSS_ALL, codeContext, out IDebugDisassemblyStream2 disassemblyStream));
                if (disassembleArguments.InstructionOffset.GetValueOrDefault(0) != 0)
                {
                    eb.CheckHR(disassemblyStream.Seek(enum_SEEK_START.SEEK_START_BEGIN, codeContext, address, (long)disassembleArguments.InstructionOffset));
                }

                DisassemblyData[] prgDisassembly = new DisassemblyData[disassembleArguments.InstructionCount];
                eb.CheckHR(disassemblyStream.Read((uint)disassembleArguments.InstructionCount, enum_DISASSEMBLY_STREAM_FIELDS.DSF_ALL, out uint pdwInstructionsRead, prgDisassembly));
                Debug.Assert(disassembleArguments.InstructionCount == pdwInstructionsRead);
                foreach (DisassemblyData data in prgDisassembly)
                {
                    if (data.dwFlags.HasFlag(enum_DISASSEMBLY_FLAGS.DF_HASSOURCE))
                    {
                        Debug.Fail("Warning: engine supports mixed instruction/source disassembly, but OpenDebugAD7 does not.");
                    }
                    DisassembledInstruction instruction = new DisassembledInstruction() {
                        Address = data.bstrAddress,
                        InstructionBytes = data.bstrCodeBytes,
                        Instruction = data.bstrOpcode,
                        Symbol = data.bstrSymbol
                    };
                    response.Instructions.Add(instruction);
                }
                responder.SetResponse(response);
            } catch (Exception e) {
                responder.SetError(new ProtocolException(e.Message));
            }
        }

        protected override void HandleSetBreakpointsRequestAsync(IRequestResponder<SetBreakpointsArguments, SetBreakpointsResponse> responder)
        {
            SetBreakpointsResponse response = new SetBreakpointsResponse();

            string path = null;
            string name = null;

            if (responder.Arguments.Source != null)
            {
                string p = responder.Arguments.Source.Path;
                if (p != null && p.Trim().Length > 0)
                {
                    path = p;
                }
                string nm = responder.Arguments.Source.Name;
                if (nm != null && nm.Trim().Length > 0)
                {
                    name = nm;
                }
            }

            var source = new Source()
            {
                Name = name,
                Path = path,
                SourceReference = 0
            };

            List<SourceBreakpoint> breakpoints = responder.Arguments.Breakpoints;

            bool sourceModified = responder.Arguments.SourceModified.GetValueOrDefault(false);

            // we do not support other sources than 'path'
            if (source.Path != null)
            {
                ErrorBuilder eb = new ErrorBuilder(() => AD7Resources.Error_UnableToSetBreakpoint);

                try
                {
                    string convertedPath = m_pathConverter.ConvertClientPathToDebugger(source.Path);

                    if (Utilities.IsWindows() && convertedPath.Length > 2)
                    {
                        // vscode may send drive letters with inconsistent casing which will mess up the key
                        // in the dictionary.  see https://github.com/Microsoft/vscode/issues/6268
                        // Normalize the drive letter casing. Note that drive letters
                        // are not localized so invariant is safe here.
                        string drive = convertedPath.Substring(0, 2);
                        if (char.IsLower(drive[0]) && drive.EndsWith(":", StringComparison.Ordinal))
                        {
                            convertedPath = String.Concat(drive.ToUpperInvariant(), convertedPath.Substring(2));
                        }
                    }

                    HashSet<int> lines = new HashSet<int>(breakpoints.Select((b) => b.Line));

                    Dictionary<int, IDebugPendingBreakpoint2> dict = null;
                    if (m_breakpoints.ContainsKey(convertedPath))
                    {
                        dict = m_breakpoints[convertedPath];
                        var keys = new int[dict.Keys.Count];
                        dict.Keys.CopyTo(keys, 0);
                        foreach (var l in keys)
                        {
                            // Delete all breakpoints that are no longer listed.
                            // In the case of modified source, delete everything.
                            if (!lines.Contains(l) || sourceModified)
                            {
                                var bp = dict[l];
                                bp.Delete();
                                dict.Remove(l);
                            }
                        }
                    }
                    else
                    {
                        dict = new Dictionary<int, IDebugPendingBreakpoint2>();
                        m_breakpoints[convertedPath] = dict;
                    }

                    var resBreakpoints = new List<Breakpoint>();
                    foreach (var bp in breakpoints)
                    {
                        if (dict.ContainsKey(bp.Line))
                        {
                            // already created
                            IDebugBreakpointRequest2 breakpointRequest;
                            if (dict[bp.Line].GetBreakpointRequest(out breakpointRequest) == 0 && 
                                breakpointRequest is AD7BreakPointRequest ad7BPRequest)
                            {
                                // Check to see if this breakpoint has a condition that has changed.
                                if (!StringComparer.Ordinal.Equals(ad7BPRequest.Condition, bp.Condition))
                                {
                                    // Condition has been modified. Delete breakpoint so it will be recreated with the updated condition.
                                    var toRemove = dict[bp.Line];
                                    toRemove.Delete();
                                    dict.Remove(bp.Line);
                                }
                                // Check to see if tracepoint changed
                                else if (!StringComparer.Ordinal.Equals(ad7BPRequest.LogMessage, bp.LogMessage))
                                {
                                    ad7BPRequest.ClearTracepoint();
                                    var toRemove = dict[bp.Line];
                                    toRemove.Delete();
                                    dict.Remove(bp.Line);
                                }
                                else
                                {
                                    if (ad7BPRequest.BindResult != null)
                                    {
                                        // use the breakpoint created from IDebugBreakpointErrorEvent2 or IDebugBreakpointBoundEvent2
                                        resBreakpoints.Add(ad7BPRequest.BindResult);
                                    }
                                    else
                                    {
                                        resBreakpoints.Add(new Breakpoint()
                                        {
                                            Id = (int)ad7BPRequest.Id,
                                            Verified = true,
                                            Line = bp.Line
                                        });
                                    }
                                    continue;
                                }
                            }
                        }


                        // Create a new breakpoint
                        if (!dict.ContainsKey(bp.Line))
                        {
                            IDebugPendingBreakpoint2 pendingBp;
                            AD7BreakPointRequest pBPRequest = new AD7BreakPointRequest(m_sessionConfig, convertedPath, m_pathConverter.ConvertClientLineToDebugger(bp.Line), bp.Condition);

                            try
                            {
                                bool verified = true;
                                if (!string.IsNullOrEmpty(bp.LogMessage))
                                {
                                    // Make sure tracepoint is valid.
                                    verified = pBPRequest.SetLogMessage(bp.LogMessage);
                                }

                                if (verified)
                                {
                                    eb.CheckHR(m_engine.CreatePendingBreakpoint(pBPRequest, out pendingBp));
                                    eb.CheckHR(pendingBp.Bind());

                                    dict[bp.Line] = pendingBp;

                                    resBreakpoints.Add(new Breakpoint()
                                    {
                                        Id = (int)pBPRequest.Id,
                                        Verified = verified,
                                        Line = bp.Line
                                    });
                                }
                                else
                                {
                                    resBreakpoints.Add(new Breakpoint()
                                    {
                                        Id = (int)pBPRequest.Id,
                                        Verified = verified,
                                        Line = bp.Line,
                                        Message = string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_UnableToParseLogMessage)
                                    });
                                }
                            }
                            catch (Exception e)
                            {
                                e = Utilities.GetInnerMost(e);
                                if (Utilities.IsCorruptingException(e))
                                {
                                    Utilities.ReportException(e);
                                }

                                resBreakpoints.Add(new Breakpoint()
                                {
                                    Id = (int)pBPRequest.Id,
                                    Verified = false,
                                    Line = bp.Line,
                                    Message = eb.GetMessageForException(e)
                                });
                            }
                        }
                    }

                    response.Breakpoints = resBreakpoints;
                }
                catch (Exception e)
                {
                    // If setBreakpoints returns an error vscode aborts launch, so we never want to return an error,
                    // so convert this to failure results

                    e = Utilities.GetInnerMost(e);
                    if (Utilities.IsCorruptingException(e))
                    {
                        Utilities.ReportException(e);
                    }

                    string message = eb.GetMessageForException(e);
                    List<Breakpoint> resBreakpoints = breakpoints.Select(bp => new Breakpoint()
                    {
                        Id = (int)AD7BreakPointRequest.GetNextBreakpointId(),
                        Verified = false,
                        Line = bp.Line,
                        Message = message
                    }).ToList();

                    response.Breakpoints = resBreakpoints;
                }
            }

            responder.SetResponse(response);
        }

        protected override void HandleDataBreakpointInfoRequestAsync(IRequestResponder<DataBreakpointInfoArguments, DataBreakpointInfoResponse> responder)
        {
            if (responder.Arguments.Name == null)
            {
                responder.SetError(new ProtocolException("DataBreakpointInfo failed: Missing 'Name'."));
                return;
            }

            DataBreakpointInfoResponse response = new DataBreakpointInfoResponse();

            try
            {
                string name = responder.Arguments.Name;
                IDebugProperty2 property = null;
                string errorMessage = null;
                ErrorBuilder eb = new ErrorBuilder(() => AD7Resources.Error_DataBreakpointInfoFail);
                int hr = HRConstants.S_OK;

                // Did our request come with a parent object?
                if (responder.Arguments.VariablesReference.HasValue)
                {
                    int variableReference = responder.Arguments.VariablesReference.Value;
                    if (!m_variableManager.TryGet(variableReference, out object variableObj))
                    {
                        responder.SetError(new ProtocolException("DataBreakpointInfo failed: Invalid 'VariableReference'."));
                        return;
                    }

                    if (variableObj is VariableScope varScope)
                    {
                        // We have a scope object. We can grab a frame for evaluation from this
                        IDebugStackFrame2 frame = varScope.StackFrame;
                        m_variableManager.TryGetProperty((frame, name), out property);
                    }
                    else if (variableObj is VariableEvaluationData varEvalData)
                    {
                        // We have a variable parent object.
                        IDebugProperty2 parentProperty = varEvalData.DebugProperty;
                        m_variableManager.TryGetProperty((variableReference, name), out property);
                    }
                }
                else
                {
                    // We don't have a parent object. Default to using top stack frame
                    if (m_frameHandles == null || !m_frameHandles.TryGetFirst(out IDebugStackFrame2 frame))
                    {
                        response.Description = string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_DataBreakpointInfoFail, AD7Resources.Error_NoParentObject);
                    }
                    else
                    {
                        m_variableManager.TryGetProperty((frame, name), out property);
                    }
                }

                // If we've found a valid child property to set the data breakpoint on, get the address/size and return the DataId.
                if (property != null && property is IDebugProperty160 property160)
                {
                    hr = property160.GetDataBreakpointInfo160(out string address, out uint size, out string displayName, out errorMessage);
                    eb.CheckHR(hr);
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        response.Description = string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_DataBreakpointInfoFail, errorMessage);
                    }
                    else
                    {
                        // If we succeeded, return response that we can set a data bp.
                        string sSize = size.ToString(CultureInfo.InvariantCulture);
                        response.DataId = $"{address},{sSize}";
                        response.Description = string.Format(CultureInfo.CurrentCulture, AD7Resources.DataBreakpointDisplayString, displayName, sSize);
                        response.AccessTypes = new List<DataBreakpointAccessType>() { DataBreakpointAccessType.Write };
                    }
                }
                else if (response.Description == null)
                {
                    response.Description = string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_DataBreakpointInfoFail, AD7Resources.Error_ChildPropertyNotFound);
                }
            }
            catch (Exception ex)
            {
                if (ex is AD7Exception ad7ex)
                    response.Description = string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_DataBreakpointInfoFail, ad7ex.Message);
                else
                    response.Description = string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_DataBreakpointInfoFail, "");
            }
            finally
            {
                responder.SetResponse(response);
            }
        }

        protected override void HandleSetDataBreakpointsRequestAsync(IRequestResponder<SetDataBreakpointsArguments, SetDataBreakpointsResponse> responder)
        {
            if (responder.Arguments.Breakpoints == null)
            {
                responder.SetError(new ProtocolException("SetDataBreakpointRequest failed: Missing 'breakpoints'."));
                return;
            }

            List<DataBreakpoint> breakpoints = responder.Arguments.Breakpoints;
            SetDataBreakpointsResponse response = new SetDataBreakpointsResponse();
            Dictionary<string, IDebugPendingBreakpoint2> newBreakpoints = new Dictionary<string, IDebugPendingBreakpoint2>();
            ErrorBuilder eb = new ErrorBuilder(() => AD7Resources.Error_DataBreakpointInfoFail);

            try
            {
                foreach (KeyValuePair<string, IDebugPendingBreakpoint2> b in m_dataBreakpoints)
                {
                    if (breakpoints.Find((p) => p.DataId == b.Key) != null)
                    {
                        newBreakpoints[b.Key] = b.Value;    // breakpoint still in new list
                    }
                    else
                    {
                        b.Value.Delete();   // not in new list so delete it
                    }
                }

                foreach (DataBreakpoint b in breakpoints)
                {
                    if (m_dataBreakpoints.ContainsKey(b.DataId))
                    {   // already created
                        IDebugBreakpointRequest2 breakpointRequest;
                        if (m_dataBreakpoints[b.DataId].GetBreakpointRequest(out breakpointRequest) == 0 &&
                                    breakpointRequest is AD7BreakPointRequest ad7BPRequest)
                        {
                            // Check to see if this breakpoint has a condition that has changed.
                            if (!StringComparer.Ordinal.Equals(ad7BPRequest.Condition, b.Condition))
                            {
                                // Condition has been modified. Delete breakpoint so it will be recreated with the updated condition.
                                var toRemove = m_dataBreakpoints[b.DataId];
                                toRemove.Delete();
                                m_dataBreakpoints.Remove(b.DataId);
                            }
                            else
                            {
                                if (ad7BPRequest.BindResult != null)
                                {
                                    response.Breakpoints.Add(ad7BPRequest.BindResult);
                                }
                                else
                                {
                                    response.Breakpoints.Add(new Breakpoint()
                                    {
                                        Id = (int)ad7BPRequest.Id,
                                        Verified = false,
                                        Line = 0
                                    });

                                }
                                continue;
                            }
                        }
                    }

                    // Bind the new data bp
                    if (!m_dataBreakpoints.ContainsKey(b.DataId))
                    {
                        int hr = HRConstants.S_OK;

                        int lastCommaIdx = b.DataId.LastIndexOf(',');
                        if (lastCommaIdx == -1)
                        {
                            eb.ThrowHR(HRConstants.E_FAIL);
                        }

                        // format is "{dataId},{size}" where dataId = "{address},{displayName}"
                        string strSize = b.DataId.Substring(lastCommaIdx + 1);
                        string address = b.DataId.Substring(0, lastCommaIdx);
                        uint size = uint.Parse(strSize, CultureInfo.InvariantCulture);

                        AD7BreakPointRequest pBPRequest = new AD7BreakPointRequest(address, size);
                        hr = m_engine.CreatePendingBreakpoint(pBPRequest, out IDebugPendingBreakpoint2 pendingBp);
                        eb.CheckHR(hr);

                        hr = pendingBp.Bind();
                        if (hr == HRConstants.S_OK)
                        {
                            newBreakpoints[b.DataId] = pendingBp;
                        }
                        response.Breakpoints.Add(pBPRequest.BindResult);
                    }
                }
                responder.SetResponse(response);
            }
            catch (Exception ex)
            {
                responder.SetError(new ProtocolException(ex.Message));
            }
            finally
            {
                m_dataBreakpoints = newBreakpoints;
            }
        }


        protected override void HandleSetExceptionBreakpointsRequestAsync(IRequestResponder<SetExceptionBreakpointsArguments> responder)
        {
            HashSet<Guid> activeExceptionCategories = new HashSet<Guid>();

            List<ExceptionFilterOptions> filterOptions = responder.Arguments.FilterOptions;
            if (filterOptions != null && filterOptions.Count > 0)
            {
                foreach (ExceptionFilterOptions filterOption in filterOptions)
                {
                    ExceptionBreakpointFilter filter = m_engineConfiguration.ExceptionSettings.ExceptionBreakpointFilters.FirstOrDefault(x => x.filter == filterOption.FilterId);

                    if (filter != null)
                    {
                        // Mark category as active
                        activeExceptionCategories.Add(filter.categoryId);

                        // Handle exceptions with a specific exception class.
                        if (!string.IsNullOrWhiteSpace(filterOption.Condition))
                        {
                            string[] conditions = filterOption.Condition.Split(',');

                            // Validate condition strings
                            List<string> validConditions = new List<string>();
                            foreach (string condition in conditions)
                            {
                                string conditionTrimmed = condition.Trim();
                                if (LanguageUtilities.IsValidIdentifier(conditionTrimmed))
                                {
                                    validConditions.Add(conditionTrimmed);
                                }
                                else
                                {
                                    m_logger.WriteLine(LoggingCategory.StdErr, string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_Invalid_Exception_Condition, conditionTrimmed));
                                }
                            }

                            IEnumerable<string> toAdd = validConditions.Except(m_exceptionBreakpoints).ToList().Distinct();
                            foreach (string condition in toAdd)
                            {
                                var exceptionInfo = new EXCEPTION_INFO[1];
                                exceptionInfo[0].dwState = filter.State;
                                exceptionInfo[0].guidType = filter.categoryId;
                                exceptionInfo[0].bstrExceptionName = condition;
                                m_engine.SetException(exceptionInfo);

                                m_exceptionBreakpoints.Add(condition);
                            }

                            IEnumerable<string> toRemove = m_exceptionBreakpoints.Except(validConditions).ToList().Distinct();
                            foreach (string condition in toRemove)
                            {
                                var exceptionInfo = new EXCEPTION_INFO[1];
                                exceptionInfo[0].dwState = filter.State;
                                exceptionInfo[0].guidType = filter.categoryId;
                                exceptionInfo[0].bstrExceptionName = condition;

                                m_engine.RemoveSetException(exceptionInfo);
                                m_exceptionBreakpoints.Remove(condition);
                            }
                        }
                        else
                        {
                            // Enable all exceptions
                            SetCategoryGuidExceptions(filter.categoryId, filter.State);
                        }
                    }
                    else
                    {
                        m_logger.WriteLine(LoggingCategory.StdErr, string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_FilterOption_Not_Supported, filterOption.FilterId));
                    }
                }
            }
            else
            {
                List<string> filters = responder.Arguments.Filters;
                if (filters != null)
                {
                    foreach (string filter in filters)
                    {
                        ExceptionBreakpointFilter breakpointFilter = m_engineConfiguration.ExceptionSettings.ExceptionBreakpointFilters.FirstOrDefault(ebf => ebf.filter == filter);
                        if (breakpointFilter != null)
                        {
                            activeExceptionCategories.Add(breakpointFilter.categoryId);
                            SetCategoryGuidExceptions(breakpointFilter.categoryId, breakpointFilter.State);
                        }
                        else
                        {
                            Debug.Fail("Unknown exception filter " + filter);
                        }
                    }
                }
            }

            // Disable unused filters
            IEnumerable<ExceptionSettings.CategoryConfiguration> unusedCategories = m_engineConfiguration.ExceptionSettings.Categories.Where(c => !activeExceptionCategories.Contains(c.Id));
            foreach (ExceptionSettings.CategoryConfiguration category in unusedCategories)
            {
                SetExceptionCategory(category, enum_EXCEPTION_STATE.EXCEPTION_STOP_SECOND_CHANCE);
            }

            responder.SetResponse(new SetExceptionBreakpointsResponse());
        }

        protected override void HandleSetFunctionBreakpointsRequestAsync(IRequestResponder<SetFunctionBreakpointsArguments, SetFunctionBreakpointsResponse> responder)
        {
            if (responder.Arguments.Breakpoints == null)
            {
                responder.SetError(new ProtocolException("SetFunctionBreakpointRequest failed: Missing 'breakpoints'."));
                return;
            }

            List<FunctionBreakpoint> breakpoints = responder.Arguments.Breakpoints;
            Dictionary<string, IDebugPendingBreakpoint2> newBreakpoints = new Dictionary<string, IDebugPendingBreakpoint2>();

            SetFunctionBreakpointsResponse response = new SetFunctionBreakpointsResponse();

            foreach (KeyValuePair<string, IDebugPendingBreakpoint2> b in m_functionBreakpoints)
            {
                if (breakpoints.Find((p) => p.Name == b.Key) != null)
                {
                    newBreakpoints[b.Key] = b.Value;    // breakpoint still in new list
                }
                else
                {
                    b.Value.Delete();   // not in new list so delete it
                }
            }

            foreach (FunctionBreakpoint b in breakpoints)
            {
                if (m_functionBreakpoints.ContainsKey(b.Name))
                {   // already created
                    IDebugBreakpointRequest2 breakpointRequest;
                    if (m_functionBreakpoints[b.Name].GetBreakpointRequest(out breakpointRequest) == 0 &&
                                breakpointRequest is AD7BreakPointRequest ad7BPRequest)
                    {
                        // Check to see if this breakpoint has a condition that has changed.
                        if (!StringComparer.Ordinal.Equals(ad7BPRequest.Condition, b.Condition))
                        {
                            // Condition has been modified. Delete breakpoint so it will be recreated with the updated condition.
                            var toRemove = m_functionBreakpoints[b.Name];
                            toRemove.Delete();
                            m_functionBreakpoints.Remove(b.Name);
                        }
                        else
                        {
                            if (ad7BPRequest.BindResult != null)
                            {
                                response.Breakpoints.Add(ad7BPRequest.BindResult);
                            }
                            else
                            {
                                response.Breakpoints.Add(new Breakpoint()
                                {
                                    Id = (int)ad7BPRequest.Id,
                                    Verified = true,
                                    Line = 0
                                });

                            }
                            continue;
                        }
                    }
                }

                // bind the new function names
                if (!m_functionBreakpoints.ContainsKey(b.Name))
                {
                    IDebugPendingBreakpoint2 pendingBp;
                    AD7BreakPointRequest pBPRequest = new AD7BreakPointRequest(b.Name);

                    int hr = m_engine.CreatePendingBreakpoint(pBPRequest, out pendingBp);

                    if (hr == HRConstants.S_OK && pendingBp != null)
                    {
                        hr = pendingBp.Bind();
                    }

                    if (hr == HRConstants.S_OK)
                    {
                        newBreakpoints[b.Name] = pendingBp;
                        response.Breakpoints.Add(new Breakpoint()
                        {
                            Id = (int)pBPRequest.Id,
                            Verified = true,
                            Line = 0
                        }); // success
                    }
                    else
                    {
                        response.Breakpoints.Add(new Breakpoint()
                        {
                            Id = (int)pBPRequest.Id,
                            Verified = false,
                            Line = 0
                        }); // couldn't create and/or bind
                    }
                }
            }

            m_functionBreakpoints = newBreakpoints;

            responder.SetResponse(response);
        }

        protected override void HandleCompletionsRequestAsync(IRequestResponder<CompletionsArguments, CompletionsResponse> responder)
        {
            if (!m_isStopped)
            {
                responder.SetError(new ProtocolException("Failed to handle CompletionsRequest", new Message(1105, AD7Resources.Error_TargetNotStopped)));
                return;
            }

            IDebugStackFrame2 frame = null;
            int? frameId = responder.Arguments.FrameId;
            if (frameId != null)
                _ = m_frameHandles.TryGet(frameId.Value, out frame);

            try
            {
                string command = responder.Arguments.Text;
                var matchlist = new List<CompletionItem>();

                var debugProgram = m_engine as IDebugProgramDAP;

                if (debugProgram.AutoComplete(command, frame, out string[] results) == HRConstants.S_OK)
                {
                    foreach (string result in results)
                    {
                        matchlist.Add(new CompletionItem()
                        {
                            Label = result,
                            Start = 0,
                            Type = CompletionItemType.Text,
                            Length = result.Length
                        });
                    }
                }

                responder.SetResponse(new CompletionsResponse(matchlist));
            }
            catch (NotImplementedException)
            {
                // If MIDebugEngine does not implemented AutoCompleted, just return an empty response.
                responder.SetResponse(new CompletionsResponse());
            }
            catch (Exception e)
            {
                responder.SetError(new ProtocolException("Auto-completion failed!", e));
            }
        }

        protected override void HandleEvaluateRequestAsync(IRequestResponder<EvaluateArguments, EvaluateResponse> responder)
        {
            EvaluateArguments.ContextValue context = responder.Arguments.Context.GetValueOrDefault(EvaluateArguments.ContextValue.Unknown);
            int frameId = responder.Arguments.FrameId.GetValueOrDefault(-1);
            string expression = responder.Arguments.Expression;

            if (expression == null)
            {
                responder.SetError(new ProtocolException("Failed to handle EvaluateRequest: Missing 'expression'"));
                return;
            }

            // if we are not stopped, return evaluation failure
            if (!m_isStopped)
            {
                responder.SetError(new ProtocolException("Failed to handle EvaluateRequest", new Message(1105, AD7Resources.Error_TargetNotStopped)));
                return;
            }
            DateTime evaluationStartTime = DateTime.Now;

            bool isExecInConsole = false;
            // If the expression isn't empty and its a Repl request, do additional checking
            if (!String.IsNullOrEmpty(expression) && context == EvaluateArguments.ContextValue.Repl)
            {
                // If this is an -exec command (or starts with '`') treat it as a console command and log telemetry
                if (expression.StartsWith("-exec", StringComparison.Ordinal) || expression[0] == '`')
                    isExecInConsole = true;
            }

            int hr;
            ErrorBuilder eb = new ErrorBuilder(() => AD7Resources.Error_Scenario_Evaluate);
            IDebugStackFrame2 frame;

            bool success = false;
            if (frameId == -1 && isExecInConsole)
            {
                // If exec in console and no stack frame, evaluate off the top frame.
                success = m_frameHandles.TryGetFirst(out frame);
            }
            else
            {
                success = m_frameHandles.TryGet(frameId, out frame);
            }

            if (!success)
            {
                Dictionary<string, object> properties = new Dictionary<string, object>();
                properties.Add(DebuggerTelemetry.TelemetryStackFrameId, frameId);
                properties.Add(DebuggerTelemetry.TelemetryExecuteInConsole, isExecInConsole);
                DebuggerTelemetry.ReportError(DebuggerTelemetry.TelemetryEvaluateEventName, 1108, "Invalid frameId", properties);
                responder.SetError(new ProtocolException("Cannot evaluate expression on the specified stack frame."));
                return;
            }

            uint radix = Constants.EvaluationRadix;

            if (responder.Arguments.Format != null)
            {
                ValueFormat format = responder.Arguments.Format;

                if (format.Hex == true)
                {
                    radix = 16;
                }
            }

            if (m_settingsCallback != null)
            {
                // MIEngine generally gets the radix from IDebugSettingsCallback110 rather than using the radix passed
                m_settingsCallback.Radix = radix;
            }

            IDebugExpressionContext2 expressionContext;
            hr = frame.GetExpressionContext(out expressionContext);
            eb.CheckHR(hr);

            IDebugExpression2 expressionObject;
            string error;
            uint errorIndex;
            hr = expressionContext.ParseText(expression, enum_PARSEFLAGS.PARSE_EXPRESSION, Constants.ParseRadix, out expressionObject, out error, out errorIndex);
            if (!string.IsNullOrEmpty(error))
            {
                // TODO: Is this how errors should be returned?
                DebuggerTelemetry.ReportError(DebuggerTelemetry.TelemetryEvaluateEventName, 4001, "Error parsing expression");
                responder.SetError(new ProtocolException(error));
                return;
            }
            eb.CheckHR(hr);
            eb.CheckOutput(expressionObject);

            // NOTE: This is the same as what vssdebug normally passes for the watch window
            enum_EVALFLAGS flags = enum_EVALFLAGS.EVAL_RETURNVALUE |
                enum_EVALFLAGS.EVAL_NOEVENTS |
                (enum_EVALFLAGS)enum_EVALFLAGS110.EVAL110_FORCE_REAL_FUNCEVAL;

            if (context == EvaluateArguments.ContextValue.Hover) // No side effects for data tips
            {
                flags |= enum_EVALFLAGS.EVAL_NOSIDEEFFECTS;
            }

            IDebugProperty2 property;
            if (expressionObject is IDebugExpressionDAP expressionDapObject)
            {
                DAPEvalFlags dapEvalFlags = DAPEvalFlags.NONE;
                if (context == EvaluateArguments.ContextValue.Clipboard)
                {
                    dapEvalFlags |= DAPEvalFlags.CLIPBOARD_CONTEXT;
                }
                hr = expressionDapObject.EvaluateSync(flags, dapEvalFlags, Constants.EvaluationTimeout, null, out property);
            }
            else
            {
                hr = expressionObject.EvaluateSync(flags, Constants.EvaluationTimeout, null, out property);
            }

            eb.CheckHR(hr);
            eb.CheckOutput(property);

            DEBUG_PROPERTY_INFO[] propertyInfo = new DEBUG_PROPERTY_INFO[1];
            enum_DEBUGPROP_INFO_FLAGS propertyInfoFlags = GetDefaultPropertyInfoFlags();

            if (context == EvaluateArguments.ContextValue.Hover) // No side effects for data tips
            {
                propertyInfoFlags |= (enum_DEBUGPROP_INFO_FLAGS)enum_DEBUGPROP_INFO_FLAGS110.DEBUGPROP110_INFO_NOSIDEEFFECTS;
            }

            property.GetPropertyInfo(propertyInfoFlags, radix, Constants.EvaluationTimeout, null, 0, propertyInfo);

            // If the expression evaluation produces an error result and we are trying to get the expression for data tips
            // return a failure result so that VS code won't display the error message in data tips
            if (((propertyInfo[0].dwAttrib & enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR) == enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR) && context == EvaluateArguments.ContextValue.Hover)
            {
                responder.SetError(new ProtocolException("Evaluation error"));
                return;
            }

            string memoryReference = AD7Utils.GetMemoryReferenceFromIDebugProperty(property);

            Variable variable = m_variableManager.CreateVariable(ref propertyInfo[0], propertyInfoFlags, memoryReference);

            if (context != EvaluateArguments.ContextValue.Hover)
            {
                DebuggerTelemetry.ReportEvaluation(
                    ((propertyInfo[0].dwAttrib & enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR) == enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR),
                    DateTime.Now - evaluationStartTime,
                    isExecInConsole ? new Dictionary<string, object>() { { DebuggerTelemetry.TelemetryExecuteInConsole, true } } : null);
            }

            responder.SetResponse(new EvaluateResponse()
            {
                Result = variable.Value,
                Type = variable.Type,
                VariablesReference = variable.VariablesReference,
                MemoryReference = memoryReference
            });
        }

        protected override void HandleReadMemoryRequestAsync(IRequestResponder<ReadMemoryArguments, ReadMemoryResponse> responder)
        {
            int hr;
            ReadMemoryArguments rma = responder.Arguments;
            ErrorBuilder eb = new ErrorBuilder(() => AD7Resources.Error_Scenario_ReadMemory);
            try
            {
                if (string.IsNullOrEmpty(rma.MemoryReference))
                {
                    throw new ArgumentException("ReadMemoryArguments.MemoryReference is null or empty.");
                }

                hr = GetMemoryContext(rma.MemoryReference, rma.Offset, out IDebugMemoryContext2 memoryContext, out ulong address);
                eb.CheckHR(hr);

                byte[] data = new byte[rma.Count];
                uint unreadableBytes = 0;
                uint bytesRead = 0;

                if (rma.Count != 0)
                {
                    hr = m_program.GetMemoryBytes(out IDebugMemoryBytes2 debugMemoryBytes);
                    eb.CheckHR(hr);

                    hr = debugMemoryBytes.ReadAt(memoryContext, (uint)rma.Count, data, out bytesRead, ref unreadableBytes);
                    eb.CheckHR(hr);
                }

                responder.SetResponse(new ReadMemoryResponse()
                {
                    Address = string.Format(CultureInfo.InvariantCulture, "0x{0:X}", address),
                    Data = Convert.ToBase64String(data, 0, (int)bytesRead),
                    UnreadableBytes = (int?)unreadableBytes
                });
            }
            catch (Exception e)
            {
                responder.SetError(new ProtocolException(e.Message));
            }
        }

        protected override void HandleSetInstructionBreakpointsRequestAsync(IRequestResponder<SetInstructionBreakpointsArguments, SetInstructionBreakpointsResponse> responder)
        {
            if (responder.Arguments.Breakpoints == null)
            {
                responder.SetError(new ProtocolException("HandleSetInstructionBreakpointsRequest failed: Missing 'breakpoints'."));
                return;
            }

            ErrorBuilder eb = new ErrorBuilder(() => AD7Resources.Error_UnableToSetInstructionBreakpoint);

            SetInstructionBreakpointsResponse response = new SetInstructionBreakpointsResponse();

            List<InstructionBreakpoint> breakpoints = responder.Arguments.Breakpoints;
            Dictionary<ulong, IDebugPendingBreakpoint2> newBreakpoints = new Dictionary<ulong, IDebugPendingBreakpoint2>();
            try
            {
                HashSet<ulong> requestAddresses = responder.Arguments.Breakpoints.Select(x => ResolveInstructionReference(x.InstructionReference, x.Offset)).ToHashSet();

                foreach (KeyValuePair<ulong, IDebugPendingBreakpoint2> b in m_instructionBreakpoints)
                {
                    if (requestAddresses.Contains(b.Key))
                    {
                        newBreakpoints[b.Key] = b.Value;    // breakpoint still in new list
                    }
                    else
                    {
                        IDebugPendingBreakpoint2 pendingBp = b.Value;
                        if (pendingBp != null &&
                            pendingBp.GetBreakpointRequest(out IDebugBreakpointRequest2 request) == HRConstants.S_OK &&
                            request is AD7BreakPointRequest ad7Request)
                        {
                            HostMarshal.ReleaseCodeContextId(ad7Request.MemoryContextIntPtr);
                        }
                        else
                        {
                            Debug.Fail("Why can't we retrieve the MemoryContextIntPtr?");
                        }
                        b.Value.Delete();   // not in new list so delete it
                    }
                }

                foreach (var instructionBp in responder.Arguments.Breakpoints)
                {
                    eb.CheckHR(GetMemoryContext(instructionBp.InstructionReference, instructionBp.Offset, out IDebugMemoryContext2 memoryContext, out ulong address));

                    if (m_instructionBreakpoints.ContainsKey(address))
                    {
                        IDebugBreakpointRequest2 breakpointRequest;
                        if (m_instructionBreakpoints[address].GetBreakpointRequest(out breakpointRequest) == 0 &&
                                    breakpointRequest is AD7BreakPointRequest ad7BPRequest)
                        {
                            // Check to see if this breakpoint has a condition that has changed.
                            if (!StringComparer.Ordinal.Equals(ad7BPRequest.Condition, instructionBp.Condition))
                            {
                                // Condition has been modified. Delete breakpoint so it will be recreated with the updated condition.
                                var toRemove = m_instructionBreakpoints[address];
                                toRemove.Delete();
                                m_instructionBreakpoints.Remove(address);
                            }
                            else
                            {
                                if (ad7BPRequest.BindResult != null)
                                {
                                    response.Breakpoints.Add(ad7BPRequest.BindResult);
                                }
                                else
                                {
                                    response.Breakpoints.Add(new Breakpoint()
                                    {
                                        Id = (int)ad7BPRequest.Id,
                                        Verified = true,
                                        Line = 0
                                    });

                                }
                                continue;
                            }
                        }
                    }
                    else
                    {
                        IDebugPendingBreakpoint2 pendingBp;
                        AD7BreakPointRequest pBPRequest = new AD7BreakPointRequest(memoryContext);

                        eb.CheckHR(m_engine.CreatePendingBreakpoint(pBPRequest, out pendingBp));

                        if (pendingBp != null && pendingBp.Bind() == HRConstants.S_OK)
                        {
                            newBreakpoints[address] = pendingBp;
                            response.Breakpoints.Add(new Breakpoint()
                            {
                                Id = (int)pBPRequest.Id,
                                Verified = true,
                                Line = 0
                            }); // success
                        }
                        else
                        {
                            response.Breakpoints.Add(new Breakpoint()
                            {
                                Id = (int)pBPRequest.Id,
                                Verified = false,
                                Line = 0,
                                Message = string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_UnableToSetInstructionBreakpoint, address)
                            }); // couldn't create and/or bind
                        }
                    }
                }

                m_instructionBreakpoints = newBreakpoints;

                responder.SetResponse(response);
            }
            catch (Exception e)
            {
                responder.SetError(new ProtocolException(e.Message));
            }
        }

        #endregion

        #region IDebugPortNotify2

        int IDebugPortNotify2.AddProgramNode(IDebugProgramNode2 programNode)
        {
            if (m_process == null || m_engine == null)
            {
                throw new InvalidOperationException();
            }

            IDebugProgram2[] programs = { new AD7Program(m_process) };
            IDebugProgramNode2[] programNodes = { programNode };

            return m_engine.Attach(programs, programNodes, 1, this, enum_ATTACH_REASON.ATTACH_REASON_LAUNCH);
        }

        int IDebugPortNotify2.RemoveProgramNode(IDebugProgramNode2 pProgramNode)
        {
            return HRConstants.S_OK;
        }

#endregion

#region IDebugEventCallback2

        private readonly Dictionary<Guid, Action<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2>> m_syncEventHandler = new Dictionary<Guid, Action<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2>>();
        private readonly Dictionary<Guid, Func<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2, Task>> m_asyncEventHandler = new Dictionary<Guid, Func<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2, Task>>();

        private void RegisterAD7EventCallbacks()
        {
            // Sync Handlers
            RegisterSyncEventHandler(typeof(IDebugEngineCreateEvent2), HandleIDebugEngineCreateEvent2);
            RegisterSyncEventHandler(typeof(IDebugStepCompleteEvent2), HandleIDebugStepCompleteEvent2);
            RegisterSyncEventHandler(typeof(IDebugEntryPointEvent2), HandleIDebugEntryPointEvent2);
            RegisterSyncEventHandler(typeof(IDebugBreakpointEvent2), HandleIDebugBreakpointEvent2);
            RegisterSyncEventHandler(typeof(IDebugBreakEvent2), HandleIDebugBreakEvent2);
            RegisterSyncEventHandler(typeof(IDebugExceptionEvent2), HandleIDebugExceptionEvent2);
            RegisterSyncEventHandler(typeof(IDebugProgramDestroyEvent2), HandleIDebugProgramDestroyEvent2);
            RegisterSyncEventHandler(typeof(IDebugThreadCreateEvent2), HandleIDebugThreadCreateEvent2);
            RegisterSyncEventHandler(typeof(IDebugThreadDestroyEvent2), HandleIDebugThreadDestroyEvent2);
            RegisterSyncEventHandler(typeof(IDebugModuleLoadEvent2), HandleIDebugModuleLoadEvent2);
            RegisterSyncEventHandler(typeof(IDebugBreakpointBoundEvent2), HandleIDebugBreakpointBoundEvent2);
            RegisterSyncEventHandler(typeof(IDebugBreakpointErrorEvent2), HandleIDebugBreakpointErrorEvent2);
            RegisterSyncEventHandler(typeof(IDebugOutputStringEvent2), HandleIDebugOutputStringEvent2);
            RegisterSyncEventHandler(typeof(IDebugMessageEvent2), HandleIDebugMessageEvent2);
            RegisterSyncEventHandler(typeof(IDebugProcessInfoUpdatedEvent158), HandleIDebugProcessInfoUpdatedEvent158);

            // Async Handlers
            RegisterAsyncEventHandler(typeof(IDebugProgramCreateEvent2), HandleIDebugProgramCreateEvent2);
        }

        private void RegisterSyncEventHandler(Type type, Action<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2> handler)
        {
            m_syncEventHandler.Add(type.GetTypeInfo().GUID, handler);
        }

        private void RegisterAsyncEventHandler(Type type, Func<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2, Task> handler)
        {
            m_asyncEventHandler.Add(type.GetTypeInfo().GUID, handler);
        }

        public int Event(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib)
        {
            enum_EVENTATTRIBUTES attributes = unchecked((enum_EVENTATTRIBUTES)dwAttrib);

            Action<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2> syncEventHandler;
            if (m_syncEventHandler.TryGetValue(riidEvent, out syncEventHandler))
            {
                syncEventHandler(pEngine, pProcess, pProgram, pThread, pEvent);
            }

            Task task = null;
            Func<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2, Task> asyncEventHandler;
            if (m_asyncEventHandler.TryGetValue(riidEvent, out asyncEventHandler))
            {
                task = asyncEventHandler(pEngine, pProcess, pProgram, pThread, pEvent);
            }

            if (attributes.HasFlag(enum_EVENTATTRIBUTES.EVENT_SYNCHRONOUS))
            {
                if (task == null)
                {
                    pEngine.ContinueFromSynchronousEvent(pEvent);
                }
                else
                {
                    task.ContinueWith((j) => pEngine.ContinueFromSynchronousEvent(pEvent));
                }
            }

            return 0;
        }

        public void HandleIDebugEngineCreateEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            // Send configuration settings (e.g. Just My Code) to the engine.
            m_engine.SetMetric("JustMyCodeStepping", m_sessionConfig.JustMyCode ? "1" : "0");
            m_engine.SetMetric("EnableStepFiltering", m_sessionConfig.EnableStepFiltering ? "1" : "0");
        }

        public void HandleIDebugStepCompleteEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            FireStoppedEvent(pThread, StoppedEvent.ReasonValue.Step);
        }

        public void HandleIDebugEntryPointEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            if (m_sessionConfig.StopAtEntrypoint)
            {
                FireStoppedEvent(pThread, StoppedEvent.ReasonValue.Step);
            }
            else
            {
                BeforeContinue();
                m_program.Continue(pThread);
            }
        }

        public void HandleIDebugBreakpointEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            IDebugBreakpointEvent2 breakpointEvent = pEvent as IDebugBreakpointEvent2;
            StoppedEvent.ReasonValue reason = GetStoppedEventReason(breakpointEvent);
            IList <Tracepoint> tracepoints = GetTracepoints(breakpointEvent);
            if (tracepoints.Any())
            {
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    foreach (var tp in tracepoints)
                    {
                        int hr = tp.GetLogMessage(pThread, Constants.ParseRadix, m_processName, m_processId, out string logMessage);
                        if (hr != HRConstants.S_OK)
                        {
                            DebuggerTelemetry.ReportError(DebuggerTelemetry.TelemetryTracepointEventName, logMessage);
                            m_logger.WriteLine(LoggingCategory.DebuggerError, logMessage);
                        }
                        else
                        {
                            m_logger.WriteLine(LoggingCategory.DebuggerStatus, logMessage);
                        }
                    }

                    // Need to check to see if the previous continuation of the debuggee was a step. 
                    // If so, we need to send a stopping event to the UI to signal the step completed successfully. 
                    if (!m_isStepping)
                    {
                        ThreadPool.QueueUserWorkItem((obj) =>
                        {
                            BeforeContinue();
                            m_program.Continue(pThread);
                        });
                    }
                    else
                    {
                        FireStoppedEvent(pThread, reason);
                    }
                });
            }
            else
            {
                FireStoppedEvent(pThread, reason);
            }
        }

        public void HandleIDebugBreakEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            DebuggerTelemetry.ReportEvent(DebuggerTelemetry.TelemetryPauseEventName);
            FireStoppedEvent(pThread, StoppedEvent.ReasonValue.Pause);
        }

        public void HandleIDebugExceptionEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            Stopped(pThread);

            IDebugExceptionEvent2 exceptionEvent = (IDebugExceptionEvent2)pEvent;

            string exceptionDescription;
            exceptionEvent.GetExceptionDescription(out exceptionDescription);

            FireStoppedEvent(pThread, StoppedEvent.ReasonValue.Exception, exceptionDescription);
        }

        public Task HandleIDebugProgramCreateEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            Debug.Assert(m_program == null, "Multiple program create events?");
            if (m_program == null)
            {
                m_program = pProgram;
                Protocol.SendEvent(new InitializedEvent());
            }

            return m_configurationDoneTCS.Task;
        }

        public void HandleIDebugProgramDestroyEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            if (pProcess == null)
            {
                pProcess = m_process;
            }
            m_process = null;

            if (pProcess != null)
            {
                m_port.RemoveProcess(pProcess);
            }

            string exitMessage;
            uint ec = 0;
            if (m_isAttach)
            {
                exitMessage = string.Format(CultureInfo.CurrentCulture, AD7Resources.DebuggerDisconnectMessage, m_processName);
            }
            else
            {
                IDebugProgramDestroyEvent2 ee = (IDebugProgramDestroyEvent2)pEvent;
                ee.GetExitCode(out ec);
                exitMessage = string.Format(CultureInfo.CurrentCulture, AD7Resources.ProcessExitMessage, m_processName, (int)ec);
            }

            m_logger.WriteLine(LoggingCategory.ProcessExit, exitMessage);

            Protocol.SendEvent(new ExitedEvent((int)ec));
            Protocol.SendEvent(new TerminatedEvent());


            SendDebugCompletedTelemetry();
            m_disconnectedOrTerminated.Set();
        }

        public void HandleIDebugThreadCreateEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            int id = pThread.Id();
            lock (m_threads)
            {
                m_threads[id] = pThread;
            }
            Protocol.SendEvent(new ThreadEvent(ThreadEvent.ReasonValue.Started, id));
        }

        public void HandleIDebugThreadDestroyEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            int id = pThread.Id();

            lock (m_threads)
            {
                m_threads.Remove(id);
            }
            Protocol.SendEvent(new ThreadEvent(ThreadEvent.ReasonValue.Exited, id));
        }

        public void HandleIDebugModuleLoadEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            IDebugModule2 module;
            string moduleLoadMessage = null;
            int isLoad = 0;
            ((IDebugModuleLoadEvent2)pEvent).GetModule(out module, ref moduleLoadMessage, ref isLoad);

            m_logger.WriteLine(LoggingCategory.Module, moduleLoadMessage);

            int? moduleId = null;
            ModuleEvent.ReasonValue reason = ModuleEvent.ReasonValue.Unknown;

            if (isLoad != 0)
            {
                moduleId = (int?)RegisterDebugModule(module);
                reason = ModuleEvent.ReasonValue.New;
            } else {
                moduleId = ReleaseDebugModule(module);
                reason = ModuleEvent.ReasonValue.Removed;
            }

            if (moduleId != null)
            {
                var mod = ConvertToModule(module, (int)moduleId);
                if (mod != null)
                {
                    Protocol.SendEvent(new ModuleEvent(reason, mod));
                }
            }
        }

        public void HandleIDebugBreakpointBoundEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            var breakpointBoundEvent = (IDebugBreakpointBoundEvent2)pEvent;

            foreach (var boundBreakpoint in GetBoundBreakpoints(breakpointBoundEvent))
            {
                IDebugPendingBreakpoint2 pendingBreakpoint;
                if (boundBreakpoint.GetPendingBreakpoint(out pendingBreakpoint) == HRConstants.S_OK)
                {
                    IDebugBreakpointRequest2 breakpointRequest;
                    if (pendingBreakpoint.GetBreakpointRequest(out breakpointRequest) == HRConstants.S_OK)
                    {
                        AD7BreakPointRequest ad7BPRequest = (AD7BreakPointRequest)breakpointRequest;

                        // Once bound, attempt to get the bound line number from the breakpoint.
                        // If the AD7 calls fail, fallback to the original pending breakpoint line number.
                        int? lineNumber = GetBoundBreakpointLineNumber(boundBreakpoint);
                        if (lineNumber == null && ad7BPRequest.DocumentPosition != null)
                        {
                            lineNumber = m_pathConverter.ConvertDebuggerLineToClient(ad7BPRequest.DocumentPosition.Line);
                        }

                        Breakpoint bp = new Breakpoint()
                        {
                            Verified = true,
                            Id = (int)ad7BPRequest.Id,
                            Line = lineNumber.GetValueOrDefault(0)
                        };

                        ad7BPRequest.BindResult = bp;
                        Protocol.SendEvent(new BreakpointEvent(BreakpointEvent.ReasonValue.Changed, bp));
                    }
                }
            }
        }

        public void HandleIDebugBreakpointErrorEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            var breakpointErrorEvent = (IDebugBreakpointErrorEvent2)pEvent;

            IDebugErrorBreakpoint2 errorBreakpoint;
            if (breakpointErrorEvent.GetErrorBreakpoint(out errorBreakpoint) == 0)
            {
                IDebugPendingBreakpoint2 pendingBreakpoint;
                if (errorBreakpoint.GetPendingBreakpoint(out pendingBreakpoint) == 0)
                {
                    IDebugBreakpointRequest2 breakpointRequest;
                    if (pendingBreakpoint.GetBreakpointRequest(out breakpointRequest) == 0)
                    {
                        string errorMsg = string.Empty;

                        IDebugErrorBreakpointResolution2 errorBreakpointResolution;
                        if (errorBreakpoint.GetBreakpointResolution(out errorBreakpointResolution) == 0)
                        {
                            BP_ERROR_RESOLUTION_INFO[] bpInfo = new BP_ERROR_RESOLUTION_INFO[1];
                            if (errorBreakpointResolution.GetResolutionInfo(enum_BPERESI_FIELDS.BPERESI_MESSAGE, bpInfo) == 0)
                            {
                                errorMsg = bpInfo[0].bstrMessage;
                            }
                        }

                        AD7BreakPointRequest ad7BPRequest = (AD7BreakPointRequest)breakpointRequest;
                        Breakpoint bp = null;
                        if (ad7BPRequest.DocumentPosition != null)
                        {
                            if (string.IsNullOrWhiteSpace(ad7BPRequest.Condition))
                            {
                                bp = new Breakpoint()
                                {
                                    Verified = false,
                                    Id = (int)ad7BPRequest.Id,
                                    Line = m_pathConverter.ConvertDebuggerLineToClient(ad7BPRequest.DocumentPosition.Line),
                                    Message = errorMsg
                                };
                            }
                            else
                            {
                                bp = new Breakpoint()
                                {
                                    Verified = false,
                                    Id = (int)ad7BPRequest.Id,
                                    Line = m_pathConverter.ConvertDebuggerLineToClient(ad7BPRequest.DocumentPosition.Line),
                                    Message = string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_ConditionBreakpoint, ad7BPRequest.Condition, errorMsg)
                                };
                            }
                        }
                        else if (ad7BPRequest.FunctionPosition != null)
                        {
                            bp = new Breakpoint()
                            {
                                Verified = false,
                                Id = (int)ad7BPRequest.Id,
                                Line = 0,
                                Message = errorMsg
                            };

                            // TODO: currently VSCode will ignore the error message from "breakpoint" event, the workaround is to log the error to output window
                            string outputMsg = string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_FunctionBreakpoint, ad7BPRequest.FunctionPosition.Name, errorMsg);
                            m_logger.WriteLine(LoggingCategory.DebuggerError, outputMsg);
                        }
                        else // data bp
                        {
                            string outputMsg = string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_InvalidDataBreakpoint, errorMsg);
                            bp = new Breakpoint()
                            {
                                Verified = false,
                                Id = (int)ad7BPRequest.Id,
                                Line = 0,
                                Message = outputMsg
                            };
                            m_logger.WriteLine(LoggingCategory.DebuggerError, outputMsg);
                        }

                        ad7BPRequest.BindResult = bp;
                        Protocol.SendEvent(new BreakpointEvent(BreakpointEvent.ReasonValue.Changed, bp));
                    }
                }
            }
        }

        public void HandleIDebugOutputStringEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            // OutputStringEvent will include program output if the external console is disabled.

            var outputStringEvent = (IDebugOutputStringEvent2)pEvent;
            string text;
            if (outputStringEvent.GetString(out text) == 0)
            {
                m_logger.Write(LoggingCategory.StdOut, text);
            }
        }

        public void HandleIDebugMessageEvent2(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            var outputStringEvent = (IDebugMessageEvent2)pEvent;
            string text;
            enum_MESSAGETYPE[] messageType = new enum_MESSAGETYPE[1];
            uint type, helpId;
            string helpFileName;
            // TODO: Add VS Code support for message box based events, for now we will just output them
            // to the console since that is the best we can do.
            if (outputStringEvent.GetMessage(messageType, out text, out type, out helpFileName, out helpId) == 0)
            {
                const uint MB_ICONERROR = 0x00000010;
                const uint MB_ICONWARNING = 0x00000030;

                if ((messageType[0] & enum_MESSAGETYPE.MT_TYPE_MASK) == enum_MESSAGETYPE.MT_MESSAGEBOX)
                {
                    MessagePrefix prefix = MessagePrefix.None;
                    uint icon = type & 0xf0;
                    if (icon == MB_ICONERROR)
                    {
                        prefix = MessagePrefix.Error;
                    }
                    else if (icon == MB_ICONWARNING)
                    {
                        prefix = MessagePrefix.Warning;
                    }

                    // If we get an error message event during the launch, save it, as we may well want to return that as the launch failure message back to VS Code.
                    if (m_currentLaunchState != null && prefix != MessagePrefix.None)
                    {
                        lock (m_lock)
                        {
                            if (m_currentLaunchState != null && m_currentLaunchState.CurrentError == null)
                            {
                                m_currentLaunchState.CurrentError = new Tuple<MessagePrefix, string>(prefix, text);
                                return;
                            }
                        }
                    }

                    SendMessageEvent(prefix, text);
                }
                else if ((messageType[0] & enum_MESSAGETYPE.MT_REASON_MASK) == enum_MESSAGETYPE.MT_REASON_EXCEPTION)
                {
                    m_logger.Write(LoggingCategory.Exception, text);
                }
                else
                {
                    LoggingCategory category = LoggingCategory.DebuggerStatus;
                    // Check if the message looks like an error or warning. We will check with whatever
                    // our localized error/warning prefix might be and we will also accept the English
                    // version of the string.
                    if (text.StartsWith(AD7Resources.Prefix_Error, StringComparison.OrdinalIgnoreCase) ||
                        text.StartsWith(AD7Resources.Prefix_Warning, StringComparison.OrdinalIgnoreCase) ||
                        text.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) ||
                        text.StartsWith("Warning:", StringComparison.OrdinalIgnoreCase))
                    {
                        category = LoggingCategory.DebuggerError;
                    }

                    m_logger.Write(category, text);
                }
            }
        }

        public void HandleIDebugProcessInfoUpdatedEvent158(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent)
        {
            IDebugProcessInfoUpdatedEvent158 debugProcessInfoUpdated = pEvent as IDebugProcessInfoUpdatedEvent158;

            if (debugProcessInfoUpdated != null && 
                debugProcessInfoUpdated.GetUpdatedProcessInfo(out string name, out uint systemProcessId) == HRConstants.S_OK)
            {
                // Update Process Name and Id
                m_processName = name;
                m_processId = (int)systemProcessId;

                // Send ProcessEvent to Client
                ProcessEvent processEvent = new ProcessEvent();
                processEvent.Name = m_processName;
                processEvent.SystemProcessId = m_processId;

                if (m_isAttach)
                {
                    processEvent.StartMethod = ProcessEvent.StartMethodValue.Attach;
                }
                else
                {
                    processEvent.StartMethod = ProcessEvent.StartMethodValue.Launch;
                }

                if (m_engine is IDebugProgramDAP debugProgram)
                {
                    if (debugProgram.GetPointerSize(out int pointerSize) == HRConstants.S_OK)
                    {
                        processEvent.PointerSize = pointerSize;
                    }
                }
                Protocol.SendEvent(processEvent);
            }
        }

#endregion

        private class DebugSettingsCallback : IDebugSettingsCallback110
        {
            public DebugSettingsCallback()
            {
                Radix = Constants.EvaluationRadix;
            }

            internal uint Radix { get; set; }

            int IDebugSettingsCallback110.GetDisplayRadix(out uint pdwRadix)
            {
                pdwRadix = Radix;
                return HRConstants.S_OK;
            }

            int IDebugSettingsCallback110.GetUserDocumentPath(out string pbstrUserDocumentPath)
            {
                throw new NotImplementedException();
            }

            int IDebugSettingsCallback110.ShouldHideNonPublicMembers(out int pfHideNonPublicMembers)
            {
                throw new NotImplementedException();
            }

            int IDebugSettingsCallback110.ShouldSuppressImplicitToStringCalls(out int pfSuppressImplicitToStringCalls)
            {
                throw new NotImplementedException();
            }
        }
    }
}
