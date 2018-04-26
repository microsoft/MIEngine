// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DebugEngineHost;
using Microsoft.DebugEngineHost.VSCode;
using Microsoft.VisualStudio.Debugger.Interop;
using OpenDebug;
using OpenDebugAD7.AD7Impl;
using StackFrame = OpenDebug.StackFrame;
using Thread = OpenDebug.Thread;
using System.Text.RegularExpressions;

namespace OpenDebugAD7
{
    internal class AD7DebugSession : DebugSession, IDebugSession, IDebugEventCallback2, IDebugPortNotify2
    {
        private class VariableEvaluationData
        {
            internal IDebugProperty2 DebugProperty;
            internal enum_DEBUGPROP_INFO_FLAGS propertyInfoFlags;
        }

        private class ThreadFrameEnumInfo
        {
            internal IEnumDebugFrameInfo2 FrameEnum { get; private set; }
            internal uint TotalFrames { get; private set; }
            internal uint CurrentPosition { get; set; }

            internal ThreadFrameEnumInfo(IEnumDebugFrameInfo2 frameEnum, uint totalFrames)
            {
                FrameEnum = frameEnum;
                TotalFrames = totalFrames;
                CurrentPosition = 0;
            }
        }

        // POST_PREVIEW_TODO: no-func-eval support, radix, timeout
        private const uint EvaluationRadix = 10;
        private const uint EvaluationTimeout = 5000;
        private const int DisconnectTimeout = 2000;

        // This is a general purpose lock. Don't hold it across long operations.
        private readonly object _lock = new object();
        private readonly DebugProtocolCallbacks _debugProtocolCallbacks;
        private readonly EngineConfiguration _engineConfig;
        private readonly SessionConfiguration _sessionConfig = new SessionConfiguration();
        private readonly DebugEventLogger _logger;
        private readonly IDebugEngine2 _engine;
        private readonly IDebugEngineLaunch2 _engineLaunch;
        private IDebugProcess2 _process;
        private string _processName;
        private IDebugProgram2 _program;
        private readonly Dictionary<string, Dictionary<int, IDebugPendingBreakpoint2>> _breakpoints;
        private Dictionary<string, IDebugPendingBreakpoint2> _functionBreakpoints;
        private readonly Dictionary<int, IDebugThread2> _threads = new Dictionary<int, IDebugThread2>();
        private readonly Dictionary<int, ThreadFrameEnumInfo> _threadFrameEnumInfos = new Dictionary<int, ThreadFrameEnumInfo>();
        private readonly HandleCollection<Object> _variableHandles;
        private readonly HandleCollection<IDebugStackFrame2> _frameHandles;
        private readonly AD7Port _port;
        private readonly TaskCompletionSource<object> _configurationDoneTCS = new TaskCompletionSource<object>();
        private readonly Dictionary<Guid, Action<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2>> _syncEventHandler = new Dictionary<Guid, Action<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2>>();
        private readonly Dictionary<Guid, Func<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2, Task>> _asyncEventHandler = new Dictionary<Guid, Func<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2, Task>>();
        private CurrentLaunchState _currentLaunchState;
        private PathMapper _pathMapper;
        private readonly ManualResetEvent _disconnectedOrTerminated;
        private int _firstStoppingEvent;
        private uint _breakCounter = 0;
        private bool _isAttach;
        private bool _isCoreDump;
        private bool _isStopped = false;

        public AD7DebugSession(DebugProtocolCallbacks debugProtocolCallbacks, EngineConfiguration config) : base(false)
        {
            _debugProtocolCallbacks = debugProtocolCallbacks;
            _logger = new DebugEventLogger(_debugProtocolCallbacks.Send, _debugProtocolCallbacks.SendRaw);
            _engineConfig = config;
            _variableHandles = new HandleCollection<Object>();
            _frameHandles = new HandleCollection<IDebugStackFrame2>();
            _breakpoints = new Dictionary<string, Dictionary<int, IDebugPendingBreakpoint2>>();
            _functionBreakpoints = new Dictionary<string, IDebugPendingBreakpoint2>();

            _engine = (IDebugEngine2)_engineConfig.LoadEngine();

            TypeInfo engineType = _engine.GetType().GetTypeInfo();
            HostTelemetry.InitializeTelemetry(SendTelemetryEvent, engineType, _engineConfig.AdapterId);
            DebuggerTelemetry.InitializeTelemetry(_debugProtocolCallbacks.Send, engineType, typeof(Host).GetTypeInfo(), _engineConfig.AdapterId);

            HostOutputWindow.InitializeLaunchErrorCallback((error) => _logger.WriteLine(LoggingCategory.DebuggerError, error));

            _engineLaunch = (IDebugEngineLaunch2)_engine;
            _engine.SetRegistryRoot(_engineConfig.AdapterId);
            _port = new AD7Port(this);
            _disconnectedOrTerminated = new ManualResetEvent(false);
            _firstStoppingEvent = 0;

            RegisterSyncEventHandler(typeof(IDebugEngineCreateEvent2), (engine, process, program, thread, eventObject) =>
            {
                // Send configuration settings (e.g. Just My Code) to the engine.
                _engine.SetMetric("JustMyCodeStepping", _sessionConfig.JustMyCode ? "1" : "0");
                _engine.SetMetric("EnableStepFiltering", _sessionConfig.EnableStepFiltering ? "1" : "0");
            });
            RegisterSyncEventHandler(typeof(IDebugStepCompleteEvent2), (engine, process, program, thread, eventObject) =>
            {
                FireStoppedEvent(thread, "step");
            });
            RegisterSyncEventHandler(typeof(IDebugEntryPointEvent2), (engine, process, program, thread, eventObject) =>
            {
                if (_sessionConfig.StopAtEntrypoint)
                {
                    FireStoppedEvent(thread, "step");
                }
                else
                {
                    BeforeContinue();
                    _program.Continue(thread);
                }
            });
            RegisterSyncEventHandler(typeof(IDebugBreakpointEvent2), (engine, process, program, thread, eventObject) =>
            {
                FireStoppedEvent(thread, "breakpoint");
            });
            RegisterSyncEventHandler(typeof(IDebugBreakEvent2), (engine, process, program, thread, eventObject) =>
            {
                DebuggerTelemetry.ReportEvent(DebuggerTelemetry.TelemetryPauseEventName);
                FireStoppedEvent(thread, "pause");
            });
            RegisterSyncEventHandler(typeof(IDebugExceptionEvent2), (engine, process, program, thread, eventObject) =>
            {
                Stopped(thread);
                IDebugExceptionEvent2 ee = (IDebugExceptionEvent2)eventObject;
                string text;
                ee.GetExceptionDescription(out text);
                FireStoppedEvent(thread, "exception", text);
            });
            RegisterAsyncEventHandler(typeof(IDebugProgramCreateEvent2), (engine, process, program, thread, eventObject) =>
            {
                Debug.Assert(_program == null, "Multiple program create events?");
                if (_program == null)
                {
                    _program = program;
                    _debugProtocolCallbacks.SendLater(new InitializedEvent());
                }

                return _configurationDoneTCS.Task;
            });
            RegisterSyncEventHandler(typeof(IDebugProgramDestroyEvent2), (engine, process, program, thread, eventObject) =>
            {
                if (process == null)
                {
                    process = _process;
                }
                _process = null;

                if (process != null)
                {
                    _port.RemoveProcess(process);
                }

                string exitMessage;
                uint ec = 0;
                if (_isAttach)
                {
                    exitMessage = string.Format(CultureInfo.CurrentCulture, AD7Resources.DebuggerDisconnectMessage, _processName);
                }
                else
                {
                    IDebugProgramDestroyEvent2 ee = (IDebugProgramDestroyEvent2)eventObject;
                    ee.GetExitCode(out ec);
                    exitMessage = string.Format(CultureInfo.CurrentCulture, AD7Resources.ProcessExitMessage, _processName, (int)ec);
                }

                _logger.WriteLine(LoggingCategory.ProcessExit, exitMessage);

                _debugProtocolCallbacks.Send(new ExitedEvent((int)ec));
                _debugProtocolCallbacks.Send(new TerminatedEvent());


                this.SendDebugCompletedTelemetry();
                _disconnectedOrTerminated.Set();
            });
            RegisterSyncEventHandler(typeof(IDebugThreadCreateEvent2), (engine, process, program, thread, eventObject) =>
            {
                int id = thread.Id();
                _threads[id] = thread;
                _debugProtocolCallbacks.Send(new ThreadEvent("started", id));
            });
            RegisterSyncEventHandler(typeof(IDebugThreadDestroyEvent2), (engine, process, program, thread, eventObject) =>
            {
                int id = thread.Id();
                _threads.Remove(id);
                _debugProtocolCallbacks.Send(new ThreadEvent("exited", id));
            });
            RegisterSyncEventHandler(typeof(IDebugModuleLoadEvent2), (engine, process, program, thread, eventObject) =>
            {
                IDebugModule2 module;
                string moduleLoadMessage = null;
                int isLoad = 0;
                ((IDebugModuleLoadEvent2)eventObject).GetModule(out module, ref moduleLoadMessage, ref isLoad);

                _logger.WriteLine(LoggingCategory.Module, moduleLoadMessage);
            });
            RegisterSyncEventHandler(typeof(IDebugBreakpointBoundEvent2), (engine, process, program, thread, eventObject) =>
            {
                var breakpointBoundEvent = (IDebugBreakpointBoundEvent2)eventObject;

                foreach (var boundBreakpoint in GetBoundBreakpoints(breakpointBoundEvent))
                {
                    IDebugPendingBreakpoint2 pendingBreakpoint;
                    if (boundBreakpoint.GetPendingBreakpoint(out pendingBreakpoint) == Constants.S_OK)
                    {
                        IDebugBreakpointRequest2 breakpointRequest;
                        if (pendingBreakpoint.GetBreakpointRequest(out breakpointRequest) == Constants.S_OK)
                        {
                            AD7BreakPointRequest ad7BPRequest = (AD7BreakPointRequest)breakpointRequest;

                            // Once bound, attempt to get the bound line number from the breakpoint.
                            // If the AD7 calls fail, fallback to the original pending breakpoint line number.
                            int? lineNumber = this.GetBoundBreakpointLineNumber(boundBreakpoint);
                            if (lineNumber == null && ad7BPRequest.DocumentPosition != null)
                                lineNumber = ConvertDebuggerLineToClient(ad7BPRequest.DocumentPosition.Line);

                            Breakpoint bp = new Breakpoint(ad7BPRequest.Id, true, lineNumber ?? 0);

                            ad7BPRequest.BindResult = bp;
                            _debugProtocolCallbacks.SendLater(new BreakpointEvent(BreakpointEvent.Reason.changed, bp));
                        }
                    }
                }
            });
            RegisterSyncEventHandler(typeof(IDebugBreakpointErrorEvent2), (engine, process, program, thread, eventObject) =>
            {
                var breakpointErrorEvent = (IDebugBreakpointErrorEvent2)eventObject;

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
                                    bp = new Breakpoint(ad7BPRequest.Id, false, ConvertDebuggerLineToClient(ad7BPRequest.DocumentPosition.Line), errorMsg);
                                }
                                else
                                {
                                    bp = new Breakpoint(ad7BPRequest.Id, false, ConvertDebuggerLineToClient(ad7BPRequest.DocumentPosition.Line),
                                        string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_ConditionBreakpoint, ad7BPRequest.Condition, errorMsg));
                                }
                            }
                            else
                            {
                                bp = new Breakpoint(ad7BPRequest.Id, false, 0, errorMsg);

                                // TODO: currently VSCode will ignore the error message from "breakpoint" event, the workaround is to log the error to output window
                                string outputMsg = string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_FunctionBreakpoint, ad7BPRequest.FunctionPosition.Name, errorMsg);
                                _logger.WriteLine(LoggingCategory.DebuggerError, outputMsg);
                            }

