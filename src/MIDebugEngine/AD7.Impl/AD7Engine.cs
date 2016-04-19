// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MICore;
using System.Globalization;
using Microsoft.DebugEngineHost;

using Logger = MICore.Logger;

namespace Microsoft.MIDebugEngine
{
    // AD7Engine is the primary entrypoint object for the sample engine. 
    //
    // It implements:
    //
    // IDebugEngine2: This interface represents a debug engine (DE). It is used to manage various aspects of a debugging session, 
    // from creating breakpoints to setting and clearing exceptions.
    //
    // IDebugEngineLaunch2: Used by a debug engine (DE) to launch and terminate programs.
    //
    // IDebugProgram3: This interface represents a program that is running in a process. Since this engine only debugs one process at a time and each 
    // process only contains one program, it is implemented on the engine.
    //
    // IDebugEngineProgram2: This interface provides simultanious debugging of multiple threads in a debuggee.

    [System.Runtime.InteropServices.ComVisible(true)]
    [System.Runtime.InteropServices.Guid("0fc2f352-2fc1-4f80-8736-51cd1ab28f16")]
    sealed public class AD7Engine : IDebugEngine2, IDebugEngineLaunch2, IDebugProgram3, IDebugEngineProgram2, IDebugMemoryBytes2, IDebugEngine110
    {
        // used to send events to the debugger. Some examples of these events are thread create, exception thrown, module load.
        private EngineCallback _engineCallback;

        // The sample debug engine is split into two parts: a managed front-end and a mixed-mode back end. DebuggedProcess is the primary
        // object in the back-end. AD7Engine holds a reference to it.
        private DebuggedProcess _debuggedProcess;

        // This object facilitates calling from this thread into the worker thread of the engine. This is necessary because the Win32 debugging
        // api requires thread affinity to several operations.
        private WorkerThread _pollThread;

        // This object manages breakpoints in the sample engine.
        private BreakpointManager _breakpointManager;

        // A unique identifier for the program being debugged.
        private Guid _ad7ProgramId;

        private HostConfigurationStore _configStore;

        public Logger Logger { private set; get; }

        private IDebugSettingsCallback110 _settingsCallback;

        public AD7Engine()
        {
            Host.EnsureMainThreadInitialized();

            _breakpointManager = new BreakpointManager(this);
        }

        ~AD7Engine()
        {
            if (_pollThread != null)
            {
                _pollThread.Close();
            }
        }

        internal EngineCallback Callback
        {
            get { return _engineCallback; }
        }

        internal DebuggedProcess DebuggedProcess
        {
            get { return _debuggedProcess; }
        }

        internal uint CurrentRadix()
        {
            uint radix;
            if (_settingsCallback != null && _settingsCallback.GetDisplayRadix(out radix) == Constants.S_OK)
            {
                if (radix != _debuggedProcess.MICommandFactory.Radix)
                {
                    _debuggedProcess.WorkerThread.RunOperation(async () =>
                    {
                        await _debuggedProcess.MICommandFactory.SetRadix(radix);
                    });
                }
            }
            return _debuggedProcess.MICommandFactory.Radix;
        }

        internal bool ProgramCreateEventSent
        {
            get;
            private set;
        }

        public string GetAddressDescription(ulong ip)
        {
            return EngineUtils.GetAddressDescription(_debuggedProcess, ip);
        }

        public object GetMetric(string metric)
        {
            return _configStore.GetEngineMetric(metric);
        }

        #region IDebugEngine2 Members

