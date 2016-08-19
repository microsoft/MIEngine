// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using liblinux;
using liblinux.Persistence;
using Microsoft.VisualStudio.Linux.ConnectionManager;

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

            AD7Port newPort = new AD7Port(this, name, isInAddPort: true);

            if (newPort.IsConnected)
            {
                port = newPort;
                return HR.S_OK;
            }

            port = null;
            return HR.E_REMOTE_CONNECT_USER_CANCELED;
        }

        public int CanAddPort()
        {
            return HR.S_OK;
        }

        public int EnumPorts(out IEnumDebugPorts2 ppEnum)
        {
            ConnectionInfoStore store = new ConnectionInfoStore();
            IDebugPort2[] ports = new IDebugPort2[store.Connections.Count];

            for (int i = 0; i < store.Connections.Count; i++)
            {
                ConnectionInfo connectionInfo = (ConnectionInfo)store.Connections[i];
                ports[i] = new AD7Port(this, ConnectionManager.GetFormattedConnectionName(connectionInfo), isInAddPort: false);
            }

            ppEnum = new AD7PortEnum(ports);
            return HR.S_OK;
        }

        public int GetPort(ref Guid guidPort, out IDebugPort2 ppPort)
        {
            throw new NotImplementedException();
        }

        public int GetPortSupplierId(out Guid guidPortSupplier)
        {
            guidPortSupplier = Guid.Empty;

            // Check if liblinux exists in user's installation, if not, don't enable SSH port supplier
            bool libLinuxLoaded = IsLibLinuxAvailable();
            if (!libLinuxLoaded)
                return HR.E_FAIL; 

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
            return HR.S_OK;
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

        /// <summary>
        /// Checks if LibLinux is available by getting IVsConnectionManager service.
        /// </summary>
        /// <returns>True if LibLinux is available, false otherwise.</returns>
        private bool IsLibLinuxAvailable()
        {
            IVsShell shell = Package.GetGlobalService(typeof(SVsShell)) as IVsShell;

            if (shell == null)
                return false;

            return (ServiceProvider.GlobalProvider.GetService(typeof(IVsConnectionManager)) as IVsConnectionManager) != null;
        }
    }
}
