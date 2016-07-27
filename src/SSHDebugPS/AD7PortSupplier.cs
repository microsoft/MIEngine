// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SSHDebugPS
{
    [ComVisible(true)]
    [Guid("1326FB0D-EA51-4F90-BEDF-5948588B0FE1")]
    internal class AD7PortSupplier : IDebugPortSupplier2, IDebugPortSupplier3, IDebugPortSupplierDescription2
    {
        private const string Name = "SSH";
        private readonly Guid _Id = new Guid("3FDDF14E-E758-4695-BE0C-7509920432C9");
        public AD7PortSupplier()
        {
        }

        public int AddPort(IDebugPortRequest2 request, out IDebugPort2 port)
        {
            string name;
            HR.Check(request.GetPortName(out name));

            port = new AD7Port(this, name, isInAddPort: true);
            return HR.S_OK;
        }

        public int CanAddPort()
        {
            return HR.S_OK;
        }

        public int EnumPorts(out IEnumDebugPorts2 ppEnum)
        {
            throw new NotImplementedException();
        }

        public int GetPort(ref Guid guidPort, out IDebugPort2 ppPort)
        {
            throw new NotImplementedException();
        }

        public int GetPortSupplierId(out Guid guidPortSupplier)
        {
            guidPortSupplier = _Id;
            return HR.S_OK;
        }

        public int GetPortSupplierName(out string name)
        {
            name = Name;
            return HR.S_OK;
        }

        public int RemovePort(IDebugPort2 pPort)
        {
            throw new NotImplementedException();
        }

        public int CanPersistPorts()
        {
            // Tell the SDM that we would like it to keep an MRU of port names for us
            return HR.S_FALSE;
        }

        public unsafe int EnumPersistedPorts(BSTR_ARRAY portNames, out IEnumDebugPorts2 portEnum)
        {
            IDebugPort2[] ports = new IDebugPort2[portNames.dwCount];
            for (int c = 0; c < portNames.dwCount; c++)
            {
                char* bstrPortName = ((char**)portNames.Members)[c];
                string name = new string(bstrPortName);

                ports[c] = new AD7Port(this, name, isInAddPort: false);
            }

            portEnum = new AD7PortEnum(ports);
            return HR.S_OK;
        }

        int IDebugPortSupplierDescription2.GetDescription(enum_PORT_SUPPLIER_DESCRIPTION_FLAGS[] flags, out string text)
        {
            text = StringResources.PSDescription;
            return HR.S_OK;
        }
    }
}