        // Attach the debug engine to a program. 
        int IDebugEngine2.Attach(IDebugProgram2[] rgpPrograms, IDebugProgramNode2[] rgpProgramNodes, uint celtPrograms, IDebugEventCallback2 ad7Callback, enum_ATTACH_REASON dwReason)
        {
            Debug.Assert(_ad7ProgramId == Guid.Empty);

            if (celtPrograms != 1)
            {
                Debug.Fail("SampleEngine only expects to see one program in a process");
                throw new ArgumentException();
            }

            try
            {
                AD_PROCESS_ID processId = EngineUtils.GetProcessId(rgpPrograms[0]);

                EngineUtils.RequireOk(rgpPrograms[0].GetProgramId(out _ad7ProgramId));

                // Attach can either be called to attach to a new process, or to complete an attach
                // to a launched process
                if (_pollThread == null)
                {
                    // We are being asked to debug a process when we currently aren't debugging anything
                    _pollThread = new WorkerThread(Logger);

                    _engineCallback = new EngineCallback(this, ad7Callback);

                    // Complete the win32 attach on the poll thread
                    _pollThread.RunOperation(new Operation(delegate
                    {
                        throw new NotImplementedException();
                    }));

                    _pollThread.PostedOperationErrorEvent += _debuggedProcess.OnPostedOperationError;
                }
                else
                {
                    if (!EngineUtils.ProcIdEquals(processId, _debuggedProcess.Id))
                    {
                        Debug.Fail("Asked to attach to a process while we are debugging");
                        return Constants.E_FAIL;
                    }
                }

                AD7EngineCreateEvent.Send(this);
                AD7ProgramCreateEvent.Send(this);
                this.ProgramCreateEventSent = true;

                return Constants.S_OK;
            }
            catch (MIException e)
            {
                return e.HResult;
            }
            catch (Exception e) when (ExceptionHelper.BeforeCatch(e, Logger, reportOnlyCorrupting:true))
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Requests that all programs being debugged by this DE stop execution the next time one of their threads attempts to run.
        // This is normally called in response to the user clicking on the pause button in the debugger.
        // When the break is complete, an AsyncBreakComplete event will be sent back to the debugger.
        int IDebugEngine2.CauseBreak()
        {
            return ((IDebugProgram2)this).CauseBreak();
        }

        // Called by the SDM to indicate that a synchronous debug event, previously sent by the DE to the SDM,
        // was received and processed. The only event the sample engine sends in this fashion is Program Destroy.
        // It responds to that event by shutting down the engine.
        int IDebugEngine2.ContinueFromSynchronousEvent(IDebugEvent2 eventObject)
        {
            try
            {
                if (eventObject is AD7ProgramCreateEvent)
                {
                    Exception exception = null;

                    try
                    {
                        // At this point breakpoints and exception settings have been sent down, so we can resume the target
                        _pollThread.RunOperation(() =>
                        {
                            return _debuggedProcess.ResumeFromLaunch();
                        });
                    }
                    catch (Exception e)
                    {
                        exception = e;
                        // Return from the catch block so that we can let the exception unwind - the stack can get kind of big
                    }

                    if (exception != null)
                    {
                        // If something goes wrong, report the error and then stop debugging. The SDM will drop errors
                        // from ContinueFromSynchronousEvent, so we want to deal with them ourself.
                        SendStartDebuggingError(exception);
                        _debuggedProcess.Terminate();
                    }

                    return Constants.S_OK;
                }
                else if (eventObject is AD7ProgramDestroyEvent)
                {
                    Dispose();
                }
                else
                {
                    Debug.Fail("Unknown syncronious event");
                }
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }

            return Constants.S_OK;
        }

        private void Dispose()
        {
            WorkerThread pollThread = _pollThread;
            DebuggedProcess debuggedProcess = _debuggedProcess;

            _engineCallback = null;
            _debuggedProcess = null;
            _pollThread = null;
            _ad7ProgramId = Guid.Empty;

            debuggedProcess?.Close();
            pollThread?.Close();
        }

        // Creates a pending breakpoint in the engine. A pending breakpoint is contains all the information needed to bind a breakpoint to 
        // a location in the debuggee.
        int IDebugEngine2.CreatePendingBreakpoint(IDebugBreakpointRequest2 pBPRequest, out IDebugPendingBreakpoint2 ppPendingBP)
        {
            Debug.Assert(_breakpointManager != null);
            ppPendingBP = null;

            try
            {
                _breakpointManager.CreatePendingBreakpoint(pBPRequest, out ppPendingBP);
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }

            return Constants.S_OK;
        }

        // Informs a DE that the program specified has been atypically terminated and that the DE should 
        // clean up all references to the program and send a program destroy event.
        int IDebugEngine2.DestroyProgram(IDebugProgram2 pProgram)
        {
            // Tell the SDM that the engine knows that the program is exiting, and that the
            // engine will send a program destroy. We do this because the Win32 debug api will always
            // tell us that the process exited, and otherwise we have a race condition.

            return (AD7_HRESULT.E_PROGRAM_DESTROY_PENDING);
        }

        // Gets the GUID of the DE.
        int IDebugEngine2.GetEngineId(out Guid guidEngine)
        {
            guidEngine = new Guid(EngineConstants.EngineId);
            return Constants.S_OK;
        }

        // Removes the list of exceptions the IDE has set for a particular run-time architecture or language.
        int IDebugEngine2.RemoveAllSetExceptions(ref Guid guidType)
        {
            _debuggedProcess?.ExceptionManager.RemoveAllSetExceptions(guidType);
            return Constants.S_OK;
        }

        // Removes the specified exception so it is no longer handled by the debug engine.
        // The sample engine does not support exceptions in the debuggee so this method is not actually implemented.       
        int IDebugEngine2.RemoveSetException(EXCEPTION_INFO[] pException)
        {
            _debuggedProcess?.ExceptionManager.RemoveSetException(ref pException[0]);
            return Constants.S_OK;
        }

        // Specifies how the DE should handle a given exception.
        // The sample engine does not support exceptions in the debuggee so this method is not actually implemented.
        int IDebugEngine2.SetException(EXCEPTION_INFO[] pException)
        {
            _debuggedProcess?.ExceptionManager.SetException(ref pException[0]);
            return Constants.S_OK;
        }

        // Sets the locale of the DE.
        // This method is called by the session debug manager (SDM) to propagate the locale settings of the IDE so that
        // strings returned by the DE are properly localized. The sample engine is not localized so this is not implemented.
        int IDebugEngine2.SetLocale(ushort wLangID)
        {
            return Constants.S_OK;
        }

        // A metric is a registry value used to change a debug engine's behavior or to advertise supported functionality. 
        // This method can forward the call to the appropriate form of the Debugging SDK Helpers function, SetMetric.
        int IDebugEngine2.SetMetric(string pszMetric, object varValue)
        {
            if (string.CompareOrdinal(pszMetric, "JustMyCodeStepping") == 0)
            {
                string strJustMyCode = varValue.ToString();
                bool optJustMyCode;
                if (string.CompareOrdinal(strJustMyCode, "0") == 0)
                {
                    optJustMyCode = false;
                }
                else if (string.CompareOrdinal(strJustMyCode, "1") == 0)
                {
                    optJustMyCode = true;
                }
                else
                {
                    return Constants.E_FAIL;
                }

                _pollThread.RunOperation(new Operation(() => { _debuggedProcess.MICommandFactory.SetJustMyCode(optJustMyCode); }));
                return Constants.S_OK;
            }

            return Constants.E_NOTIMPL;
        }

        // Sets the registry root currently in use by the DE. Different installations of Visual Studio can change where their registry information is stored
        // This allows the debugger to tell the engine where that location is.
        int IDebugEngine2.SetRegistryRoot(string registryRoot)
        {
            _configStore = new HostConfigurationStore(registryRoot, EngineConstants.EngineId);
            Logger = Logger.EnsureInitialized(_configStore);
            return Constants.S_OK;
        }

        #endregion

        #region IDebugEngineLaunch2 Members

        // Determines if a process can be terminated.
        int IDebugEngineLaunch2.CanTerminateProcess(IDebugProcess2 process)
        {
            Debug.Assert(_pollThread != null);
            Debug.Assert(_engineCallback != null);
            Debug.Assert(_debuggedProcess != null);

            AD_PROCESS_ID processId = EngineUtils.GetProcessId(process);

            if (EngineUtils.ProcIdEquals(processId, _debuggedProcess.Id))
            {
                return Constants.S_OK;
            }
            else
            {
                return Constants.S_FALSE;
            }
        }

        // Launches a process by means of the debug engine.
        // Normally, Visual Studio launches a program using the IDebugPortEx2::LaunchSuspended method and then attaches the debugger 
        // to the suspended program. However, there are circumstances in which the debug engine may need to launch a program 
        // (for example, if the debug engine is part of an interpreter and the program being debugged is an interpreted language), 
        // in which case Visual Studio uses the IDebugEngineLaunch2::LaunchSuspended method
        // The IDebugEngineLaunch2::ResumeProcess method is called to start the process after the process has been successfully launched in a suspended state.
        int IDebugEngineLaunch2.LaunchSuspended(string pszServer, IDebugPort2 port, string exe, string args, string dir, string env, string options, enum_LAUNCH_FLAGS launchFlags, uint hStdInput, uint hStdOutput, uint hStdError, IDebugEventCallback2 ad7Callback, out IDebugProcess2 process)
        {
            Debug.Assert(_pollThread == null);
            Debug.Assert(_engineCallback == null);
            Debug.Assert(_debuggedProcess == null);
            Debug.Assert(_ad7ProgramId == Guid.Empty);

            process = null;

            _engineCallback = new EngineCallback(this, ad7Callback);

            Exception exception;

            try
            {
                // Note: LaunchOptions.GetInstance can be an expensive operation and may push a wait message loop
                LaunchOptions launchOptions = LaunchOptions.GetInstance(_configStore, exe, args, dir, options, _engineCallback, TargetEngine.Native, Logger);

                // We are being asked to debug a process when we currently aren't debugging anything
                _pollThread = new WorkerThread(Logger);
                var cancellationTokenSource = new CancellationTokenSource();

                using (cancellationTokenSource)
                {
                    _pollThread.RunOperation(ResourceStrings.InitializingDebugger, cancellationTokenSource, (HostWaitLoop waitLoop) =>
                    {
                        try
                        {
                            _debuggedProcess = new DebuggedProcess(true, launchOptions, _engineCallback, _pollThread, _breakpointManager, this, _configStore);
                        }
                        finally
                        {
                            // If there is an exception from the DebuggeedProcess constructor, it is our responsibility to dispose the DeviceAppLauncher,
                            // otherwise the DebuggedProcess object takes ownership.
                            if (_debuggedProcess == null && launchOptions.DeviceAppLauncher != null)
                            {
                                launchOptions.DeviceAppLauncher.Dispose();
                            }
                        }

                        _pollThread.PostedOperationErrorEvent += _debuggedProcess.OnPostedOperationError;

                        return _debuggedProcess.Initialize(waitLoop, cancellationTokenSource.Token);
                    });
                }

                EngineUtils.RequireOk(port.GetProcess(_debuggedProcess.Id, out process));

                return Constants.S_OK;
            }
            catch (Exception e) when (ExceptionHelper.BeforeCatch(e, Logger, reportOnlyCorrupting: true))
            {
                exception = e;
                // Return from the catch block so that we can let the exception unwind - the stack can get kind of big
            }

            // If we just return the exception as an HRESULT, we will loose our message, so we instead send up an error event, and then
            // return E_ABORT.
            Logger.Flush();
            SendStartDebuggingError(exception);

            Dispose();

            return Constants.E_ABORT;
        }

        private void SendStartDebuggingError(Exception exception)
        {
            if (exception is OperationCanceledException)
            {
                return; // don't show a message in this case
            }

            string description = EngineUtils.GetExceptionDescription(exception);
            string message = string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_UnableToStartDebugging, description);

            var initializationException = exception as MIDebuggerInitializeFailedException;
            if (initializationException != null)
            {
                string outputMessage = string.Join("\r\n", initializationException.OutputLines) + "\r\n";

                // NOTE: We can't write to the output window by sending an AD7 event because this may be called before the session create event
                HostOutputWindow.WriteLaunchError(outputMessage);
            }

            _engineCallback.OnErrorImmediate(message);
        }

