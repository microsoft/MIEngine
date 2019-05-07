// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using liblinux;
using liblinux.Persistence;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Linux.ConnectionManager;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.SSHDebugPS.SSH
{
    [ComVisible(true)]
    [Guid("1326FB0D-EA51-4F90-BEDF-5948588B0FE1")]
    internal class SSHPortSupplier : AD7PortSupplier
    {
        private const string _Name = "SSH";
        private readonly Guid _Id = new Guid("3FDDF14E-E758-4695-BE0C-7509920432C9");

        protected override Guid Id { get { return _Id; } }
        protected override string Name { get { return _Name; } }
        protected override string Description { get { return StringResources.SSH_PSDescription; } }

        public SSHPortSupplier() : base()
        { }

        public override int AddPort(IDebugPortRequest2 request, out IDebugPort2 port)
        {
            string name;
            HR.Check(request.GetPortName(out name));

            AD7Port newPort = new SSHPort(this, name, isInAddPort: true);

            if (newPort.IsConnected)
            {
                port = newPort;
                return HR.S_OK;
            }

            port = null;
            return HR.E_REMOTE_CONNECT_USER_CANCELED;
        }

        public override int EnumPorts(out IEnumDebugPorts2 ppEnum)
        {
            ConnectionInfoStore store = new ConnectionInfoStore();
            IDebugPort2[] ports = new IDebugPort2[store.Connections.Count];

            for (int i = 0; i < store.Connections.Count; i++)
            {
                ConnectionInfo connectionInfo = (ConnectionInfo)store.Connections[i];
                ports[i] = new SSHPort(this, GetFormattedSSHConnectionName(connectionInfo), isInAddPort: false);
            }

            ppEnum = new AD7PortEnum(ports);
            return HR.S_OK;
        }

        public override int GetPortSupplierId(out Guid guidPortSupplier)
        {
            guidPortSupplier = Guid.Empty;

            // Check if liblinux exists in user's installation, if not, don't enable SSH port supplier
            try
            {
                // If Microsoft.VisualStudio.Linux.ConnectionManager.Contracts.dll, which is installed with liblinux, is not available FileNotFoundException will be thrown.
                bool libLinuxLoaded = IsLibLinuxAvailable();
                if (!libLinuxLoaded)
                {
                    return HR.E_FAIL;
                }
            }
            catch (FileNotFoundException)
            {
                return HR.E_FAIL;
            }

            return base.GetPortSupplierId(out guidPortSupplier);
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

        public override unsafe int EnumPersistedPorts(BSTR_ARRAY portNames, out IEnumDebugPorts2 portEnum)
        {
            IDebugPort2[] ports = new IDebugPort2[portNames.dwCount];
            for (int c = 0; c < portNames.dwCount; c++)
            {
                char* bstrPortName = ((char**)portNames.Members)[c];
                string name = new string(bstrPortName);

                ports[c] = new SSHPort(this, name, isInAddPort: false);
            }

            portEnum = new AD7PortEnum(ports);
            return HR.S_OK;
        }

        internal static string GetFormattedSSHConnectionName(ConnectionInfo connectionInfo)
        {
            return connectionInfo.UserName + "@" + connectionInfo.HostNameOrAddress;
        }
    }
}
