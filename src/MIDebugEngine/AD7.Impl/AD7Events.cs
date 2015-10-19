// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using MICore;
using System.Diagnostics;

// This file contains the various event objects that are sent to the debugger from the sample engine via IDebugEventCallback2::Event.
// These are used in EngineCallback.cs.
// The events are how the engine tells the debugger about what is happening in the debuggee process. 
// There are three base classe the other events derive from: AD7AsynchronousEvent, AD7StoppingEvent, and AD7SynchronousEvent. These 
// each implement the IDebugEvent2.GetAttributes method for the type of event they represent. 
// Most events sent the debugger are asynchronous events.


namespace Microsoft.MIDebugEngine
{
    #region Event base classes

    internal class AD7AsynchronousEvent : IDebugEvent2
    {
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_ASYNCHRONOUS;

        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return Constants.S_OK;
        }
    }

    internal class AD7StoppingEvent : IDebugEvent2
    {
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_ASYNC_STOP;

        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return Constants.S_OK;
        }
    }

    internal class AD7SynchronousEvent : IDebugEvent2
    {
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_SYNCHRONOUS;

        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return Constants.S_OK;
        }
    }

    internal class AD7SynchronousStoppingEvent : IDebugEvent2
    {
        public const uint Attributes = (uint)enum_EVENTATTRIBUTES.EVENT_STOPPING | (uint)enum_EVENTATTRIBUTES.EVENT_SYNCHRONOUS;

        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return Constants.S_OK;
        }
    }

    #endregion

    // The debug engine (DE) sends this interface to the session debug manager (SDM) when an instance of the DE is created.
    internal sealed class AD7EngineCreateEvent : AD7AsynchronousEvent, IDebugEngineCreateEvent2
    {
        public const string IID = "FE5B734C-759D-4E59-AB04-F103343BDD06";
        private IDebugEngine2 _engine;

        private AD7EngineCreateEvent(AD7Engine engine)
        {
            _engine = engine;
        }

        public static void Send(AD7Engine engine)
        {
            AD7EngineCreateEvent eventObject = new AD7EngineCreateEvent(engine);
            engine.Callback.Send(eventObject, IID, null, null);
        }

        int IDebugEngineCreateEvent2.GetEngine(out IDebugEngine2 engine)
        {
            engine = _engine;

            return Constants.S_OK;
        }
    }

    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a program is attached to.
    internal sealed class AD7ProgramCreateEvent : AD7SynchronousEvent, IDebugProgramCreateEvent2
    {
        public const string IID = "96CD11EE-ECD4-4E89-957E-B5D496FC4139";

        internal static void Send(AD7Engine engine)
        {
            AD7ProgramCreateEvent eventObject = new AD7ProgramCreateEvent();
            engine.Callback.Send(eventObject, IID, engine, null);
        }
    }


    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a module is loaded or unloaded.
    internal sealed class AD7ModuleLoadEvent : AD7AsynchronousEvent, IDebugModuleLoadEvent2
    {
        public const string IID = "989DB083-0D7C-40D1-A9D9-921BF611A4B2";

        private readonly AD7Module _module;
        private readonly bool _fLoad;

        public AD7ModuleLoadEvent(AD7Module module, bool fLoad)
        {
            _module = module;
            _fLoad = fLoad;
        }

        int IDebugModuleLoadEvent2.GetModule(out IDebugModule2 module, ref string debugMessage, ref int fIsLoad)
        {
            module = _module;

            if (_fLoad)
            {
                debugMessage = String.Concat("Loaded '", _module.DebuggedModule.Name, "'");
                fIsLoad = 1;
            }
            else
            {
                debugMessage = String.Concat("Unloaded '", _module.DebuggedModule.Name, "'");
                fIsLoad = 0;
            }

            return Constants.S_OK;
        }
    }

    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a program has run to completion
    // or is otherwise destroyed.
    internal sealed class AD7ProgramDestroyEvent : AD7SynchronousEvent, IDebugProgramDestroyEvent2
    {
        public const string IID = "E147E9E3-6440-4073-A7B7-A65592C714B5";

        private readonly uint _exitCode;
        public AD7ProgramDestroyEvent(uint exitCode)
        {
            _exitCode = exitCode;
        }

        #region IDebugProgramDestroyEvent2 Members

        int IDebugProgramDestroyEvent2.GetExitCode(out uint exitCode)
        {
            exitCode = _exitCode;

            return Constants.S_OK;
        }

        #endregion
    }

    internal sealed class AD7MessageEvent : IDebugEvent2, IDebugMessageEvent2
    {
        public const string IID = "3BDB28CF-DBD2-4D24-AF03-01072B67EB9E";

        private readonly OutputMessage _outputMessage;
        private readonly bool _isAsync;
        
        public AD7MessageEvent(OutputMessage outputMessage, bool isAsync)
        {
            _outputMessage = outputMessage;
            _isAsync = isAsync;
        }

        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            if (_isAsync)
                eventAttributes = (uint)enum_EVENTATTRIBUTES.EVENT_ASYNCHRONOUS;
            else
                eventAttributes = (uint)enum_EVENTATTRIBUTES.EVENT_IMMEDIATE;

            return Constants.S_OK;
        }

        int IDebugMessageEvent2.GetMessage(enum_MESSAGETYPE[] pMessageType, out string pbstrMessage, out uint pdwType, out string pbstrHelpFileName, out uint pdwHelpId)
        {
            return ConvertMessageToAD7(_outputMessage, pMessageType, out pbstrMessage, out pdwType, out pbstrHelpFileName, out pdwHelpId);
        }

        internal static int ConvertMessageToAD7(OutputMessage outputMessage, enum_MESSAGETYPE[] pMessageType, out string pbstrMessage, out uint pdwType, out string pbstrHelpFileName, out uint pdwHelpId)
        {
            const uint MB_ICONERROR = 0x00000010;
            const uint MB_ICONWARNING = 0x00000030;

            pMessageType[0] = outputMessage.MessageType;
            pbstrMessage = outputMessage.Message;
            pdwType = 0;
            if ((outputMessage.MessageType & enum_MESSAGETYPE.MT_TYPE_MASK) == enum_MESSAGETYPE.MT_MESSAGEBOX)
            {
                switch (outputMessage.SeverityValue)
                {
                    case OutputMessage.Severity.Error:
                        pdwType |= MB_ICONERROR;
                        break;

                    case OutputMessage.Severity.Warning:
                        pdwType |= MB_ICONWARNING;
                        break;
                }
            }

            pbstrHelpFileName = null;
            pdwHelpId = 0;

            return Constants.S_OK;
        }

        int IDebugMessageEvent2.SetResponse(uint dwResponse)
        {
            return Constants.S_OK;
        }
    }

    internal sealed class AD7ErrorEvent : IDebugEvent2, IDebugErrorEvent2
    {
        public const string IID = "FDB7A36C-8C53-41DA-A337-8BD86B14D5CB";

        private readonly OutputMessage _outputMessage;
        private readonly bool _isAsync;

        public AD7ErrorEvent(OutputMessage outputMessage, bool isAsync)
        {
            _outputMessage = outputMessage;
            _isAsync = isAsync;
        }

        int IDebugEvent2.GetAttributes(out uint eventAttributes)
        {
            if (_isAsync)
                eventAttributes = (uint)enum_EVENTATTRIBUTES.EVENT_ASYNCHRONOUS;
            else
                eventAttributes = (uint)enum_EVENTATTRIBUTES.EVENT_IMMEDIATE;

            return Constants.S_OK;
        }

        int IDebugErrorEvent2.GetErrorMessage(enum_MESSAGETYPE[] pMessageType, out string errorFormat, out int hrErrorReason, out uint pdwType, out string helpFilename, out uint pdwHelpId)
        {
            hrErrorReason = unchecked((int)_outputMessage.ErrorCode);
            return AD7MessageEvent.ConvertMessageToAD7(_outputMessage, pMessageType, out errorFormat, out pdwType, out helpFilename, out pdwHelpId);
        }
    }

    internal sealed class AD7BreakpointErrorEvent : AD7AsynchronousEvent, IDebugBreakpointErrorEvent2
    {
        public const string IID = "ABB0CA42-F82B-4622-84E4-6903AE90F210";

        private AD7ErrorBreakpoint _error;

        public AD7BreakpointErrorEvent(AD7ErrorBreakpoint error)
        {
            _error = error;
        }

        public int GetErrorBreakpoint(out IDebugErrorBreakpoint2 ppErrorBP)
        {
            ppErrorBP = _error;
            return Constants.S_OK;
        }
    }

    internal sealed class AD7BreakpointUnboundEvent : AD7AsynchronousEvent, IDebugBreakpointUnboundEvent2
    {
        public const string IID = "78d1db4f-c557-4dc5-a2dd-5369d21b1c8c";

        private readonly enum_BP_UNBOUND_REASON _reason;
        private AD7BoundBreakpoint _bp;

        public AD7BreakpointUnboundEvent(AD7BoundBreakpoint bp, enum_BP_UNBOUND_REASON reason)
        {
            _reason = reason;
            _bp = bp;
        }

        public int GetBreakpoint(out IDebugBoundBreakpoint2 ppBP)
        {
            ppBP = _bp;
            return Constants.S_OK;
        }

        public int GetReason(enum_BP_UNBOUND_REASON[] pdwUnboundReason)
        {
            pdwUnboundReason[0] = _reason;
            return Constants.S_OK;
        }
    }

    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a thread is created in a program being debugged.
    internal sealed class AD7ThreadCreateEvent : AD7AsynchronousEvent, IDebugThreadCreateEvent2
    {
        public const string IID = "2090CCFC-70C5-491D-A5E8-BAD2DD9EE3EA";
    }

    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a thread has exited.
    internal sealed class AD7ThreadDestroyEvent : AD7AsynchronousEvent, IDebugThreadDestroyEvent2
    {
        public const string IID = "2C3B7532-A36F-4A6E-9072-49BE649B8541";

        private readonly uint _exitCode;
        public AD7ThreadDestroyEvent(uint exitCode)
        {
            _exitCode = exitCode;
        }

        #region IDebugThreadDestroyEvent2 Members

        int IDebugThreadDestroyEvent2.GetExitCode(out uint exitCode)
        {
            exitCode = _exitCode;

            return Constants.S_OK;
        }

        #endregion
    }

    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) when a program is loaded, but before any code is executed.
    internal sealed class AD7LoadCompleteEvent : AD7StoppingEvent, IDebugLoadCompleteEvent2
    {
        public const string IID = "B1844850-1349-45D4-9F12-495212F5EB0B";

        public AD7LoadCompleteEvent()
        {
        }
    }

    internal sealed class AD7EntryPointEvent : AD7StoppingEvent, IDebugEntryPointEvent2
    {
        public const string IID = "E8414A3E-1642-48EC-829E-5F4040E16DA9";

        public AD7EntryPointEvent()
        {
        }
    }

    internal sealed class AD7ExpressionCompleteEvent : AD7AsynchronousEvent, IDebugExpressionEvaluationCompleteEvent2
    {
        public const string IID = "C0E13A85-238A-4800-8315-D947C960A843";

        public AD7ExpressionCompleteEvent(IVariableInformation var, IDebugProperty2 prop = null)
        {
            _var = var;
            _prop = prop;
        }

        public int GetExpression(out IDebugExpression2 expr)
        {
            expr = new AD7Expression(_var);
            return Constants.S_OK;
        }

        public int GetResult(out IDebugProperty2 prop)
        {
            prop = _prop != null ? _prop : new AD7Property(_var);
            return Constants.S_OK;
        }

        private IVariableInformation _var;
        private IDebugProperty2 _prop;
    }

    // This interface tells the session debug manager (SDM) that an exception has occurred in the debuggee.
    internal sealed class AD7ExceptionEvent : AD7StoppingEvent, IDebugExceptionEvent2
    {
        public const string IID = "51A94113-8788-4A54-AE15-08B74FF922D0";

        public AD7ExceptionEvent(string name, string description, uint code, Guid? exceptionCategory, ExceptionBreakpointState state)
        {
            _name = name;
            _code = code;
            _description = description ?? name;
            _category = exceptionCategory ?? new Guid(EngineConstants.EngineId);

            switch (state)
            {
                case ExceptionBreakpointState.None:
                    _state = enum_EXCEPTION_STATE.EXCEPTION_STOP_SECOND_CHANCE;
                    break;

                case ExceptionBreakpointState.BreakThrown:
                    _state = enum_EXCEPTION_STATE.EXCEPTION_STOP_FIRST_CHANCE | enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_FIRST_CHANCE;
                    break;

                case ExceptionBreakpointState.BreakUserHandled:
                    _state = enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_UNCAUGHT;
                    break;

                default:
                    Debug.Fail("Unexpected state value");
                    _state = enum_EXCEPTION_STATE.EXCEPTION_STOP_SECOND_CHANCE;
                    break;
            }
        }

        #region IDebugExceptionEvent2 Members

        public int CanPassToDebuggee()
        {
            // Cannot pass it on
            return Constants.S_FALSE;
        }

        public int GetException(EXCEPTION_INFO[] pExceptionInfo)
        {
            EXCEPTION_INFO ex = new EXCEPTION_INFO();
            ex.bstrExceptionName = _name;
            ex.dwCode = _code;
            ex.dwState = _state;
            ex.guidType = _category;
            pExceptionInfo[0] = ex;
            return Constants.S_OK;
        }

        public int GetExceptionDescription(out string pbstrDescription)
        {
            pbstrDescription = _description;
            return Constants.S_OK;
        }

        public int PassToDebuggee(int fPass)
        {
            return Constants.S_OK;
        }

        private string _name;
        private uint _code;
        private string _description;
        private Guid _category;
        private enum_EXCEPTION_STATE _state;
        #endregion
    }

    // This interface tells the session debug manager (SDM) that a step has completed
    internal sealed class AD7StepCompleteEvent : AD7StoppingEvent, IDebugStepCompleteEvent2
    {
        public const string IID = "0f7f24c1-74d9-4ea6-a3ea-7edb2d81441d";
    }

    // This interface tells the session debug manager (SDM) that an asynchronous break has been successfully completed.
    internal sealed class AD7AsyncBreakCompleteEvent : AD7StoppingEvent, IDebugBreakEvent2
    {
        public const string IID = "c7405d1d-e24b-44e0-b707-d8a5a4e1641b";
    }

    // This interface is sent by the debug engine (DE) to the session debug manager (SDM) to output a string for debug tracing.
    internal sealed class AD7OutputDebugStringEvent : AD7AsynchronousEvent, IDebugOutputStringEvent2
    {
        public const string IID = "569c4bb1-7b82-46fc-ae28-4536ddad753e";

        private string _str;
        public AD7OutputDebugStringEvent(string str)
        {
            _str = str;
        }

        #region IDebugOutputStringEvent2 Members

        int IDebugOutputStringEvent2.GetString(out string pbstrString)
        {
            pbstrString = _str;
            return Constants.S_OK;
        }

        #endregion
    }

    // This interface is sent by the debug engine (DE) to indicate the results of searching for symbols for a module in the debuggee
    internal sealed class AD7SymbolSearchEvent : AD7AsynchronousEvent, IDebugSymbolSearchEvent2
    {
        public const string IID = "638F7C54-C160-4c7b-B2D0-E0337BC61F8C";

        private AD7Module _module;
        private string _searchInfo;
        private enum_MODULE_INFO_FLAGS _symbolFlags;

        public AD7SymbolSearchEvent(AD7Module module, string searchInfo, enum_MODULE_INFO_FLAGS symbolFlags)
        {
            _module = module;
            _searchInfo = searchInfo;
            _symbolFlags = symbolFlags;
        }

        #region IDebugSymbolSearchEvent2 Members

        int IDebugSymbolSearchEvent2.GetSymbolSearchInfo(out IDebugModule3 pModule, ref string pbstrDebugMessage, enum_MODULE_INFO_FLAGS[] pdwModuleInfoFlags)
        {
            pModule = _module;
            pbstrDebugMessage = _searchInfo;
            pdwModuleInfoFlags[0] = _symbolFlags;

            return Constants.S_OK;
        }

        #endregion
    }

    // This interface is sent when a pending breakpoint has been bound in the debuggee.
    internal sealed class AD7BreakpointBoundEvent : AD7AsynchronousEvent, IDebugBreakpointBoundEvent2
    {
        public const string IID = "1dddb704-cf99-4b8a-b746-dabb01dd13a0";

        private AD7PendingBreakpoint _pendingBreakpoint;
        private AD7BoundBreakpoint _boundBreakpoint;

        public AD7BreakpointBoundEvent(AD7PendingBreakpoint pendingBreakpoint, AD7BoundBreakpoint boundBreakpoint)
        {
            _pendingBreakpoint = pendingBreakpoint;
            _boundBreakpoint = boundBreakpoint;
        }

        #region IDebugBreakpointBoundEvent2 Members

        int IDebugBreakpointBoundEvent2.EnumBoundBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            IDebugBoundBreakpoint2[] boundBreakpoints = new IDebugBoundBreakpoint2[1];
            boundBreakpoints[0] = _boundBreakpoint;
            ppEnum = new AD7BoundBreakpointsEnum(boundBreakpoints);
            return Constants.S_OK;
        }

        int IDebugBreakpointBoundEvent2.GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBP)
        {
            ppPendingBP = _pendingBreakpoint;
            return Constants.S_OK;
        }

        #endregion
    }

    // This Event is sent when a breakpoint is hit in the debuggee
    internal sealed class AD7BreakpointEvent : AD7StoppingEvent, IDebugBreakpointEvent2
    {
        public const string IID = "501C1E21-C557-48B8-BA30-A1EAB0BC4A74";

        private IEnumDebugBoundBreakpoints2 _boundBreakpoints;

        public AD7BreakpointEvent(IEnumDebugBoundBreakpoints2 boundBreakpoints)
        {
            _boundBreakpoints = boundBreakpoints;
        }

        #region IDebugBreakpointEvent2 Members

        int IDebugBreakpointEvent2.EnumBreakpoints(out IEnumDebugBoundBreakpoints2 ppEnum)
        {
            ppEnum = _boundBreakpoints;
            return Constants.S_OK;
        }

        #endregion
    }

    internal sealed class AD7CustomDebugEvent : AD7AsynchronousEvent, IDebugCustomEvent110
    {
        public const string IID = "2615D9BC-1948-4D21-81EE-7A963F20CF59";

        private readonly Guid _guidVSService;
        private readonly Guid _sourceId;
        private readonly int _messageCode;
        private readonly object _parameter1;
        private readonly object _parameter2;

        public AD7CustomDebugEvent(Guid guidVSService, Guid sourceId, int messageCode, object parameter1, object parameter2)
        {
            _guidVSService = guidVSService;
            _sourceId = sourceId;
            _messageCode = messageCode;
            _parameter1 = parameter1;
            _parameter2 = parameter2;
        }

        int IDebugCustomEvent110.GetCustomEventInfo(out Guid guidVSService, VsComponentMessage[] message)
        {
            guidVSService = _guidVSService;
            message[0].SourceId = _sourceId;
            message[0].MessageCode = (uint)_messageCode;
            message[0].Parameter1 = _parameter1;
            message[0].Parameter2 = _parameter2;

            return Constants.S_OK;
        }
    }
}
