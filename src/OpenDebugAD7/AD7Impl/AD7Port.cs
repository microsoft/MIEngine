// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDebugAD7.AD7Impl
{
    internal sealed class AD7Port : IDebugPort2, IDebugDefaultPort2
    {
        private readonly Dictionary<AD_PROCESS_ID, AD7Process> _processMap = new Dictionary<AD_PROCESS_ID, AD7Process>();
        private readonly IDebugPortNotify2 _portNotify;

        internal AD7Port(IDebugPortNotify2 portNotify)
        {
            _portNotify = portNotify;
        }

        public int GetPortName(out string pbstrName)
        {
            throw new NotImplementedException();
        }

        public int GetPortId(out Guid pguidPort)
        {
            throw new NotImplementedException();
        }

        public int GetPortRequest(out IDebugPortRequest2 ppRequest)
        {
            throw new NotImplementedException();
        }

        public int GetPortSupplier(out IDebugPortSupplier2 ppSupplier)
        {
            throw new NotImplementedException();
        }

        public int GetProcess(AD_PROCESS_ID processId, out IDebugProcess2 ppProcess)
        {
            AD7Process process;
            lock (_processMap)
            {
                if (!_processMap.TryGetValue(processId, out process))
                {
                    process = new AD7Process(this, processId);
                    _processMap.Add(processId, process);
                }
            }

            ppProcess = process;
            return HRConstants.S_OK;
        }

        public int EnumProcesses(out IEnumDebugProcesses2 ppEnum)
        {
            throw new NotImplementedException();
        }

        public bool RemoveProcess(IDebugProcess2 process)
        {
            var ad7Process = process as AD7Process;
            if (ad7Process == null)
            {
                throw new ArgumentOutOfRangeException("process");
            }

            lock (_processMap)
            {
                return _processMap.Remove(ad7Process.PhysicalProcessId);
            }
        }

        public int GetPortNotify(out IDebugPortNotify2 portNotify)
        {
            portNotify = _portNotify;
            return HRConstants.S_OK;
        }

        public int GetServer(out IDebugCoreServer3 ppServer)
        {
            throw new NotImplementedException();
        }

        public int QueryIsLocal()
        {
            throw new NotImplementedException();
        }
    }
}
