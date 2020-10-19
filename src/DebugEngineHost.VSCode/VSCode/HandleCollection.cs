// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.DebugEngineHost.VSCode
{
    public sealed class HandleCollection<T>
    {
        private const int START_HANDLE = 1000;

        private int _nextHandle;
        private Dictionary<int, T> _handleMap;

        public HandleCollection()
        {
            _nextHandle = START_HANDLE;
            _handleMap = new Dictionary<int, T>();
        }

        public void Reset()
        {
            _nextHandle = START_HANDLE;
            _handleMap.Clear();
        }

        public int Create(T value)
        {
            var handle = _nextHandle++;
            _handleMap[handle] = value;
            return handle;
        }

        public bool TryGet(int handle, out T value)
        {
            if (_handleMap.TryGetValue(handle, out value))
            {
                return true;
            }
            return false;
        }

        public bool TryGetFirst(out T value)
        {
            if (IsEmpty)
            {
                value = default(T);
                return false;
            }

            return TryGet(_handleMap.Keys.Min(), out value);
        }

        public T this[int handle]
        {
            get
            {
                T value;
                if (!TryGet(handle, out value))
                {
                    throw new ArgumentOutOfRangeException(nameof(handle));
                }

                return value;
            }
        }

        public bool Remove(int handle)
        {
            return _handleMap.Remove(handle);
        }

        public bool IsEmpty
        {
            get
            {
                return _handleMap.Count == 0;
            }
        }
    }
}