        // Resume a process launched by IDebugEngineLaunch2.LaunchSuspended
        int IDebugEngineLaunch2.ResumeProcess(IDebugProcess2 process)
        {
            Debug.Assert(_pollThread != null);
            Debug.Assert(_engineCallback != null);
            Debug.Assert(_debuggedProcess != null);
            Debug.Assert(_ad7ProgramId == Guid.Empty);

            try
            {
                AD_PROCESS_ID processId = EngineUtils.GetProcessId(process);

                if (!EngineUtils.ProcIdEquals(processId, _debuggedProcess.Id))
                {
                    return Constants.S_FALSE;
                }

                // Send a program node to the SDM. This will cause the SDM to turn around and call IDebugEngine2.Attach
                // which will complete the hookup with AD7
                IDebugPort2 port;
                EngineUtils.RequireOk(process.GetPort(out port));

                IDebugDefaultPort2 defaultPort = (IDebugDefaultPort2)port;

                IDebugPortNotify2 portNotify;
                EngineUtils.RequireOk(defaultPort.GetPortNotify(out portNotify));

                EngineUtils.RequireOk(portNotify.AddProgramNode(new AD7ProgramNode(_debuggedProcess.Id)));

                if (_ad7ProgramId == Guid.Empty)
                {
                    Debug.Fail("Unexpected problem -- IDebugEngine2.Attach wasn't called");
                    return Constants.E_FAIL;
                }

                // NOTE: We wait for the program create event to be continued before we really resume the process

                return Constants.S_OK;
            }
            catch (MIException e)
            {
                return e.HResult;
            }
            catch (Exception e) when (ExceptionHelper.BeforeCatch(e, Logger, reportOnlyCorrupting: true))
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // This function is used to terminate a process that the SampleEngine launched
        // The debugger will call IDebugEngineLaunch2::CanTerminateProcess before calling this method.
        int IDebugEngineLaunch2.TerminateProcess(IDebugProcess2 process)
        {
            Debug.Assert(_pollThread != null);
            Debug.Assert(_engineCallback != null);
            Debug.Assert(_debuggedProcess != null);

            AD_PROCESS_ID processId = EngineUtils.GetProcessId(process);
            if (!EngineUtils.ProcIdEquals(processId, _debuggedProcess.Id))
            {
                return Constants.S_FALSE;
            }

            try
            {
                _pollThread.RunOperation(() => _debuggedProcess.CmdTerminate());

                if (_debuggedProcess.MICommandFactory.Mode != MIMode.Clrdbg)
                {
                    _debuggedProcess.Terminate();
                }
                else
                {
                    // Clrdbg issues a proper exit event on CmdTerminate call, don't call _debuggedProcess.Terminate() which 
                    // simply sends a fake exit event that overrides the exit code of the real one
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore failures caused by the connection already being dead.
            }

            return Constants.S_OK;
        }

        #endregion

        #region IDebugProgram2 Members

        // Determines if a debug engine (DE) can detach from the program.
        public int CanDetach()
        {
            // The sample engine always supports detach
            return Constants.S_OK;
        }

        // The debugger calls CauseBreak when the user clicks on the pause button in VS. The debugger should respond by entering
        // breakmode. 
        public int CauseBreak()
        {
            _pollThread.RunOperation(() => _debuggedProcess.CmdBreak());

            return Constants.S_OK;
        }

        // Continue is called from the SDM when it wants execution to continue in the debugee
        // but have stepping state remain. An example is when a tracepoint is executed, 
        // and the debugger does not want to actually enter break mode.
        public int Continue(IDebugThread2 pThread)
        {
            // VS Code currently isn't providing a thread Id in certain cases. Work around this by handling null values.
            AD7Thread thread = pThread as AD7Thread;

            try
            {
                if (_pollThread.IsPollThread())
                {
                    _debuggedProcess.Continue(thread?.GetDebuggedThread());
                }
                else
                {
                    _pollThread.RunOperation(() => _debuggedProcess.Continue(thread?.GetDebuggedThread()));
                }
            }
            catch (InvalidCoreDumpOperationException)
            {
                return AD7_HRESULT.E_CRASHDUMP_UNSUPPORTED;
            }

            return Constants.S_OK;
        }

        // Detach is called when debugging is stopped and the process was attached to (as opposed to launched)
        // or when one of the Detach commands are executed in the UI.
        public int Detach()
        {
            _breakpointManager.ClearBoundBreakpoints();

            _pollThread.RunOperation(() => _debuggedProcess.CmdDetach());

            return Constants.S_OK;
        }

        // Enumerates the code contexts for a given position in a source file.
        public int EnumCodeContexts(IDebugDocumentPosition2 docPosition, out IEnumDebugCodeContexts2 ppEnum)
        {
            string documentName;
            EngineUtils.CheckOk(docPosition.GetFileName(out documentName));

            // Get the location in the document
            TEXT_POSITION[] startPosition = new TEXT_POSITION[1];
            TEXT_POSITION[] endPosition = new TEXT_POSITION[1];
            EngineUtils.CheckOk(docPosition.GetRange(startPosition, endPosition));
            List<IDebugCodeContext2> codeContexts = new List<IDebugCodeContext2>();

            List<ulong> addresses = null;
            uint line = startPosition[0].dwLine + 1;
            _debuggedProcess.WorkerThread.RunOperation(async () =>
            {
                addresses = await DebuggedProcess.StartAddressesForLine(documentName, line);
            });

            if (addresses != null && addresses.Count > 0)
            {
                foreach (var a in addresses)
                {
                    var codeCxt = new AD7MemoryAddress(this, a, null);
                    TEXT_POSITION pos;
                    pos.dwLine = line;
                    pos.dwColumn = 0;
                    MITextPosition textPosition = new MITextPosition(documentName, pos, pos);
                    codeCxt.SetDocumentContext(new AD7DocumentContext(textPosition, codeCxt));
                    codeContexts.Add(codeCxt);
                }
                if (codeContexts.Count > 0)
                {
                    ppEnum = new AD7CodeContextEnum(codeContexts.ToArray());
                    return Constants.S_OK;
                }
            }
            ppEnum = null;
            return Constants.E_FAIL;
        }

        // EnumCodePaths is used for the step-into specific feature -- right click on the current statment and decide which
        // function to step into. This is not something that the SampleEngine supports.
        public int EnumCodePaths(string hint, IDebugCodeContext2 start, IDebugStackFrame2 frame, int fSource, out IEnumCodePaths2 pathEnum, out IDebugCodeContext2 safetyContext)
        {
            pathEnum = null;
            safetyContext = null;
            return Constants.E_NOTIMPL;
        }

        // EnumModules is called by the debugger when it needs to enumerate the modules in the program.
        public int EnumModules(out IEnumDebugModules2 ppEnum)
        {
            DebuggedModule[] modules = _debuggedProcess.GetModules();

            AD7Module[] moduleObjects = new AD7Module[modules.Length];
            for (int i = 0; i < modules.Length; i++)
            {
                moduleObjects[i] = new AD7Module(modules[i], _debuggedProcess);
            }

            ppEnum = new Microsoft.MIDebugEngine.AD7ModuleEnum(moduleObjects);

            return Constants.S_OK;
        }

        // EnumThreads is called by the debugger when it needs to enumerate the threads in the program.
        public int EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
            DebuggedThread[] threads = null;
            DebuggedProcess.WorkerThread.RunOperation(async () => threads = await DebuggedProcess.ThreadCache.GetThreads());

            AD7Thread[] threadObjects = new AD7Thread[threads.Length];
            for (int i = 0; i < threads.Length; i++)
            {
                Debug.Assert(threads[i].Client != null);
                threadObjects[i] = (AD7Thread)threads[i].Client;
            }

            ppEnum = new Microsoft.MIDebugEngine.AD7ThreadEnum(threadObjects);

            return Constants.S_OK;
        }

        // The properties returned by this method are specific to the program. If the program needs to return more than one property, 
        // then the IDebugProperty2 object returned by this method is a container of additional properties and calling the 
        // IDebugProperty2::EnumChildren method returns a list of all properties.
        // A program may expose any number and type of additional properties that can be described through the IDebugProperty2 interface. 
        // An IDE might display the additional program properties through a generic property browser user interface.
        // The sample engine does not support this
        public int GetDebugProperty(out IDebugProperty2 ppProperty)
        {
            throw new NotImplementedException();
        }

        // The debugger calls this when it needs to obtain the IDebugDisassemblyStream2 for a particular code-context.
        // The sample engine does not support dissassembly so it returns E_NOTIMPL
        // In order for this to be called, the Disassembly capability must be set in the registry for this Engine
        public int GetDisassemblyStream(enum_DISASSEMBLY_STREAM_SCOPE dwScope, IDebugCodeContext2 codeContext, out IDebugDisassemblyStream2 disassemblyStream)
        {
            disassemblyStream = new AD7DisassemblyStream(this, dwScope, codeContext);
            return Constants.S_OK;
        }

        // This method gets the Edit and Continue (ENC) update for this program. A custom debug engine always returns E_NOTIMPL
        public int GetENCUpdate(out object update)
        {
            // The sample engine does not participate in managed edit & continue.
            update = null;
            return Constants.S_OK;
        }

        // Gets the name and identifier of the debug engine (DE) running this program.
        public int GetEngineInfo(out string engineName, out Guid engineGuid)
        {
            engineName = ResourceStrings.EngineName;
            engineGuid = new Guid(EngineConstants.EngineId);
            return Constants.S_OK;
        }

        // The memory bytes as represented by the IDebugMemoryBytes2 object is for the program's image in memory and not any memory 
        // that was allocated when the program was executed.
        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            ppMemoryBytes = this;
            return Constants.S_OK;
        }