                            ad7BPRequest.BindResult = bp;
                            _debugProtocolCallbacks.SendLater(new BreakpointEvent(BreakpointEvent.Reason.changed, bp));
                        }
                    }
                }
            });
            RegisterSyncEventHandler(typeof(IDebugOutputStringEvent2), (engine, process, program, thread, eventObject) =>
            {
                // OutputStringEvent will include program output if the external console is disabled.

                var outputStringEvent = (IDebugOutputStringEvent2)eventObject;
                string text;
                if (outputStringEvent.GetString(out text) == 0)
                {
                    _logger.Write(LoggingCategory.StdOut, text);
                }
            });
            RegisterSyncEventHandler(typeof(IDebugMessageEvent2), (engine, process, program, thread, eventObject) =>
            {
                var outputStringEvent = (IDebugMessageEvent2)eventObject;
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
                            prefix = MessagePrefix.Error;
                        else if (icon == MB_ICONWARNING)
                            prefix = MessagePrefix.Warning;

                        // If we get an error message event during the launch, save it, as we may well want to return that as the launch failure message back to VS Code.
                        if (_currentLaunchState != null && prefix != MessagePrefix.None)
                        {
                            lock (_lock)
                            {
                                if (_currentLaunchState != null && _currentLaunchState.CurrentError == null)
                                {
                                    _currentLaunchState.CurrentError = new Tuple<MessagePrefix, string>(prefix, text);
                                    return;
                                }
                            }
                        }

                        SendMessageEvent(prefix, text);
                    }
                    else if ((messageType[0] & enum_MESSAGETYPE.MT_REASON_MASK) == enum_MESSAGETYPE.MT_REASON_EXCEPTION)
                    {
                        _logger.Write(LoggingCategory.Exception, text);
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

                        _logger.Write(category, text);
                    }
                }
            });
        }

        /// <summary>
        /// Attempts to get the line number of the bound breakpoint.
        /// Does not report any errors encountered.
        /// </summary>
        /// <returns>The line number, or null if the line information was not found</returns>
        private int? GetBoundBreakpointLineNumber(IDebugBoundBreakpoint2 boundBreakpoint)
        {
            int hr;
            IDebugBreakpointResolution2 breakpointResolution;
            hr = boundBreakpoint.GetBreakpointResolution(out breakpointResolution);
            if (hr != Constants.S_OK)
                return null;

            BP_RESOLUTION_INFO[] resolutionInfo = new BP_RESOLUTION_INFO[1];
            hr = breakpointResolution.GetResolutionInfo(enum_BPRESI_FIELDS.BPRESI_BPRESLOCATION, resolutionInfo);
            if (hr != Constants.S_OK)
                return null;

            BP_RESOLUTION_LOCATION location = resolutionInfo[0].bpResLocation;
            enum_BP_TYPE bpType = (enum_BP_TYPE)location.bpType;
            if (bpType != enum_BP_TYPE.BPT_CODE || location.unionmember1 == IntPtr.Zero)
                return null;

            IDebugCodeContext2 codeContext;
            try
            {
                codeContext = HostMarshal.GetCodeContextForIntPtr(location.unionmember1);
                HostMarshal.ReleaseCodeContextId(location.unionmember1);
                location.unionmember1 = IntPtr.Zero;
            }
            catch (ArgumentException)
            {
                return null;
            }
            IDebugDocumentContext2 docContext;
            hr = codeContext.GetDocumentContext(out docContext);
            if (hr != Constants.S_OK)
                return null;

            // VSTS 237376: Shared library compiled without symbols will still bind a bp, but not have a docContext
            if (null == docContext)
                return null;

            TEXT_POSITION[] begin = new TEXT_POSITION[1];
            TEXT_POSITION[] end = new TEXT_POSITION[1];
            hr = docContext.GetStatementRange(begin, end);
            if (hr != Constants.S_OK)
                return null;

            return this.ConvertDebuggerLineToClient((int)begin[0].dwLine);
        }

        /// <summary>
        /// Wrap COM enum APIs to IEnumerable for getting bound breakpoints.
        /// Does not report any errors encountered.
        /// </summary>
        private static IEnumerable<IDebugBoundBreakpoint2> GetBoundBreakpoints(IDebugBreakpointBoundEvent2 breakpointBoundEvent)
        {
            int hr;
            IEnumDebugBoundBreakpoints2 boundBreakpointsEnum;
            hr = breakpointBoundEvent.EnumBoundBreakpoints(out boundBreakpointsEnum);
            if (hr != Constants.S_OK)
                return Enumerable.Empty<IDebugBoundBreakpoint2>();

            uint bufferSize;
            hr = boundBreakpointsEnum.GetCount(out bufferSize);
            if (hr != Constants.S_OK)
                return Enumerable.Empty<IDebugBoundBreakpoint2>();

            IDebugBoundBreakpoint2[] boundBreakpoints = new IDebugBoundBreakpoint2[bufferSize];
            uint fetched = 0;
            hr = boundBreakpointsEnum.Next(bufferSize, boundBreakpoints, ref fetched);
            if (hr != Constants.S_OK || fetched != bufferSize)
                return Enumerable.Empty<IDebugBoundBreakpoint2>();

            return boundBreakpoints;
        }

        private void RegisterSyncEventHandler(Type type, Action<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2> handler)
        {
            _syncEventHandler.Add(type.GetTypeInfo().GUID, handler);
        }
        private void RegisterAsyncEventHandler(Type type, Func<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2, Task> handler)
        {
            _asyncEventHandler.Add(type.GetTypeInfo().GUID, handler);
        }

        public int Event(IDebugEngine2 engine, IDebugProcess2 process, IDebugProgram2 program, IDebugThread2 thread, IDebugEvent2 eventObject, ref Guid riidEvent, uint dwAttrib)
        {
            enum_EVENTATTRIBUTES attributes = unchecked((enum_EVENTATTRIBUTES)dwAttrib);

            Action<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2> syncEventHandler;
            if (_syncEventHandler.TryGetValue(riidEvent, out syncEventHandler))
            {
                syncEventHandler(engine, process, program, thread, eventObject);
            }

            Task task = null;
            Func<IDebugEngine2, IDebugProcess2, IDebugProgram2, IDebugThread2, IDebugEvent2, Task> asyncEventHandler;
            if (_asyncEventHandler.TryGetValue(riidEvent, out asyncEventHandler))
            {
                task = asyncEventHandler(engine, process, program, thread, eventObject);
            }

            if (attributes.HasFlag(enum_EVENTATTRIBUTES.EVENT_SYNCHRONOUS))
            {
                if (task == null)
                {
                    engine.ContinueFromSynchronousEvent(eventObject);
                }
                else
                {
                    task.ContinueWith((j) => engine.ContinueFromSynchronousEvent(eventObject));
                }
            }

            return 0;
        }

        // Sets the debug settings that are common between launch and attach scenarios
        private void SetCommonDebugSettings(dynamic args, out int sourceFileMappings)
        {
            // Save the Just My Code setting. We will set it once the engine is created.
            _sessionConfig.JustMyCode = ((bool?)args.justMyCode) ?? _sessionConfig.JustMyCode;
            _sessionConfig.RequireExactSource = ((bool?)args.requireExactSource) ?? _sessionConfig.RequireExactSource;
            _sessionConfig.EnableStepFiltering = ((bool?)args.enableStepFiltering) ?? _sessionConfig.EnableStepFiltering;

            if (args.logging != null)
            {
                _logger.SetLoggingConfiguration(LoggingCategory.Exception, (bool?)args.logging.exceptions ?? true);
                _logger.SetLoggingConfiguration(LoggingCategory.Module, (bool?)args.logging.moduleLoad ?? true);
                _logger.SetLoggingConfiguration(LoggingCategory.StdOut, (bool?)args.logging.programOutput ?? true);
                _logger.SetLoggingConfiguration(LoggingCategory.StdErr, (bool?)args.logging.programOutput ?? true);

                bool? engineLogging = (bool?)args.logging.engineLogging;
                if (engineLogging.HasValue)
                {
                    _logger.SetLoggingConfiguration(LoggingCategory.EngineLogging, engineLogging.Value);
                    _debugProtocolCallbacks.SetEngineLogger(s => _logger.WriteLineRaw(LoggingCategory.EngineLogging, s));
                }

                bool? trace = (bool?)args.logging.trace;
                bool? traceResponse = (bool?)args.logging.traceResponse;
                if (trace.HasValue || traceResponse.HasValue)
                {
                    _logger.SetLoggingConfiguration(LoggingCategory.AdapterTrace, (trace ?? false) || (traceResponse ?? false));
                    _debugProtocolCallbacks.SetTraceLogger(s => _logger.WriteLineRaw(LoggingCategory.AdapterTrace, s));
                }

                if (traceResponse.HasValue)
                {
                    _logger.SetLoggingConfiguration(LoggingCategory.AdapterResponse, traceResponse.Value);
                    _debugProtocolCallbacks.SetResponseLogger(s => _logger.WriteLineRaw(LoggingCategory.AdapterResponse, s));
                }
            }

            sourceFileMappings = 0;
            Dictionary<string, string> sourceFileMap = null;
            {
                dynamic sourceFileMapProperty = args.sourceFileMap;
                if (sourceFileMapProperty != null)
                {
                    try
                    {
                        sourceFileMap = sourceFileMapProperty.ToObject<Dictionary<string, string>>();
                        sourceFileMappings = sourceFileMap.Count();
                    }
                    catch (Exception e)
                    {
                        SendMessageEvent(MessagePrefix.Error, "Configuration for 'sourceFileMap' has a format error and will be ignored.\nException: " + e.Message);
                        sourceFileMap = null;
                    }
                }
            }
            _pathMapper = new PathMapper(sourceFileMap);
        }

        public async override Task<DebugResult> Launch(dynamic args)
        {
            int hr;
            DateTime launchStartTime = DateTime.Now;

            string program = ((string)args.program)?.Trim();
            if (string.IsNullOrEmpty(program))
            {
                return CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryLaunchEventName, 1001, "launch: property 'program' is missing or empty");
            }

            // If program is still in the default state, yell
            if (program.EndsWith(">") && program.Contains('<'))
            {
                return CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryLaunchEventName, 1001, "launch: launch.json must be configured. Change 'program' to the path to the executable file that you would like to debug.");
            }

            // Pipe trasport can talk to remote machines so paths and files should not be checked in this case.
            bool skipFilesystemChecks = (args.pipeTransport != null || args.miDebuggerServerAddress != null);

            // For a remote scenario, we assume whatever input user has provided is correct.
            // The target remote could be any OS, so we don't try to change anything.
            if (!skipFilesystemChecks)
            {
                if (!ValidateProgramPath(ref program))
                {
                    return CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryLaunchEventName, 1002, String.Format("launch: program '{0}' does not exist", program));
                }
            }

            string workingDirectory = (string)args.cwd;
            if (string.IsNullOrEmpty(workingDirectory))
            {
                return CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryLaunchEventName, 1003, "launch: property 'cwd' is missing or empty");
            }

            if (!skipFilesystemChecks)
            {
                workingDirectory = ConvertLaunchPathForVsCode(workingDirectory);
                if (!Directory.Exists(workingDirectory))
                {
                    return CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryLaunchEventName, 1004, String.Format("launch: workingDirectory '{0}' does not exist", workingDirectory));
                }
            }

            if (!String.IsNullOrEmpty((string)args.processId))
            {
                return this.CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryLaunchEventName, 1001, "The parameter: processId should not be specified on Launch. Please use request type: attach");
            }

            int sourceFileMappings = 0;
            SetCommonDebugSettings(args, sourceFileMappings: out sourceFileMappings);

            bool success = false;
            try
            {
                lock (_lock)
                {
                    Debug.Assert(_currentLaunchState == null, "Concurrent launches??");
                    _currentLaunchState = new CurrentLaunchState();
                }
                var eb = new ErrorBuilder(() => AD7Resources.Error_Scenario_Launch);

                // Don't convert the workingDirectory string if we are a pipeTransport connection. We are assuming that the user has the correct directory separaters for their target OS
                string workingDirectoryString = args.pipeTransport != null ? workingDirectory : ConvertClientPathToDebugger(workingDirectory);

                bool debugServerUsed = false;
                bool isOpenOCD = false;
                bool stopAtEntrypoint;
                bool visualizerFileUsed;
                string launchOptions = MILaunchOptions.CreateLaunchOptions(
                    program: program,
                    workingDirectory: workingDirectoryString,
                    args: args,
                    stopAtEntry: out stopAtEntrypoint,
                    isCoreDump: out _isCoreDump,
                    debugServerUsed: out debugServerUsed,
                    isOpenOCD: out isOpenOCD,
                    visualizerFileUsed: out visualizerFileUsed);

                _sessionConfig.StopAtEntrypoint = stopAtEntrypoint;

                _processName = program;

                enum_LAUNCH_FLAGS flags = enum_LAUNCH_FLAGS.LAUNCH_DEBUG;
                if ((bool?)args.noDebug ?? false)
                {
                    flags = enum_LAUNCH_FLAGS.LAUNCH_NODEBUG;
                }

                // Then attach
                hr = _engineLaunch.LaunchSuspended(null,
                    _port,
                    program,
                    null,
                    null,
                    null,
                    launchOptions,
                    flags,
                    0,
                    0,
                    0,
                    this,
                    out _process);
                if (hr != Constants.S_OK)
                {
                    // If the engine raised a message via an error event, fire that instead
                    if (hr == Constants.E_ABORT)
                    {
                        string message;
                        lock (_lock)
                        {
                            message = _currentLaunchState?.CurrentError?.Item2;
                            _currentLaunchState = null;
                        }
                        if (message != null)
                        {
                            throw new AD7Exception(message);
                        }
                    }

                    eb.ThrowHR(hr);
                }

                hr = _engineLaunch.ResumeProcess(_process);
                if (hr < 0)
                {
                    // try to terminate the process if we can
                    try
                    {
                        _engineLaunch.TerminateProcess(_process);
                    }
                    catch
                    {
                        // Ignore failures since we are already dealing with an error
                    }

                    eb.ThrowHR(hr);
                }

                var properties = new Dictionary<string, object>(StringComparer.Ordinal);

                properties.Add(DebuggerTelemetry.TelemetryIsCoreDump, _isCoreDump);
                if (debugServerUsed)
                {
                    properties.Add(DebuggerTelemetry.TelemetryUsesDebugServer, isOpenOCD ? "openocd" : "other");
                }
                if (flags.HasFlag(enum_LAUNCH_FLAGS.LAUNCH_NODEBUG))
                {
                    properties.Add(DebuggerTelemetry.TelemetryIsNoDebug, true);
                }

                properties.Add(DebuggerTelemetry.TelemetryVisualizerFileUsed, visualizerFileUsed);
                properties.Add(DebuggerTelemetry.TelemetrySourceFileMappings, sourceFileMappings);

                DebuggerTelemetry.ReportTimedEvent(DebuggerTelemetry.TelemetryLaunchEventName, DateTime.Now - launchStartTime, properties);

                success = true;
            }
            finally
            {
                // Clear _currentLaunchState
                CurrentLaunchState currentLaunchState;
                lock (_lock)
                {
                    currentLaunchState = _currentLaunchState;
                    _currentLaunchState = null;
                }

                if (!success)
                {
                    _process = null;
                }

                // If we had an error event that we didn't wind up returning as an exception, raise it as an event
                Tuple<MessagePrefix, string> currentError = currentLaunchState?.CurrentError;
                if (currentError != null)
                {
                    SendMessageEvent(currentError.Item1, currentError.Item2);
                }
            }

            return new DebugResult();
        }

