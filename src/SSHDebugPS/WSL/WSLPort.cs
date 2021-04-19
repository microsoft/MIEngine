// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SSHDebugPS.WSL
{
    internal class WSLPort : AD7Port
    {
        public WSLPort(AD7PortSupplier portSupplier, string name, bool isInAddPort)
            : base(portSupplier, name, isInAddPort) { }

        protected override Connection GetConnectionInternal()
        {
            return new WSLConnection(Name);
        }
    }
}