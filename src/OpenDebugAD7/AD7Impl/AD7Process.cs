// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDebugAD7.AD7Impl
{
    internal sealed class AD7Process : IDebugProcess2
    {
        public readonly AD_PROCESS_ID PhysicalProcessId;
        private readonly IDebugPort2 _port;
        private readonly Lazy<Guid> _processId = new Lazy<Guid>(() => Guid.NewGuid(), LazyThreadSafetyMode.PublicationOnly);

        internal AD7Process(IDebugPort2 port, AD_PROCESS_ID processId)
        {
            _port = port;
            this.PhysicalProcessId = processId;
        }

        public int Attach(IDebugEventCallback2 pCallback, Guid[] rgguidSpecificEngines, uint celtSpecificEngines, int[] rghrEngineAttach)
        {
            throw new NotImplementedException();
        }

        public int CanDetach()
        {
            throw new NotImplementedException();
        }

        public int CauseBreak()
        {
            throw new NotImplementedException();
        }

        public int Detach()
        {
            throw new NotImplementedException();
        }

        public int EnumPrograms(out IEnumDebugPrograms2 ppEnum)
        {
            throw new NotImplementedException();
        }

        public int EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
            throw new NotImplementedException();
        }

        public int GetAttachedSessionName(out string pbstrSessionName)
        {
            throw new NotImplementedException();
        }

        public int GetInfo(enum_PROCESS_INFO_FIELDS Fields, PROCESS_INFO[] pProcessInfo)
        {
            throw new NotImplementedException();
        }

        public int GetName(enum_GETNAME_TYPE gnType, out string pbstrName)
        {
            throw new NotImplementedException();
        }

        public int GetPhysicalProcessId(AD_PROCESS_ID[] pProcessId)
        {
            pProcessId[0] = this.PhysicalProcessId;
            return HRConstants.S_OK;
        }

        public int GetPort(out IDebugPort2 ppPort)
        {
            ppPort = _port;
            return HRConstants.S_OK;
        }

        public int GetProcessId(out Guid pguidProcessId)
        {
            pguidProcessId = _processId.Value;
            return HRConstants.S_OK;
        }

        public int GetServer(out IDebugCoreServer2 ppServer)
        {
            throw new NotImplementedException();
        }

        public int Terminate()
        {
            throw new NotImplementedException();
        }
    }
}
