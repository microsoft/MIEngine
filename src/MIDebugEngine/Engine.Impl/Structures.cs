// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Collections.ObjectModel;
using MICore;

namespace Microsoft.MIDebugEngine
{
    internal class ThreadContext
    {
        public ThreadContext(ulong? addr, MITextPosition textPosition, string function, uint level, string from)
        {
            pc = addr;
            sp = 0;
            TextPosition = textPosition;
            Function = function;
            Level = level;
            From = from;
        }

        /// <summary>
        /// [Optional] Program counter. This will be null for an annotated frame.
        /// </summary>
        public ulong? pc { get; private set; }
        public ulong sp { get; private set; }
        public string Function { get; private set; }
        public MITextPosition TextPosition { get; private set; }

        public string From { get; private set; }

        public uint Level { get; private set; }

        /// <summary>
        /// Finds the module for this context
        /// </summary>
        /// <param name="process">process of the context</param>
        /// <returns>[Optional] Module, if the frame has one</returns>
        internal DebuggedModule FindModule(DebuggedProcess process)
        {
            // CLRDBG TODO: Use module id to find the module

            if (this.pc.HasValue)
            {
                return process.ResolveAddress(this.pc.Value);
            }

            return null;
        }
    }

    public class OutputMessage
    {
        public enum Severity
        {
            Error,
            Warning
        };

        public readonly string Message;
        public readonly enum_MESSAGETYPE MessageType;
        public readonly Severity SeverityValue;

        /// <summary>
        /// Error HRESULT to send to the debug package. 0 (S_OK) if there is no associated error code.
        /// </summary>
        public readonly uint ErrorCode;

        public OutputMessage(string message, enum_MESSAGETYPE messageType, Severity severity, uint errorCode = 0)
        {
            this.Message = message;
            this.MessageType = messageType;
            this.SeverityValue = severity;
            this.ErrorCode = errorCode;
        }
    }

    public interface ISampleEngineCallback
    {
        void OnModuleLoad(DebuggedModule module);
        void OnModuleUnload(DebuggedModule module);
        void OnThreadStart(DebuggedThread thread);
        void OnThreadExit(DebuggedThread thread, uint exitCode);
        void OnProcessExit(uint exitCode);
        void OnOutputString(string outputString);
        void OnOutputMessage(OutputMessage outputMessage);

        /// <summary>
        /// Raises an error event to Visual Studio, which will show a message box. Note that this function should never throw.
        /// </summary>
        /// <param name="message">[Required] message to send</param>
        void OnError(string message);
        void OnBreakpoint(DebuggedThread thread, ReadOnlyCollection<object> clients);
        void OnException(DebuggedThread thread, string name, string description, uint code, Guid? exceptionCategory = null, ExceptionBreakpointState state = ExceptionBreakpointState.None);
        void OnStepComplete(DebuggedThread thread);
        void OnAsyncBreakComplete(DebuggedThread thread);
        void OnLoadComplete(DebuggedThread thread);
        //void OnProgramDestroy(uint exitCode);
        void OnSymbolSearch(DebuggedModule module, string status, uint dwStatsFlags);
        void OnBreakpointBound(Object objPendingBreakpoint);
        void OnEntryPoint(DebuggedThread thread);
    };

    public class Constants
    {
        public const int S_OK = 0;
        public const int S_FALSE = 1;
        public const int E_NOTIMPL = unchecked((int)0x80004001);
        public const int E_FAIL = unchecked((int)0x80004005);
        public const int E_ABORT = unchecked((int)(0x80004004));
        public const int RPC_E_SERVERFAULT = unchecked((int)(0x80010105));
    };

    public class RegisterGroup
    {
        public readonly string Name;
        internal int Count { get; set; }

        public RegisterGroup(string name)
        {
            Name = name;
            Count = 0;
        }
    }

    public class RegisterDescription
    {
        public readonly string Name;
        public RegisterGroup Group { get; private set; }
        public readonly int Index;
        public RegisterDescription(string name, RegisterGroup group, int i)
        {
            Name = name;
            Group = group;
            Index = i;
            Group.Count++;
        }
    };
}
