// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Threading;
using MICore;
using Microsoft.DebugEngineHost;

namespace Microsoft.MIDebugEngine
{
    internal class EngineCallback : ISampleEngineCallback, MICore.IDeviceAppLauncherEventCallback
    {
        private readonly IDebugEventCallback2 _eventCallback;
        private readonly AD7Engine _engine;
        private int _sentProgramDestroy;

        public EngineCallback(AD7Engine engine, IDebugEventCallback2 ad7Callback)
        {
            _engine = engine;
            _eventCallback = HostMarshal.GetThreadSafeEventCallback(ad7Callback);
        }

        public void Send(IDebugEvent2 eventObject, string iidEvent, IDebugProgram2 program, IDebugThread2 thread)
        {
            uint attributes;
            Guid riidEvent = new Guid(iidEvent);
            if (!(eventObject is AD7OutputDebugStringEvent))
            {
                _engine.Logger.WriteLine("Send Event {0}", eventObject.GetType().Name);
            }
            EngineUtils.RequireOk(eventObject.GetAttributes(out attributes));
            EngineUtils.RequireOk(_eventCallback.Event(_engine, null, program, thread, eventObject, ref riidEvent, attributes));
        }

        public void Send(IDebugEvent2 eventObject, string iidEvent, IDebugThread2 thread)
        {
            IDebugProgram2 program = _engine;
            if (!_engine.ProgramCreateEventSent)
            {
                // Any events before programe create shouldn't include the program
                program = null;
            }

            Send(eventObject, iidEvent, program, thread);
        }

        public void OnError(string message)
        {
            SendMessage(message, OutputMessage.Severity.Error, isAsync: true);
        }

        /// <summary>
        /// Sends an error to the user, blocking until the user dismisses the error
        /// </summary>
        /// <param name="message">string to display to the user</param>
        public void OnErrorImmediate(string message)
        {
            SendMessage(message, OutputMessage.Severity.Error, isAsync: false);
        }

        public void OnWarning(string message)
        {
            SendMessage(message, OutputMessage.Severity.Warning, isAsync: true);
        }

        public void OnModuleLoad(DebuggedModule debuggedModule)
        {
            // This will get called when the entrypoint breakpoint is fired because the engine sends a mod-load event
            // for the exe.
            if (_engine.DebuggedProcess != null)
            {
                Debug.Assert(_engine.DebuggedProcess.WorkerThread.IsPollThread());
            }

            AD7Module ad7Module = new AD7Module(debuggedModule, _engine.DebuggedProcess);
            AD7ModuleLoadEvent eventObject = new AD7ModuleLoadEvent(ad7Module, true /* this is a module load */);

            debuggedModule.Client = ad7Module;

            // The sample engine does not support binding breakpoints as modules load since the primary exe is the only module
            // symbols are loaded for. A production debugger will need to bind breakpoints when a new module is loaded.

            Send(eventObject, AD7ModuleLoadEvent.IID, null);
        }

        public void OnSymbolsLoaded(DebuggedModule module)
        {
            var eventObject = new AD7SymbolLoadEvent(module.Client as AD7Module);
            Send(eventObject, AD7SymbolLoadEvent.IID, null);
        }

        public void OnModuleUnload(DebuggedModule debuggedModule)
        {
            Debug.Assert(_engine.DebuggedProcess.WorkerThread.IsPollThread());

            AD7Module ad7Module = (AD7Module)debuggedModule.Client;
            Debug.Assert(ad7Module != null);

            AD7ModuleLoadEvent eventObject = new AD7ModuleLoadEvent(ad7Module, false /* this is a module unload */);

            Send(eventObject, AD7ModuleLoadEvent.IID, null);
        }

        public void OnOutputString(string outputString)
        {
            AD7OutputDebugStringEvent eventObject = new AD7OutputDebugStringEvent(outputString);

            Send(eventObject, AD7OutputDebugStringEvent.IID, null);
        }