        // Gets the name of the program.
        // The name returned by this method is always a friendly, user-displayable name that describes the program.
        public int GetName(out string programName)
        {
            // The Sample engine uses default transport and doesn't need to customize the name of the program,
            // so return NULL.
            programName = null;
            return Constants.S_OK;
        }

        // Gets a GUID for this program. A debug engine (DE) must return the program identifier originally passed to the IDebugProgramNodeAttach2::OnAttach
        // or IDebugEngine2::Attach methods. This allows identification of the program across debugger components.
        public int GetProgramId(out Guid guidProgramId)
        {
            Debug.Assert(_ad7ProgramId != Guid.Empty);

            guidProgramId = _ad7ProgramId;
            return Constants.S_OK;
        }

        public int Step(IDebugThread2 pThread, enum_STEPKIND kind, enum_STEPUNIT unit)
        {
            AD7Thread thread = (AD7Thread)pThread;

            try
            {
                _debuggedProcess.WorkerThread.RunOperation(() => _debuggedProcess.Step(thread.GetDebuggedThread().Id, kind, unit));
            }
            catch (InvalidCoreDumpOperationException)
            {
                return AD7_HRESULT.E_CRASHDUMP_UNSUPPORTED;
            }

            return Constants.S_OK;
        }

        // Terminates the program.
        public int Terminate()
        {
            // Because the sample engine is a native debugger, it implements IDebugEngineLaunch2, and will terminate
            // the process in IDebugEngineLaunch2.TerminateProcess
            return Constants.S_OK;
        }

