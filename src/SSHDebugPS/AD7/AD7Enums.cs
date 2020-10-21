// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.VisualStudio.Debugger.Interop;

// These classes use a generic enumerator implementation to create the various enumerators required by the port supplier.

namespace Microsoft.SSHDebugPS
{
    #region Base Class
    internal class AD7Enum<T, I> where I : class
    {
        private static readonly object _lock = new object();

        private readonly T[] _data;
        private uint _position;

        public AD7Enum(T[] data)
        {
            _data = data;
            _position = 0;
        }

        public int Clone(out I ppEnum)
        {
            ppEnum = null;
            return HR.E_NOTIMPL;
        }

        public int GetCount(out uint pcelt)
        {
            pcelt = (uint)_data.Length;
            return HR.S_OK;
        }

        public int Next(uint celt, T[] rgelt, out uint celtFetched)
        {
            return Move(celt, rgelt, out celtFetched);
        }

        public int Reset()
        {
            lock (_lock)
            {
                _position = 0;

                return HR.S_OK;
            }
        }

        public int Skip(uint celt)
        {
            uint celtFetched;

            return Move(celt, null, out celtFetched);
        }

        private int Move(uint celt, T[] rgelt, out uint celtFetched)
        {
            lock (_lock)
            {
                int hr = HR.S_OK;
                celtFetched = (uint)_data.Length - _position;

                if (celt > celtFetched)
                {
                    hr = HR.S_FALSE;
                }
                else if (celt < celtFetched)
                {
                    celtFetched = celt;
                }

                if (rgelt != null)
                {
                    for (int c = 0; c < celtFetched; c++)
                    {
                        rgelt[c] = _data[_position + c];
                    }
                }

                _position += celtFetched;

                return hr;
            }
        }
    }
    #endregion Base Class

    internal class AD7PortEnum : AD7Enum<IDebugPort2, IEnumDebugPorts2>, IEnumDebugPorts2
    {
        public AD7PortEnum(IDebugPort2[] data) : base(data)
        {
        }

        public int Next(uint celt, IDebugPort2[] elements, ref uint cFetched)
        {
            return base.Next(celt, elements, out cFetched);
        }
    }

    internal class AD7ProcessEnum : AD7Enum<IDebugProcess2, IEnumDebugProcesses2>, IEnumDebugProcesses2
    {
        public AD7ProcessEnum(IDebugProcess2[] data) : base(data)
        {
        }

        public int Next(uint celt, IDebugProcess2[] elements, ref uint cFetched)
        {
            return base.Next(celt, elements, out cFetched);
        }
    }

    internal class AD7ProgramEnum : AD7Enum<IDebugProgram2, IEnumDebugPrograms2>, IEnumDebugPrograms2
    {
        public AD7ProgramEnum(IDebugProgram2[] data) : base(data)
        {
        }

        public int Next(uint celt, IDebugProgram2[] elements, ref uint cFetched)
        {
            return base.Next(celt, elements, out cFetched);
        }
    }
}