        public void OnOutputMessage(OutputMessage outputMessage)
        {
            try
            {
                if (outputMessage.ErrorCode == 0)
                {
                    var eventObject = new AD7MessageEvent(outputMessage, isAsync: true);
                    Send(eventObject, AD7MessageEvent.IID, null);
                }
                else
                {
                    var eventObject = new AD7ErrorEvent(outputMessage, isAsync: true);
                    Send(eventObject, AD7ErrorEvent.IID, null);
                }
            }
            catch
            {
                // Since we are often trying to report an exception, if something goes wrong we don't want to take down the process,
                // so ignore the failure.
            }
        }

        public void OnProcessExit(uint exitCode)
        {
            if (Interlocked.Exchange(ref _sentProgramDestroy, 1) == 0)
            {
                AD7ProgramDestroyEvent eventObject = new AD7ProgramDestroyEvent(exitCode);

                try
                {
                    Send(eventObject, AD7ProgramDestroyEvent.IID, null);
                }
                catch (InvalidOperationException)
                {
                    // If debugging has already stopped, this can throw
                }
            }
        }

        public void OnEntryPoint(DebuggedThread thread)
        {
            AD7EntryPointEvent eventObject = new AD7EntryPointEvent();

            Send(eventObject, AD7EntryPointEvent.IID, (AD7Thread)thread.Client);
        }

        public void OnThreadExit(DebuggedThread debuggedThread, uint exitCode)
        {
            Debug.Assert(_engine.DebuggedProcess.WorkerThread.IsPollThread());

            AD7Thread ad7Thread = (AD7Thread)debuggedThread.Client;
            Debug.Assert(ad7Thread != null);

            AD7ThreadDestroyEvent eventObject = new AD7ThreadDestroyEvent(exitCode);

            Send(eventObject, AD7ThreadDestroyEvent.IID, ad7Thread);
        }

        public void OnThreadStart(DebuggedThread debuggedThread)
        {
            // This will get called when the entrypoint breakpoint is fired because the engine sends a thread start event
            // for the main thread of the application.
            if (_engine.DebuggedProcess != null)
            {
                Debug.Assert(_engine.DebuggedProcess.WorkerThread.IsPollThread());
            }

            AD7ThreadCreateEvent eventObject = new AD7ThreadCreateEvent();
            Send(eventObject, AD7ThreadCreateEvent.IID, (IDebugThread2)debuggedThread.Client);
        }

        public void OnBreakpoint(DebuggedThread thread, ReadOnlyCollection<object> clients)
        {
            IDebugBoundBreakpoint2[] boundBreakpoints = new IDebugBoundBreakpoint2[clients.Count];

            int i = 0;
            foreach (object objCurrentBreakpoint in clients)
            {
                boundBreakpoints[i] = (IDebugBoundBreakpoint2)objCurrentBreakpoint;
                i++;
            }

            // An engine that supports more advanced breakpoint features such as hit counts, conditions and filters
            // should notify each bound breakpoint that it has been hit and evaluate conditions here.
            // The sample engine does not support these features.

            AD7BreakpointEvent eventObject = new AD7BreakpointEvent(boundBreakpoints);

            AD7Thread ad7Thread = (AD7Thread)thread.Client;
            Send(eventObject, AD7BreakpointEvent.IID, ad7Thread);
        }

        // Exception events are sent when an exception occurs in the debuggee that the debugger was not expecting.
        public void OnException(DebuggedThread thread, string name, string description, uint code, Guid? exceptionCategory = null, ExceptionBreakpointStates state = ExceptionBreakpointStates.None)
        {
            AD7ExceptionEvent eventObject = new AD7ExceptionEvent(name, description, code, exceptionCategory, state);

            AD7Thread ad7Thread = (AD7Thread)thread.Client;
            Send(eventObject, AD7ExceptionEvent.IID, ad7Thread);
        }

        public void OnExpressionEvaluationComplete(IVariableInformation var, IDebugProperty2 prop = null)
        {
            AD7ExpressionCompleteEvent eventObject = new AD7ExpressionCompleteEvent(_engine, var, prop);
            Send(eventObject, AD7ExpressionCompleteEvent.IID, var.Client);
        }