#pragma warning disable 1998
        public async override Task<DebugResult> Attach(dynamic args)
        {
            string processName = (string)args.processName;
            string processId = (string)args.processId;
            string miDebuggerServerAddress = (string)args.miDebuggerServerAddress;
            DateTime attachStartTime = DateTime.Now;
            bool isPipeTransport = (args.pipeTransport != null);
            bool isLocal = string.IsNullOrEmpty(miDebuggerServerAddress) && !isPipeTransport;
            bool visualizerFileUsed = false;
            int sourceFileMappings = 0;

            if (isLocal)
            {
                if (string.IsNullOrEmpty(processName) && string.IsNullOrEmpty(processId))
                {
                    return this.CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryAttachEventName, 1001, "attach: property 'processName' or 'processId' needs to be specified");
                }
                else if (!string.IsNullOrEmpty(processName) && !string.IsNullOrEmpty(processId))
                {
                    return this.CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryAttachEventName, 1002, "attach: properties 'processName' and 'processId' cannot be used together");
                }
            }
            else
            {
                string propertyCausingRemote = !string.IsNullOrEmpty(miDebuggerServerAddress) ? "miDebuggerServerAddress" : "pipeTransport";

                if (!string.IsNullOrEmpty(miDebuggerServerAddress) && (!string.IsNullOrEmpty(processName) || !string.IsNullOrEmpty(processId)))
                {
                    return this.CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryAttachEventName, 1002, "attach: properties 'processName' and 'processId' cannot be used with " + propertyCausingRemote);
                }
                else if (isPipeTransport && !string.IsNullOrEmpty(processName))
                {
                    return this.CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryAttachEventName, 1002, "attach: properties 'processName' and cannot be used with " + propertyCausingRemote);
                }
                else if (isPipeTransport && string.IsNullOrEmpty(processId) || string.IsNullOrEmpty((string)args.pipeTransport.debuggerPath))
                {
                    return this.CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryAttachEventName, 1001, "attach: properties 'processId' and 'debuggerPath' needs to be specified with " + propertyCausingRemote);
                }
            }

            int pid = 0;

            // Get pid and error checking on name / id. Handling common errors here because the error messages from clrdbg are a bit more cryptic.
            if (!string.IsNullOrEmpty(processName))
            {
                // GetProcessesByName requires process name without .exe extension
                string processFriendlyName = processName;

                if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    processFriendlyName = processName.Substring(0, processName.Length - 4);
                }

                Process[] processes = Process.GetProcessesByName(processFriendlyName);

                if (processes.Length == 0)
                {
                    return this.CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryAttachEventName, 1003, "attach: no process with the given name found");
                }
                else if (processes.Length > 1)
                {
                    return this.CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryAttachEventName, 1004, "attach: more than 1 process with the given name found. Use 'processId' to specify which process to attach to");
                }
                else
                {
                    pid = processes[0].Id;
                }
            }
            else
            {
                DebugResult result;
                if (isLocal)
                {
                    result = VerifyLocalProcessId(processId, DebuggerTelemetry.TelemetryAttachEventName, out pid);
                }
                else
                {
                    result = VerifyProcessId(processId, DebuggerTelemetry.TelemetryAttachEventName, out pid);
                }

                if (!result.Success)
                {
                    return result;
                }
            }

            SetCommonDebugSettings(args, sourceFileMappings: out sourceFileMappings);

            string executable = null;
            string launchOptions = null;
            bool success = false;
            try
            {
                lock (_lock)
                {
                    Debug.Assert(_currentLaunchState == null, "Concurrent launches??");
                    _currentLaunchState = new CurrentLaunchState();
                }
                var eb = new ErrorBuilder(() => AD7Resources.Error_Scenario_Attach);

                if (isPipeTransport)
                {
                    string program = ((string)args.program)?.Trim() ?? String.Empty;
                    if (string.IsNullOrEmpty((string)args.pipeTransport.debuggerPath))
                    {
                        return this.CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryAttachEventName, 1011, "debuggerPath is required for attachTransport.");
                    }
                    bool debugServerUsed = false;
                    bool isOpenOCD = false;
                    bool stopAtEntrypoint = false;

                    launchOptions = MILaunchOptions.CreateLaunchOptions(
                        program: program,
                        workingDirectory: String.Empty, // No cwd for attach
                        args: args,
                        stopAtEntry: out stopAtEntrypoint,
                        isCoreDump: out _isCoreDump,
                        debugServerUsed: out debugServerUsed,
                        isOpenOCD: out isOpenOCD,
                        visualizerFileUsed: out visualizerFileUsed);


                    if (string.IsNullOrEmpty(program))
                    {
                        return CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryAttachEventName, 1009, "attach: property 'program' is missing or empty");
                    }
                    else
                    {
                        executable = program;
                    }
                    _isAttach = true;
                }
                else
                {
                    string program = ((string)args.program)?.Trim();
                    if (string.IsNullOrEmpty(program))
                    {
                        return CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryAttachEventName, 1009, "attach: property 'program' is missing or empty");
                    }

                    if (!ValidateProgramPath(ref program))
                    {
                        return CreateErrorDebugResultAndLogTelemetry(DebuggerTelemetry.TelemetryAttachEventName, 1010, String.Format("attach: program path '{0}' does not exist", program));
                    }

                    bool debugServerUsed = false;
                    bool isOpenOCD = false;
                    bool stopAtEntrypoint = false;
                    launchOptions = MILaunchOptions.CreateLaunchOptions(
                        program: program,
                        workingDirectory: string.Empty,
                        args: args,
                        stopAtEntry: out stopAtEntrypoint,
                        isCoreDump: out _isCoreDump,
                        debugServerUsed: out debugServerUsed,
                        isOpenOCD: out isOpenOCD,
                        visualizerFileUsed: out visualizerFileUsed);
                    executable = program;
                    _isAttach = true;
                }

                _processName = processName ?? string.Empty;

                // attach
                int hr = _engineLaunch.LaunchSuspended(null, _port, executable, null, null, null, launchOptions, 0, 0, 0, 0, this, out _process);

                if (hr != Constants.S_OK)
                {
                    // If the engine raised a message via an error event, fire that instead
                    if (hr == Constants.E_ABORT)
                    {
                        string message;
                        lock (_lock)
                        {
                            message = _currentLaunchState?.CurrentError?.Item2;
                            _currentLaunchState = null;
                        }
                        if (message != null)
                        {
                            DebuggerTelemetry.ReportError(DebuggerTelemetry.TelemetryAttachEventName, message);
                            throw new AD7Exception(message);
                        }
                    }

                    eb.ThrowHR(hr);
                }

                hr = _engineLaunch.ResumeProcess(_process);
                if (hr < 0)
                {
                    // try to terminate the process if we can
                    try
                    {
                        _engineLaunch.TerminateProcess(_process);
                    }
                    catch
                    {
                        // Ignore failures since we are already dealing with an error
                    }

                    eb.ThrowHR(hr);
                }

                var properties = new Dictionary<string, object>(StringComparer.Ordinal);
                properties.Add(DebuggerTelemetry.TelemetryVisualizerFileUsed, visualizerFileUsed);
                properties.Add(DebuggerTelemetry.TelemetrySourceFileMappings, sourceFileMappings);

                DebuggerTelemetry.ReportTimedEvent(DebuggerTelemetry.TelemetryAttachEventName, DateTime.Now - attachStartTime, properties);
                success = true;
            }
            finally
            {
                // Clear _currentLaunchState
                CurrentLaunchState currentLaunchState;
                lock (_lock)
                {
                    currentLaunchState = _currentLaunchState;
                    _currentLaunchState = null;
                }

                if (!success)
                {
                    _process = null;
                }

                // If we had an error event that we didn't wind up returning as an exception, raise it as an event
                Tuple<MessagePrefix, string> currentError = currentLaunchState?.CurrentError;
                if (currentError != null)
                {
                    SendMessageEvent(currentError.Item1, currentError.Item2);
                }
            }

            return new DebugResult();
        }
