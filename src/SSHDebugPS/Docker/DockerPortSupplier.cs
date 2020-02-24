// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Debugger.Interop;

namespace Microsoft.SSHDebugPS.Docker
{
    [ComVisible(true)]
    [Guid("18F976B7-D66D-4C36-9CAE-A9217E0E3DF4")]
    internal class DockerPortSupplier : AD7PortSupplier
    {
        private readonly Guid _Id = new Guid("A2BBC114-47E4-473F-A49C-69EE89711243");

        protected override Guid Id { get { return _Id; } }
        protected override string Name { get { return StringResources.Docker_PSName; } }
        protected override string Description { get { return StringResources.Docker_PSDescription; } }

        public DockerPortSupplier() : base()
        { }

        public override int AddPort(IDebugPortRequest2 request, out IDebugPort2 port)
        {
            string name;
            HR.Check(request.GetPortName(out name));

            if (!string.IsNullOrWhiteSpace(name))
            {
                AD7Port newPort = new DockerPort(this, name, isInAddPort: true);

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

                ports[c] = new DockerPort(this, name, isInAddPort: false);
            }

            portEnum = new AD7PortEnum(ports);
            return HR.S_OK;
        }
    }

}
