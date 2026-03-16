// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Debugger.Interop;

namespace Microsoft.SSHDebugPS
{ 
    internal abstract class AD7PortSupplier : IDebugPortSupplier2, IDebugPortSupplier3, IDebugPortSupplierDescription2
    {
        private readonly object _portLock = new object();
        private readonly Dictionary<string, AD7Port> _ports = new Dictionary<string, AD7Port>(StringComparer.OrdinalIgnoreCase);

        protected abstract Guid Id { get; }
        protected abstract string Name { get; }
        protected abstract string Description { get; }

        public AD7PortSupplier()
        { }

        public abstract int AddPort(IDebugPortRequest2 request, out IDebugPort2 port);

        /// <summary>
        /// Track a port by name. If a port with the same name already exists,
        /// close its connection first to prevent connection leaks.
        /// </summary>
        protected void TrackPort(string name, AD7Port port)
        {
            lock (_portLock)
            {
                if (_ports.TryGetValue(name, out AD7Port oldPort) && oldPort != port)
                {
                    oldPort.Clean();
                }
                _ports[name] = port;
            }
        }

        public virtual int CanAddPort()
        {
            return HR.S_OK;
        }

        public virtual int GetPortSupplierId(out Guid guidPortSupplier)
        {
            guidPortSupplier = Id;
            return HR.S_OK;
        }

        public int GetPortSupplierName(out string name)
        {
            name = Name;
            return HR.S_OK;
        }

        int IDebugPortSupplierDescription2.GetDescription(enum_PORT_SUPPLIER_DESCRIPTION_FLAGS[] flags, out string text)
        {
            text = Description;
            return HR.S_OK;
        }

        public virtual int EnumPorts(out IEnumDebugPorts2 ppEnum)
        {
            throw new NotImplementedException();
        }

        public int GetPort(ref Guid guidPort, out IDebugPort2 ppPort)
        {
            throw new NotImplementedException();
        }

        public int RemovePort(IDebugPort2 pPort)
        {
            AD7Port ad7Port = pPort as AD7Port;
            if (ad7Port == null)
            {
                return HR.E_FAIL;
            }

            lock (_portLock)
            {
                string keyToRemove = null;
                foreach (var kvp in _ports)
                {
                    if (kvp.Value == ad7Port)
                    {
                        keyToRemove = kvp.Key;
                        break;
                    }
                }
                if (keyToRemove != null)
                {
                    _ports.Remove(keyToRemove);
                }
            }

            ad7Port.Clean();
            return HR.S_OK;
        }

        #region IDebugPortSupplier3 
        public virtual int CanPersistPorts()
        {
            return HR.S_FALSE;
        }

        /// <summary>
        /// If CanPersistPorts() returns false, the SDM will cache the ports and EnumPersistedPorts() needs to be implemented
        /// </summary>
        /// <param name="portNames"></param>
        /// <param name="portEnum"></param>
        /// <returns></returns>
        public abstract int EnumPersistedPorts(BSTR_ARRAY portNames, out IEnumDebugPorts2 portEnum);
        #endregion
    }
}
