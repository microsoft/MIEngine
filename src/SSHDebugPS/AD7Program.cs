// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SSHDebugPS
{
    [DebuggerDisplay("Program for process:{_process._processId}")]
    internal class AD7Program : IDebugProgram2
    {
        private readonly AD7Process _process;
        private readonly Guid _engineId;
        private readonly Lazy<Guid> _uniqueId = new Lazy<Guid>(() => Guid.NewGuid(), LazyThreadSafetyMode.ExecutionAndPublication);

        internal AD7Program(AD7Process process, Guid engineId)
        {
            _process = process;
            _engineId = engineId;
        }

        int IDebugProgram2.Attach(IDebugEventCallback2 pCallback)
        {
            return HR.S_FALSE; // indicate that the attach happens through the engine rather than the port supplier
        }

        int IDebugProgram2.CanDetach()
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.CauseBreak()
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.Continue(IDebugThread2 pThread)
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.Detach()
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.EnumCodeContexts(IDebugDocumentPosition2 pDocPos, out IEnumDebugCodeContexts2 ppEnum)
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.EnumCodePaths(string pszHint, IDebugCodeContext2 pStart, IDebugStackFrame2 pFrame, int fSource, out IEnumCodePaths2 ppEnum, out IDebugCodeContext2 ppSafety)
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.EnumModules(out IEnumDebugModules2 ppEnum)
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.Execute()
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.GetDebugProperty(out IDebugProperty2 ppProperty)
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.GetDisassemblyStream(enum_DISASSEMBLY_STREAM_SCOPE dwScope, IDebugCodeContext2 pCodeContext, out IDebugDisassemblyStream2 ppDisassemblyStream)
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.GetENCUpdate(out object ppUpdate)
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.GetEngineInfo(out string engineName, out Guid guidEngine)
        {
            // TODO: Do we need a real engine name?
            engineName = "<SSH-Engine>";
            guidEngine = _engineId;
            return HR.S_OK;
        }

        int IDebugProgram2.GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.GetName(out string pbstrName)
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.GetProcess(out IDebugProcess2 process)
        {
            process = _process;
            return HR.S_OK;
        }

        int IDebugProgram2.GetProgramId(out Guid guidProgramId)
        {
            guidProgramId = _uniqueId.Value;
            return HR.S_OK;
        }

        int IDebugProgram2.Step(IDebugThread2 pThread, enum_STEPKIND sk, enum_STEPUNIT Step)
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.Terminate()
        {
            throw new NotImplementedException();
        }

        int IDebugProgram2.WriteDump(enum_DUMPTYPE DUMPTYPE, string pszDumpUrl)
        {
            throw new NotImplementedException();
        }
    }
}
