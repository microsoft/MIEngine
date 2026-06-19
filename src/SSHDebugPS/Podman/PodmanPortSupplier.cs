// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Debugger.Interop;

namespace Microsoft.SSHDebugPS.Podman
{
    [ComVisible(true)]
    [Guid("C9E1E1E4-3E5A-4F2B-8D1A-5C6F7A8B9D0E")]
    internal sealed class PodmanPortSupplier : AD7PortSupplier
    {
        private readonly Guid _Id = new Guid("D4F2F3A5-6B7C-4E8D-9F0A-1B2C3D4E5F6A");

        protected override Guid Id { get { return _Id; } }
        protected override string Name { get { return StringResources.Podman_PSName; } }
        protected override string Description { get { return StringResources.Podman_PSDescription; } }

        public PodmanPortSupplier() : base()
        { }

        public override int AddPort(IDebugPortRequest2 request, out IDebugPort2 port)
        {
            string name;
            HR.Check(request.GetPortName(out name));

            if (!string.IsNullOrWhiteSpace(name))
            {
                AD7Port newPort = new PodmanPort(this, name, isInAddPort: true);

                if (newPort.IsConnected)
                {
                    port = newPort;
                    return HR.S_OK;
                }
            }

            port = null;
            return HR.E_REMOTE_CONNECT_USER_CANCELED;
        }

        public override unsafe int EnumPersistedPorts(BSTR_ARRAY portNames, out IEnumDebugPorts2 portEnum)
        {
            IDebugPort2[] ports = new IDebugPort2[portNames.dwCount];
            for (int c = 0; c < portNames.dwCount; c++)
            {
                char* bstrPortName = ((char**)portNames.Members)[c];
                string name = new string(bstrPortName);

                ports[c] = new PodmanPort(this, name, isInAddPort: false);
            }

            portEnum = new AD7PortEnum(ports);
            return HR.S_OK;
        }
    }
}