#pragma warning restore 1998

        public override Task<DebugResult> Disconnect()
        {
            int hr;

            // If we are waiting to continue program create, stop waiting
            _configurationDoneTCS.TrySetResult(null);

            if (_process != null)
            {
                string errorReason = null;
                try
                {
                    hr = _isAttach ? _program.Detach() : _engineLaunch.TerminateProcess(_process);

                    if (hr < 0)
                    {
                        errorReason = ErrorBuilder.GetErrorDescription(hr);
                    }
                    else
                    {
                        // wait for termination event
                        if (!_disconnectedOrTerminated.WaitOne(DisconnectTimeout))
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
                    string message = string.Format(CultureInfo.CurrentCulture, AD7Resources.Warning_Scenario_TerminateProcess, _processName, errorReason);
                    _logger.WriteLine(LoggingCategory.DebuggerError, message);
                }
            }

            return Task.FromResult(new DebugResult());
        }

        public override Task<DebugResult> ConfigurationDone()
        {
            // If we are waiting to continue program create, mark that we have now finished initializing settings
            _configurationDoneTCS.TrySetResult(null);

            return Task.FromResult(new DebugResult());
        }

        public override Task<DebugResult> Continue(int threadId)
        {
            // Sometimes we can get a threadId of 0. Make sure we don't look it up in this case, otherwise we will crash.
            IDebugThread2 thread = null;
            if (threadId != 0 && !_threads.TryGetValue(threadId, out thread))
            {
                // We do not accept nonzero unknown threadIds.
                Debug.Fail("Unknown threadId passed to Continue!");
                return Task.FromResult(new DebugResult());
            }

            BeforeContinue();
            ErrorBuilder builder = new ErrorBuilder(() => AD7Resources.Error_Scenario_Continue);

            bool succeeded = false;
            try
            {
                builder.CheckHR(_program.Continue(thread));
                succeeded = true;
            }
            finally
            {
                if (!succeeded)
                {
                    _isStopped = true;
                }
            }

            return Task.FromResult(new DebugResult());
        }

        public override Task<DebugResult> Next(int threadId)
        {
            return StepInternal(threadId, enum_STEPKIND.STEP_OVER, enum_STEPUNIT.STEP_STATEMENT, AD7Resources.Error_Scenario_Step_Next);
        }

        public override Task<DebugResult> StepIn(int threadId)
        {
            return StepInternal(threadId, enum_STEPKIND.STEP_INTO, enum_STEPUNIT.STEP_STATEMENT, AD7Resources.Error_Scenario_Step_In);
        }

        public override Task<DebugResult> StepOut(int threadId)
        {
            return StepInternal(threadId, enum_STEPKIND.STEP_OUT, enum_STEPUNIT.STEP_STATEMENT, AD7Resources.Error_Scenario_Step_Out);
        }

        private Task<DebugResult> StepInternal(int threadId, enum_STEPKIND stepKind, enum_STEPUNIT stepUnit, string errorMessage)
        {
            Task<DebugResult> result = Task.FromResult(new DebugResult());

            // If we are already running ignore additional step requests
            if (!_isStopped)
            {
                return result;
            }

            IDebugThread2 thread = null;
            if (!_threads.TryGetValue(threadId, out thread))
            {
                throw new AD7Exception(errorMessage);
            }

            BeforeContinue();
            ErrorBuilder builder = new ErrorBuilder(() => errorMessage);
            try
            {
                builder.CheckHR(_program.Step(thread, stepKind, stepUnit));
            }
            catch (AD7Exception)
            {
                _isStopped = true;
                throw;
            }

            return result;
        }

        public override Task<DebugResult> Pause(int threadId)
        {
            // TODO: wait for break event
            _program.CauseBreak();
            return Task.FromResult(new DebugResult());
        }

        public override Task<DebugResult> SetFunctionBreakpoints(FunctionBreakpoint[] breakpoints)
        {
            var newBreakpoints = new Dictionary<string, IDebugPendingBreakpoint2>();
            foreach (var b in _functionBreakpoints)
            {
                if (Array.Find(breakpoints, (p) => p.name == b.Key) != null)
                {
                    newBreakpoints[b.Key] = b.Value;    // breakpoint still in new list
                }
                else
                {
                    b.Value.Delete();   // not in new list so delete it
                }
            }

            var resBreakpoints = new List<Breakpoint>();
            foreach (var b in breakpoints)
            {
                // bind the new function names
                if (!_functionBreakpoints.ContainsKey(b.name))
                {
                    IDebugPendingBreakpoint2 pendingBp;
                    AD7BreakPointRequest pBPRequest = new AD7BreakPointRequest(b.name);

                    int hr = _engine.CreatePendingBreakpoint(pBPRequest, out pendingBp);
                    if (hr == Constants.S_OK && pendingBp != null)
                    {
                        hr = pendingBp.Bind();
                    }
                    if (hr == Constants.S_OK)
                    {
                        newBreakpoints[b.name] = pendingBp;
                        resBreakpoints.Add(new Breakpoint(pBPRequest.Id, true, 0));    // success
                    }
                    else
                    {
                        resBreakpoints.Add(new Breakpoint(pBPRequest.Id, false, 0));   // couldn't create and/or bind
                    }
                }
                else
                {   // already created
                    IDebugBreakpointRequest2 breakpointRequest;
                    if (_functionBreakpoints[b.name].GetBreakpointRequest(out breakpointRequest) == 0)
                    {
                        var ad7BPRequest = (AD7BreakPointRequest)breakpointRequest;
                        if (ad7BPRequest.BindResult != null)
                        {
                            resBreakpoints.Add(ad7BPRequest.BindResult);
                        }
                        else
                        {
                            resBreakpoints.Add(new Breakpoint(ad7BPRequest.Id, true, 0));
                        }
                    }
                }
            }
            _functionBreakpoints = newBreakpoints;

            return Task.FromResult(new DebugResult(new SetBreakpointsResponseBody(resBreakpoints)));
        }

        public override Task<SetBreakpointsResponseBody> SetBreakpoints(Source source, SourceBreakpoint[] breakpoints, bool sourceModified = false)
        {
            if (source.path == null)
            {
                // we do not support other sources than 'path'
                return Task.FromResult(new SetBreakpointsResponseBody());
            }

            ErrorBuilder eb = new ErrorBuilder(() => AD7Resources.Error_UnableToSetBreakpoint);

            try
            {
                string path = ConvertClientPathToDebugger(source.path);

                if (Utilities.IsWindows() && path.Length > 2)
                {
                    // vscode may send drive letters with inconsistent casing which will mess up the key
                    // in the dictionary.  see https://github.com/Microsoft/vscode/issues/6268
                    // Normalize the drive letter casing. Note that drive letters
                    // are not localized so invariant is safe here.
                    string drive = path.Substring(0, 2);
                    if (char.IsLower(drive[0]) && drive.EndsWith(":", StringComparison.Ordinal))
                    {
                        path = String.Concat(drive.ToUpperInvariant(), path.Substring(2));
                    }
                }

                HashSet<int> lines = new HashSet<int>(breakpoints.Select((b) => b.line));

                Dictionary<int, IDebugPendingBreakpoint2> dict = null;
                if (_breakpoints.ContainsKey(path))
                {
                    dict = _breakpoints[path];
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
                    _breakpoints[path] = dict;
                }

                var resBreakpoints = new List<Breakpoint>();
                foreach (var bp in breakpoints)
                {
                    if (!dict.ContainsKey(bp.line))
                    {
                        IDebugPendingBreakpoint2 pendingBp;
                        AD7BreakPointRequest pBPRequest = new AD7BreakPointRequest(_sessionConfig, path, ConvertClientLineToDebugger(bp.line), bp.condition);

                        try
                        {
                            eb.CheckHR(_engine.CreatePendingBreakpoint(pBPRequest, out pendingBp));
                            eb.CheckHR(pendingBp.Bind());

                            dict[bp.line] = pendingBp;
                            resBreakpoints.Add(new Breakpoint(pBPRequest.Id, true, bp.line));
                        }
                        catch (Exception e)
                        {
                            e = Utilities.GetInnerMost(e);
                            if (Utilities.IsCorruptingException(e))
                            {
                                Utilities.ReportException(e);
                            }

                            resBreakpoints.Add(new Breakpoint(pBPRequest.Id, false, bp.line, eb.GetMessageForException(e)));
                        }
                    }
                    else
                    {   // already created
                        IDebugBreakpointRequest2 breakpointRequest;
                        if (dict[bp.line].GetBreakpointRequest(out breakpointRequest) == 0)
                        {
                            var ad7BPRequest = (AD7BreakPointRequest)breakpointRequest;
                            if (ad7BPRequest.BindResult != null)
                            {
                                // use the breakpoint created from IDebugBreakpointErrorEvent2 or IDebugBreakpointBoundEvent2
                                resBreakpoints.Add(ad7BPRequest.BindResult);
                            }
                            else
                            {
                                resBreakpoints.Add(new Breakpoint(ad7BPRequest.Id, true, bp.line));
                            }
                        }
                    }
                }

                return Task.FromResult(new SetBreakpointsResponseBody(resBreakpoints));
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
                List<Breakpoint> resBreakpoints = breakpoints.Select(bp => new Breakpoint(AD7BreakPointRequest.GetNextBreakpointId(), false, bp.line, message)).ToList();
                return Task.FromResult(new SetBreakpointsResponseBody(resBreakpoints));
            }
        }

        public override Task<DebugResult> SetExceptionBreakpoints(string[] filter)
        {
            if (_engineConfig.ExceptionSettings.Categories.Count > 0)
            {
                if (filter == null || filter.Length == 0)
                {
                    SetAllExceptions(enum_EXCEPTION_STATE.EXCEPTION_STOP_SECOND_CHANCE);
                }
                else if (filter.Contains(ExceptionBreakpointFilter.Filter_All))
                {
                    enum_EXCEPTION_STATE state = enum_EXCEPTION_STATE.EXCEPTION_STOP_SECOND_CHANCE | enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE | enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_FIRST_CHANCE;

                    if (filter.Contains(ExceptionBreakpointFilter.Filter_UserUnhandled))
                    {
                        state |= enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_UNCAUGHT;
                    }

                    SetAllExceptions(state);
                }
                else
                {
                    if (filter.Contains(ExceptionBreakpointFilter.Filter_UserUnhandled))
                    {
                        SetAllExceptions(enum_EXCEPTION_STATE.EXCEPTION_STOP_SECOND_CHANCE | enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_UNCAUGHT);
                    }
                    else
                    {
                        // TODO: once VS Code has UI to break on more than just 'uncaught' and 'all' we will need to enhance this with more features
                        Debug.Fail("Unexpected exception filter string");
                    }
                }
            }

            return Task.FromResult(new DebugResult(new ResponseBody()));
        }

        private void SetAllExceptions(enum_EXCEPTION_STATE state)
        {
            foreach (ExceptionSettings.CategoryConfiguration category in _engineConfig.ExceptionSettings.Categories)
            {
                var exceptionInfo = new EXCEPTION_INFO[1];
                exceptionInfo[0].dwState = state;
                exceptionInfo[0].guidType = category.Id;
                exceptionInfo[0].bstrExceptionName = category.Name;
                _engine.SetException(exceptionInfo);
            }
        }

        public override Task<DebugResult> StackTrace(int threadReference, int startFrame, int levels)
        {
            var result = new List<StackFrame>();

            // if we are not stopped or receive invalid input just return an empty stack trace
            if (!_isStopped || startFrame < 0 || levels < 0)
            {
                return Task.FromResult(new DebugResult(new StackTraceResponseBody(result, 0)));
            }

            ThreadFrameEnumInfo frameEnumInfo;
            if (!_threadFrameEnumInfos.TryGetValue(threadReference, out frameEnumInfo))
            {
                IDebugThread2 thread;
                if (_threads.TryGetValue(threadReference, out thread))
                {
                    var flags = enum_FRAMEINFO_FLAGS.FIF_FRAME |   // need a frame object
                        enum_FRAMEINFO_FLAGS.FIF_FUNCNAME |        // need a function name
                        enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_MODULE | // with the module specified
                        enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS |   // with argument names and types
                        enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_TYPES |
                        enum_FRAMEINFO_FLAGS.FIF_FUNCNAME_ARGS_NAMES |
                        enum_FRAMEINFO_FLAGS.FIF_FLAGS;

                    IEnumDebugFrameInfo2 frameEnum;
                    thread.EnumFrameInfo(flags, EvaluationRadix, out frameEnum);
                    uint totalFrames;
                    frameEnum.GetCount(out totalFrames);

                    frameEnumInfo = new ThreadFrameEnumInfo(frameEnum, totalFrames);
                    _threadFrameEnumInfos.Add(threadReference, frameEnumInfo);
                }
            }

            if (startFrame >= frameEnumInfo.TotalFrames)
            {
                return Task.FromResult(new DebugResult(new StackTraceResponseBody(result, 0)));
            }

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

                if (frame != null)
                {
                    frameReference = _frameHandles.Create(frame);
                    textPosition = TextPositionTuple.GetTextPositionOfFrame(this, frame) ?? TextPositionTuple.Nil;
                }

                result.Add(new StackFrame(frameReference, frameInfo.m_bstrFuncName, textPosition.Source, textPosition.Line, textPosition.Column));
            }

            return Task.FromResult(new DebugResult(new StackTraceResponseBody(result, (int)frameEnumInfo.TotalFrames)));
        }

        static private Guid s_guidFilterAllLocalsPlusArgs = new Guid("939729a8-4cb0-4647-9831-7ff465240d5f");

        public override Task<DebugResult> Scopes(int frameReference)
        {
            var scopes = new List<Scope>();

            // if we are not stopped return empty scopes
            if (!_isStopped)
            {
                return Task.FromResult(new DebugResult(new ErrorResponseBody(new Message(1105, AD7Resources.Error_TargetNotStopped))));
            }

            IDebugStackFrame2 frame;
            if (_frameHandles.TryGet(frameReference, out frame))
            {
                uint n;
                IEnumDebugPropertyInfo2 varEnum;
                if (frame.EnumProperties(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP, 10, ref s_guidFilterAllLocalsPlusArgs, 0, out n, out varEnum) == Constants.S_OK)
                {
                    if (n > 0)
                    {
                        scopes.Add(new Scope(AD7Resources.Locals_Scope_Name, _variableHandles.Create(frame)));
                    }
                }
            }
            return Task.FromResult(new DebugResult(new ScopesResponseBody(scopes)));
        }

        private Task<DebugResult> VariablesFromFrame(IDebugStackFrame2 frame)
        {
            List<Variable> locals = new List<Variable>();

            uint n;
            IEnumDebugPropertyInfo2 varEnum;
            if (frame.EnumProperties(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP, 10, ref s_guidFilterAllLocalsPlusArgs, 0, out n, out varEnum) == Constants.S_OK)
            {
                DEBUG_PROPERTY_INFO[] props = new DEBUG_PROPERTY_INFO[1];
                uint nProps;
                while (varEnum.Next(1, props, out nProps) == Constants.S_OK)
                {
                    locals.Add(CreateVariable(props[0].pProperty, GetDefaultPropertyInfoFlags()));
                }
            }
            return Task.FromResult(new DebugResult(new VariablesResponseBody(locals)));
        }

        public override Task<DebugResult> Variables(int reference)
        {
            List<Variable> children = new List<Variable>();

            // if we are not stopped return empty variables
            if (!_isStopped)
            {
                return Task.FromResult(new DebugResult(new ErrorResponseBody(new Message(1105, AD7Resources.Error_TargetNotStopped))));
            }

            Object container;
            if (_variableHandles.TryGet(reference, out container))
            {
                if (container is IDebugStackFrame2)
                {
                    return VariablesFromFrame(container as IDebugStackFrame2);
                }

                if (container is VariableEvaluationData)
                {
                    VariableEvaluationData variableEvaluationData = (VariableEvaluationData)container;
                    IDebugProperty2 property = variableEvaluationData.DebugProperty;

                    Guid empty = Guid.Empty;
                    IEnumDebugPropertyInfo2 childEnum;
                    if (property.EnumChildren(variableEvaluationData.propertyInfoFlags, EvaluationRadix, ref empty, enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ALL, null, EvaluationTimeout, out childEnum) == 0)
                    {
                        uint count;
                        childEnum.GetCount(out count);
                        if (count > 0)
                        {
                            DEBUG_PROPERTY_INFO[] childProperties = new DEBUG_PROPERTY_INFO[count];
                            childEnum.Next(count, childProperties, out count);

                            for (uint c = 0; c < count; c++)
                            {
                                children.Add(CreateVariable(ref childProperties[c], variableEvaluationData.propertyInfoFlags));
                            }
                        }
                    }
                }
                else
                {
                    Debug.Assert(false, "Unexpected type in _variableHandles collection");
                }
            }
            return Task.FromResult(new DebugResult(new VariablesResponseBody(children)));
        }

        public override Task<DebugResult> SetVariable(int reference, string name, string value)
        {
            // if we are not stopped don't try to set
            if (!_isStopped)
            {
                return Task.FromResult(new DebugResult(1105, AD7Resources.Error_TargetNotStopped));
            }

            object container;
            if (!_variableHandles.TryGet(reference, out container))
            {
                return Task.FromResult(new DebugResult(1106, AD7Resources.Error_VariableNotFound));
            }

            enum_DEBUGPROP_INFO_FLAGS flags = GetDefaultPropertyInfoFlags();
            IDebugProperty2 property = null;
            IEnumDebugPropertyInfo2 varEnum = null;
            int hr = Constants.E_FAIL;
            if (container is IDebugStackFrame2)
            {
                uint n;
                hr = ((IDebugStackFrame2)container).EnumProperties(
                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP,
                    EvaluationRadix,
                    ref s_guidFilterAllLocalsPlusArgs,
                    EvaluationTimeout,
                    out n,
                    out varEnum);
            }
            else if (container is VariableEvaluationData)
            {
                IDebugProperty2 debugProperty = ((VariableEvaluationData)container).DebugProperty;
                if (debugProperty == null)
                {
                    return Task.FromResult(new DebugResult(1106, AD7Resources.Error_VariableNotFound));
                }

                hr = debugProperty.EnumChildren(
                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP,
                    EvaluationRadix,
                    ref s_guidFilterAllLocalsPlusArgs,
                    enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_ALL,
                    name,
                    EvaluationTimeout,
                    out varEnum);
            }

            if (hr == Constants.S_OK && varEnum != null)
            {
                DEBUG_PROPERTY_INFO[] props = new DEBUG_PROPERTY_INFO[1];
                uint nProps;
                while (varEnum.Next(1, props, out nProps) == Constants.S_OK)
                {
                    DEBUG_PROPERTY_INFO[] propertyInfo = new DEBUG_PROPERTY_INFO[1];
                    props[0].pProperty.GetPropertyInfo(flags, EvaluationRadix, EvaluationTimeout, null, 0, propertyInfo);

                    if (propertyInfo[0].bstrName == name)
                    {
                        // Make sure we can assign to this variable.
                        if (propertyInfo[0].dwAttrib.HasFlag(enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_READONLY))
                        {
                            return Task.FromResult(new DebugResult(1107, string.Format(CultureInfo.CurrentCulture, AD7Resources.Error_VariableIsReadonly, name)));
                        }

                        property = propertyInfo[0].pProperty;
                        break;
                    }
                }
            }

            if (property == null)
            {
                return Task.FromResult(new DebugResult(1106, AD7Resources.Error_VariableNotFound));
            }

            string error = null;
            if (property is IDebugProperty3)
            {
                hr = ((IDebugProperty3)property).SetValueAsStringWithError(value, EvaluationRadix, EvaluationTimeout, out error);
            }
            else
            {
                hr = property.SetValueAsString(value, EvaluationRadix, EvaluationTimeout);
            }

            if (hr != Constants.S_OK)
            {
                return Task.FromResult(new DebugResult(1107, error ?? AD7Resources.Error_SetVariableFailed));
            }

            return Task.FromResult(new DebugResult(new SetVariablesResponseBody(CreateVariable(property, flags).value)));
        }

        public override Task<DebugResult> Threads()
        {
            var threadResultList = new List<Thread>();

            // Make a copy of the threads list
            Dictionary<int, IDebugThread2> threads;
            lock (_threads)
            {
                threads = new Dictionary<int, IDebugThread2>(_threads);
            }

            // iterate over the collection asking the engine for the name
            foreach (var pair in threads)
            {
                string name;
                pair.Value.GetName(out name);
                threadResultList.Add(new Thread(pair.Key, name));
            }

            return Task.FromResult(new DebugResult(new ThreadsResponseBody(threadResultList)));
        }

        public override Task<DebugResult> Evaluate(string context, int frameId, string expression)
        {
            // if we are not stopped, return evaluation failure
            if (!_isStopped)
            {
                return Task.FromResult(new DebugResult(new ErrorResponseBody(new Message(1105, AD7Resources.Error_TargetNotStopped))));
            }
            DateTime evaluationStartTime = DateTime.Now;

            bool isExecInConsole = false;
            // If this is an -exec command, log telemetry
            if (!String.IsNullOrEmpty(expression) && expression.StartsWith("-exec", StringComparison.Ordinal))
            {
                isExecInConsole = true;
            }

            int hr;
            ErrorBuilder eb = new ErrorBuilder(() => AD7Resources.Error_Scenario_Evaluate);
            IDebugStackFrame2 frame;

            bool success = false;
            if (frameId == -1 && isExecInConsole)
            {
                // If exec in console and no stack frame, evaluate off the top frame.
                success = _frameHandles.TryGetFirst(out frame);
            }
            else
            {
                success = _frameHandles.TryGet(frameId, out frame);
            }

            if (!success)
            {
                Dictionary<string, object> properties = new Dictionary<string, object>();
                properties.Add(DebuggerTelemetry.TelemetryStackFrameId, frameId);
                properties.Add(DebuggerTelemetry.TelemetryExecuteInConsole, isExecInConsole);
                DebuggerTelemetry.ReportError(DebuggerTelemetry.TelemetryEvaluateEventName, 1108, "Invalid frameId", properties);
                return Task.FromResult(new DebugResult(1108, "Cannot evaluate expression on the specified stack frame."));
            }

            IDebugExpressionContext2 expressionContext;
            hr = frame.GetExpressionContext(out expressionContext);
            eb.CheckHR(hr);

            const uint InputRadix = 10;
            IDebugExpression2 expressionObject;
            string error;
            uint errorIndex;
            hr = expressionContext.ParseText(expression, enum_PARSEFLAGS.PARSE_EXPRESSION, InputRadix, out expressionObject, out error, out errorIndex);
            if (!string.IsNullOrEmpty(error))
            {
                // TODO: Is this how errors should be returned?
                DebuggerTelemetry.ReportError(DebuggerTelemetry.TelemetryEvaluateEventName, 4001, "Error parsing expression");
                return Task.FromResult(new DebugResult(4001, error));
            }
            eb.CheckHR(hr);
            eb.CheckOutput(expressionObject);

            // NOTE: This is the same as what vssdebug normally passes for the watch window
            enum_EVALFLAGS flags = enum_EVALFLAGS.EVAL_RETURNVALUE |
                enum_EVALFLAGS.EVAL_NOEVENTS |
                (enum_EVALFLAGS)enum_EVALFLAGS110.EVAL110_FORCE_REAL_FUNCEVAL;

            if (context == "hover") // No side effects for data tips
            {
                flags |= enum_EVALFLAGS.EVAL_NOSIDEEFFECTS;
            }

            IDebugProperty2 property;
            hr = expressionObject.EvaluateSync(flags, EvaluationTimeout, null, out property);
            eb.CheckHR(hr);
            eb.CheckOutput(property);

            DEBUG_PROPERTY_INFO[] propertyInfo = new DEBUG_PROPERTY_INFO[1];
            enum_DEBUGPROP_INFO_FLAGS propertyInfoFlags = GetDefaultPropertyInfoFlags();

            if (context == "hover") // No side effects for data tips
            {
                propertyInfoFlags |= (enum_DEBUGPROP_INFO_FLAGS)enum_DEBUGPROP_INFO_FLAGS110.DEBUGPROP110_INFO_NOSIDEEFFECTS;
            }

            property.GetPropertyInfo(propertyInfoFlags, EvaluationRadix, EvaluationTimeout, null, 0, propertyInfo);

            // If the expression evaluation produces an error result and we are trying to get the expression for data tips
            // return a failure result so that VS code won't display the error message in data tips
            if (((propertyInfo[0].dwAttrib & enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR) == enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR) && context == "hover")
            {
                return Task.FromResult(new DebugResult(1101, "Evaluation error"));
            }

            Variable variable = CreateVariable(ref propertyInfo[0], propertyInfoFlags);

            if (context != "hover")
            {
                DebuggerTelemetry.ReportEvaluation(
                    ((propertyInfo[0].dwAttrib & enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR) == enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR),
                    DateTime.Now - evaluationStartTime,
                    isExecInConsole ? new Dictionary<string, object>() { { DebuggerTelemetry.TelemetryExecuteInConsole, true } } : null);
            }

            return Task.FromResult(new DebugResult(new EvaluateResponseBody(variable.value, variable.variablesReference, variable.type)));
        }

        private enum_DEBUGPROP_INFO_FLAGS GetDefaultPropertyInfoFlags()
        {
            enum_DEBUGPROP_INFO_FLAGS flags =
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP |
                enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME |
                (enum_DEBUGPROP_INFO_FLAGS)enum_DEBUGPROP_INFO_FLAGS110.DEBUGPROP110_INFO_FORCE_REAL_FUNCEVAL;

            if (_sessionConfig.JustMyCode)
            {
                flags |= (enum_DEBUGPROP_INFO_FLAGS)enum_DEBUGPROP_INFO_FLAGS110.DEBUGPROP110_INFO_NO_NONPUBLIC_MEMBERS;
            }

            return flags;
        }

        public override Capabilities Capabilities
        {
            get
            {
                return new Capabilities
                {
                    supportsConfigurationDoneRequest = true,
                    supportsEvaluateForHovers = true,
                    supportsSetVariable = true,
                    supportsFunctionBreakpoints = _engineConfig.FunctionBP,
                    supportsConditionalBreakpoints = _engineConfig.ConditionalBP,
                    exceptionBreakpointFilters = _engineConfig.ExceptionSettings.Breakpoints
                };
            }
        }

        //---- private ------------------------------------------

        private void Stopped(IDebugThread2 thread)
        {
            Debug.Assert(_variableHandles.IsEmpty, "Why do we have variable handles?");
            Debug.Assert(_frameHandles.IsEmpty, "Why do we have frame handles?");
            Debug.Assert(_threadFrameEnumInfos.Count == 0, "Why do we have thread frame enums?");
            _isStopped = true;
        }

        private void BeforeContinue()
        {
            if (!_isCoreDump)
            {
                _isStopped = false;
                _variableHandles.Reset();
                _frameHandles.Reset();
                _threadFrameEnumInfos.Clear();
            }
        }

        private void FireStoppedEvent(IDebugThread2 thread, string reason, string text = null)
        {
            Stopped(thread);

            // Switch to another thread as engines may not expect to be called back on their event thread
            ThreadPool.QueueUserWorkItem((o) =>
            {
                IEnumDebugFrameInfo2 frameInfoEnum;
                thread.EnumFrameInfo(enum_FRAMEINFO_FLAGS.FIF_FRAME | enum_FRAMEINFO_FLAGS.FIF_FLAGS, EvaluationRadix, out frameInfoEnum);

                TextPositionTuple textPosition = TextPositionTuple.Nil;
                if (frameInfoEnum != null)
                {
                    while (true)
                    {
                        FRAMEINFO[] frameInfoArray = new FRAMEINFO[1];
                        uint cFetched = 0;
                        frameInfoEnum.Next(1, frameInfoArray, ref cFetched);
                        if (cFetched != 1)
                            break;

                        if (AD7Utils.IsAnnotatedFrame(ref frameInfoArray[0]))
                            continue;

                        textPosition = TextPositionTuple.GetTextPositionOfFrame(this, frameInfoArray[0].m_pFrame) ?? TextPositionTuple.Nil;
                        break;
                    }
                }

                lock (_lock)
                {
                    _breakCounter++;
                }
                _debugProtocolCallbacks.SendLater(new StoppedEvent(reason, textPosition.Source, textPosition.Line, textPosition.Column, text, thread.Id()));
            });

            if (Interlocked.Exchange(ref _firstStoppingEvent, 1) == 0)
            {
                _logger.WriteLine(LoggingCategory.DebuggerStatus, AD7Resources.DebugConsoleStartMessage);
            }
        }

        private Variable CreateVariable(IDebugProperty2 property, enum_DEBUGPROP_INFO_FLAGS propertyInfoFlags)
        {
            DEBUG_PROPERTY_INFO[] propertyInfo = new DEBUG_PROPERTY_INFO[1];
            property.GetPropertyInfo(propertyInfoFlags, EvaluationRadix, EvaluationTimeout, null, 0, propertyInfo);

            return CreateVariable(ref propertyInfo[0], propertyInfoFlags);
        }

        private Variable CreateVariable(ref DEBUG_PROPERTY_INFO propertyInfo, enum_DEBUGPROP_INFO_FLAGS propertyInfoFlags)
        {
            string name = propertyInfo.bstrName;
            string val = propertyInfo.bstrValue;
            string type = null;

            // If we have a type string, and the value isn't just the type string in brackets, encode the shorthand for the type in the name value.
            if (!string.IsNullOrEmpty(propertyInfo.bstrType))
            {
                type = propertyInfo.bstrType;
            }

            int handle = GetVariableHandle(propertyInfo, propertyInfoFlags);
            return new Variable(name, val, type, handle, propertyInfo.bstrFullName);
        }

        /// <summary>
        /// Returns true if 'value' is '[type-name]' or '{type-name}'
        /// </summary>
        private static bool IsBracketedType(string value, string typeName)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            if (value.Length != typeName.Length + 2)
                return false;

            char firstChar = value[0];
            char lastChar = value[value.Length - 1];
            bool isBracketed =
                ((firstChar == '[' && lastChar == ']') ||
                (firstChar == '{' && lastChar == '}'));
            if (!isBracketed)
                return false;

            return string.CompareOrdinal(value, 1, typeName, 0, typeName.Length) == 0;
        }

        /// <summary>
        /// Attempts to get a short type name from the full name.
        /// </summary>
        /// <param name="fullTypeName">The full type name.</param>
        /// <param name="shortTypeName">[Out] The shortened type name.</param>
        /// <returns>True if the type name could be shortened, otherwise false.</returns>
        public static bool TryGetCSharpShortTypeName(string fullTypeName, out string shortTypeName)
        {
            // Remove the namespace and any generics.
            var match = Regex.Match(fullTypeName,
                @"^                                 # Beginning of type
                  (?:                               # GROUP: Namespaces
                      (?:                           #     GROUP: Namespace
                          (?![\d<])                 #         Not an invalid leading character!
                          (?:                       #         Namespace
                              \w+                   #             Valid identifier characters
                              (?:                   #             GROUP: Generics
                                  \<                #                 Match an opening bracket
                                  (?>               #                 Then match either:
                                      [^<>]+|       #                     Any non-generic character, or
                                      \<(?<Level>)| #                     Another opening bracket (more depth), or
                                      \>(?<-Level>) #                     A closing bracket (less depth)
                                  )*                #                 As many times as possible
                                  (?(Level)(?!))    #                 But not so many that depth is not zero
                                  \>                #                 Then match the last closing bracket
                              )?                    #             END GROUP: Generics (optional)
                          )\.                       #         Namespaces must end in a dot!
                      )*                            #     END GROUP: Namespace (repeat as many times as possible)
                  )?                                # END GROUP: Namespaces
                  (?<Type>(?!\d)\w+)                # GROUP/END GROUP: Type
                  (?:                               # GROUP: Generics
                      \<                            #     Match an opening bracket
                      (?>                           #     Then match either:
                          [^<>]+|                   #         Any non-generic character, or
                          \<(?<Level>)|             #         Another opening bracket (more depth), or
                          \>(?<-Level>)             #         A closing bracket (less depth)
                      )*                            #     As many times as possible
                      (?(Level)(?!))                #     But not so many that depth is not zero
                      \>                            #     Then match the last closing bracket
                  )?                                # END GROUP: Generics (optional)
                  (?<Array>\[[\[,\]]*\])?           # GROUP/END GROUP: Arrays
                  (?:\ \{.*\})?                     # GROUP/END GROUP: Instance type
                  $                                 # End of type",
                RegexOptions.IgnorePatternWhitespace);

            if (match.Success)
            {
                var typeName = match.Groups["Type"].Value;

                if (!string.IsNullOrEmpty(typeName))
                {
                    // Optional named groups will have the empty string as their value if they aren't
                    // matched, so we can just concatenate the array group.
                    shortTypeName = typeName + match.Groups["Array"].Value;
                    return true;
                }
            }

            // Unable to shorten the type name.
            shortTypeName = null;
            return false;
        }

        private int GetVariableHandle(DEBUG_PROPERTY_INFO propertyInfo, enum_DEBUGPROP_INFO_FLAGS propertyInfoFlags)
        {
            int handle = 0;
            if (propertyInfo.dwAttrib.HasFlag(enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE))
            {
                handle = _variableHandles.Create(new VariableEvaluationData { DebugProperty = propertyInfo.pProperty, propertyInfoFlags = propertyInfoFlags });
            }

            return handle;
        }

        /// <summary>
        /// Create a DebugResult with error information and log a telemetry event. Note: the message should not contain PII.
        /// </summary>
        /// <param name="telemetryEventName">Name of the method with the error</param>
        /// <param name="error">Error code</param>
        /// <param name="message">Error message</param>
        /// <returns>DebugResult with the error details</returns>
        private DebugResult CreateErrorDebugResultAndLogTelemetry(string telemetryEventName, int error, string message)
        {
            DebuggerTelemetry.ReportError(telemetryEventName, error);
            return new DebugResult(error, message);
        }

        public new string ConvertClientPathToDebugger(string path)
        {
            if (Path.DirectorySeparatorChar == '/')
            {
                path = path.Replace('\\', '/');
            }
            else if (!_clientPathsAreURI)
            {
                path = path.Replace('/', '\\');
            }
            return base.ConvertClientPathToDebugger(path);
        }

        public new string ConvertDebuggerPathToClient(string path)
        {
            path = _pathMapper.ResolveSymbolPath(path);

            if (Path.DirectorySeparatorChar == '/')
            {
                path = path.Replace('\\', '/');
            }
            else if (!_debuggerPathsAreURI)
            {
                path = path.Replace('/', '\\');
            }

            path = Utilities.NormalizeFileName(path, fixCasing: (Utilities.IsWindows() || Utilities.IsOSX()));

            return base.ConvertDebuggerPathToClient(path);
        }

        public new int ConvertDebuggerLineToClient(int line)
        {
            return base.ConvertDebuggerLineToClient(line);
        }

        int IDebugPortNotify2.AddProgramNode(IDebugProgramNode2 programNode)
        {
            if (_process == null || _engine == null)
            {
                throw new InvalidOperationException();
            }

            IDebugProgram2[] programs = { new AD7Program(_process) };
            IDebugProgramNode2[] programNodes = { programNode };

            return _engine.Attach(programs, programNodes, 1, this, enum_ATTACH_REASON.ATTACH_REASON_LAUNCH);
        }

        int IDebugPortNotify2.RemoveProgramNode(IDebugProgramNode2 pProgramNode)
        {
            return Constants.S_OK;
        }

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

            _logger.Write(LoggingCategory.Telemetry, eventName, propertiesDictionary);
        }

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

            _logger.WriteLine(category, prefixString + text);
        }

        private void SendDebugCompletedTelemetry()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            lock (_lock)
            {
                properties.Add(DebuggerTelemetry.TelemetryBreakCounter, _breakCounter);
            }
            DebuggerTelemetry.ReportEvent(DebuggerTelemetry.TelemetryDebugCompletedEventName, properties);
        }

        private string ConvertLaunchPathForVsCode(string clientPath)
        {
            string path = ConvertClientPathToDebugger(clientPath);

            if (Path.DirectorySeparatorChar == '/')
            {
                // TODO/HACK: VSCode tries to take all paths and make them workspace relative. This works around this until
                // we have a VSCode side fix.
                int slashTiltaSlashIndex = path.LastIndexOf("/~/", StringComparison.Ordinal);
                if (slashTiltaSlashIndex >= 0)
                {
                    string homePath = Environment.GetEnvironmentVariable("HOME");
                    if (string.IsNullOrEmpty(homePath))
                        throw new Exception("Environment variable 'HOME' is not defined.");

                    path = Path.Combine(homePath, path.Substring(slashTiltaSlashIndex + 3));
                }
            }

            return path;
        }

        /// <summary>
        /// Validates the program path has the correct separators and has the .exe file extension (Windows)
        /// </summary>
        /// <param name="program"></param>
        /// <returns>boolean on if the file Exists</returns>
        private bool ValidateProgramPath(ref string program)
        {
            // Make sure the slashes go in the correct direction
            char directorySeparatorChar = Path.DirectorySeparatorChar;
            char wrongSlashChar = directorySeparatorChar == '\\' ? '/' : '\\';

            if (program.Contains(wrongSlashChar))
            {
                program = program.Replace(wrongSlashChar, directorySeparatorChar);
            }

            program = ConvertLaunchPathForVsCode(program);
            if (!File.Exists(program))
            {
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

        private DebugResult VerifyLocalProcessId(string processId, string telemetryEventName, out int pid)
        {
            DebugResult result = VerifyProcessId(processId, telemetryEventName, out pid);

            if (!result.Success)
            {
                return result;
            }

            try
            {
                Process.GetProcessById(pid);
            }
            catch (ArgumentException)
            {
                return this.CreateErrorDebugResultAndLogTelemetry(telemetryEventName, 1006, "attach: no process with the given id found");
            }

            return new DebugResult();
        }

        private DebugResult VerifyProcessId(string processId, string telemetryEventName, out int pid)
        {
            if (!int.TryParse(processId, out pid))
            {
                return this.CreateErrorDebugResultAndLogTelemetry(telemetryEventName, 1005, "attach: unable to parse the process id");
            }

            if (pid == 0)
            {
                return CreateErrorDebugResultAndLogTelemetry(telemetryEventName, 1008, "attach: launch.json must be configured. Change 'processId' to the process you want to debug.");
            }

            return new DebugResult();
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
    }
}