        public void OnStepComplete(DebuggedThread thread)
        {
            // Step complete is sent when a step has finished
            AD7StepCompleteEvent eventObject = new AD7StepCompleteEvent();

            AD7Thread ad7Thread = (AD7Thread)thread.Client;
            Send(eventObject, AD7StepCompleteEvent.IID, ad7Thread);
        }

        public void OnAsyncBreakComplete(DebuggedThread thread)
        {
            // This will get called when the engine receives the breakpoint event that is created when the user
            // hits the pause button in vs.
            Debug.Assert(_engine.DebuggedProcess.WorkerThread.IsPollThread());

            AD7Thread ad7Thread = (AD7Thread)thread.Client;
            AD7AsyncBreakCompleteEvent eventObject = new AD7AsyncBreakCompleteEvent();
            Send(eventObject, AD7AsyncBreakCompleteEvent.IID, ad7Thread);
        }

        public void OnLoadComplete()
        {
            AD7LoadCompleteEvent eventObject = new AD7LoadCompleteEvent();
            Send(eventObject, AD7LoadCompleteEvent.IID, null);
        }

        public void OnProgramDestroy(uint exitCode)
        {
            AD7ProgramDestroyEvent eventObject = new AD7ProgramDestroyEvent(exitCode);
            Send(eventObject, AD7ProgramDestroyEvent.IID, null);
        }

        // Engines notify the debugger that a breakpoint has bound through the breakpoint bound event.
        public void OnBreakpointBound(object objBoundBreakpoint)
        {
            AD7BoundBreakpoint boundBreakpoint = (AD7BoundBreakpoint)objBoundBreakpoint;
            IDebugPendingBreakpoint2 pendingBreakpoint;
            ((IDebugBoundBreakpoint2)boundBreakpoint).GetPendingBreakpoint(out pendingBreakpoint);

            AD7BreakpointBoundEvent eventObject = new AD7BreakpointBoundEvent((AD7PendingBreakpoint)pendingBreakpoint, boundBreakpoint);
            Send(eventObject, AD7BreakpointBoundEvent.IID, null);
        }

        // Engines notify the SDM that a pending breakpoint failed to bind through the breakpoint error event
        public void OnBreakpointError(AD7ErrorBreakpoint bperr)
        {
            AD7BreakpointErrorEvent eventObject = new AD7BreakpointErrorEvent(bperr);
            Send(eventObject, AD7BreakpointErrorEvent.IID, null);
        }

        // Engines notify the SDM that a bound breakpoint change resulted in an error
        public void OnBreakpointUnbound(AD7BoundBreakpoint bp, enum_BP_UNBOUND_REASON reason)
        {
            AD7BreakpointUnboundEvent eventObject = new AD7BreakpointUnboundEvent(bp, reason);
            Send(eventObject, AD7BreakpointUnboundEvent.IID, null);
        }

        public void OnCustomDebugEvent(Guid guidVSService, Guid sourceId, int messageCode, object parameter1, object parameter2)
        {
            var eventObject = new AD7CustomDebugEvent(guidVSService, sourceId, messageCode, parameter1, parameter2);
            Send(eventObject, AD7CustomDebugEvent.IID, null);
        }

        public void OnStopComplete(DebuggedThread thread)
        {
            var eventObject = new AD7StopCompleteEvent();
            Send(eventObject, AD7StopCompleteEvent.IID, (AD7Thread)thread.Client);
        }

        private void SendMessage(string message, OutputMessage.Severity severity, bool isAsync)
        {
            try
            {
                // IDebugErrorEvent2 is used to report error messages to the user when something goes wrong in the debug engine.
                // The sample engine doesn't take advantage of this.

                AD7MessageEvent eventObject = new AD7MessageEvent(new OutputMessage(message, enum_MESSAGETYPE.MT_MESSAGEBOX, severity), isAsync);
                Send(eventObject, AD7MessageEvent.IID, null);
            }
            catch
            {
                // Since we are often trying to report an exception, if something goes wrong we don't want to take down the process,
                // so ignore the failure.
            }
        }
    }
}