        // Writes a dump to a file.
        public int WriteDump(enum_DUMPTYPE DUMPTYPE, string pszDumpUrl)
        {
            // The sample debugger does not support creating or reading mini-dumps.
            return Constants.E_NOTIMPL;
        }

        #endregion

        #region IDebugProgram3 Members

        // ExecuteOnThread is called when the SDM wants execution to continue and have 
        // stepping state cleared.
        public int ExecuteOnThread(IDebugThread2 pThread)
        {
            AD7Thread thread = (AD7Thread)pThread;

            try
            {
                _pollThread.RunOperation(() => _debuggedProcess.Execute(thread.GetDebuggedThread()));
            }
            catch (InvalidCoreDumpOperationException)
            {
                return AD7_HRESULT.E_CRASHDUMP_UNSUPPORTED;
            }

            return Constants.S_OK;
        }

        #endregion

        #region IDebugEngineProgram2 Members

        // Stops all threads running in this program.
        // This method is called when this program is being debugged in a multi-program environment. When a stopping event from some other program 
        // is received, this method is called on this program. The implementation of this method should be asynchronous; 
        // that is, not all threads should be required to be stopped before this method returns. The implementation of this method may be 
        // as simple as calling the IDebugProgram2::CauseBreak method on this program.
        //
        // The sample engine only supports debugging native applications and therefore only has one program per-process
        public int Stop()
        {
            throw new NotImplementedException();
        }

        // WatchForExpressionEvaluationOnThread is used to cooperate between two different engines debugging 
        // the same process. The sample engine doesn't cooperate with other engines, so it has nothing
        // to do here.
        public int WatchForExpressionEvaluationOnThread(IDebugProgram2 pOriginatingProgram, uint dwTid, uint dwEvalFlags, IDebugEventCallback2 pExprCallback, int fWatch)
        {
            return Constants.S_OK;
        }

        // WatchForThreadStep is used to cooperate between two different engines debugging the same process.
        // The sample engine doesn't cooperate with other engines, so it has nothing to do here.
        public int WatchForThreadStep(IDebugProgram2 pOriginatingProgram, uint dwTid, int fWatch, uint dwFrame)
        {
            return Constants.S_OK;
        }

        #endregion

        #region IDebugMemoryBytes2 Members

        public int GetSize(out ulong pqwSize)
        {
            throw new NotImplementedException();
        }

        public int ReadAt(IDebugMemoryContext2 pStartContext, uint dwCount, byte[] rgbMemory, out uint pdwRead, ref uint pdwUnreadable)
        {
            pdwUnreadable = 0;
            AD7MemoryAddress addr = (AD7MemoryAddress)pStartContext;
            uint bytesRead = 0;
            int hr = Constants.S_OK;
            DebuggedProcess.WorkerThread.RunOperation(async () =>
            {
                bytesRead = await DebuggedProcess.ReadProcessMemory(addr.Address, dwCount, rgbMemory);
            });

            if (bytesRead == uint.MaxValue)
            {
                bytesRead = 0;
            }

            if (bytesRead < dwCount) // copied from Concord
            {
                // assume 4096 sized pages: ARM has 4K or 64K pages
                uint pageSize = 4096;
                ulong readEnd = addr.Address + bytesRead;
                ulong nextPageStart = (readEnd + pageSize - 1) / pageSize * pageSize;
                if (nextPageStart == readEnd)
                {
                    nextPageStart = readEnd + pageSize;
                }
                // if we have crossed a page boundry - Unreadable = bytes till end of page
                uint maxUnreadable = dwCount - bytesRead;
                if (addr.Address + dwCount > nextPageStart)
                {
                    pdwUnreadable = (uint)Math.Min(maxUnreadable, nextPageStart - readEnd);
                }
                else
                {
                    pdwUnreadable = (uint)Math.Min(maxUnreadable, pageSize);
                }
            }
            pdwRead = bytesRead;
            return hr;
        }

        public int WriteAt(IDebugMemoryContext2 pStartContext, uint dwCount, byte[] rgbMemory)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDebugEngine110
        public int SetMainThreadSettingsCallback110(IDebugSettingsCallback110 pCallback)
        {
            _settingsCallback = pCallback;
            return Constants.S_OK;
        }
        #endregion

        #region Deprecated interface methods
        // These methods are not called by the Visual Studio debugger, so they don't need to be implemented

        int IDebugEngine2.EnumPrograms(out IEnumDebugPrograms2 programs)
        {
            Debug.Fail("This function is not called by the debugger");

            programs = null;
            return Constants.E_NOTIMPL;
        }

        public int Attach(IDebugEventCallback2 pCallback)
        {
            Debug.Fail("This function is not called by the debugger");

            return Constants.E_NOTIMPL;
        }

        public int GetProcess(out IDebugProcess2 process)
        {
            Debug.Fail("This function is not called by the debugger");

            process = null;
            return Constants.E_NOTIMPL;
        }

        public int Execute()
        {
            Debug.Fail("This function is not called by the debugger.");
            return Constants.E_NOTIMPL;
        }

        #endregion
    }
}
