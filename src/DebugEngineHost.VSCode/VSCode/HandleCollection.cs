// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Generic;

namespace Microsoft.DebugEngineHost.VSCode
{
    public sealed class HandleCollection<T>
    {
        private const int START_HANDLE = 1000;
        private List<T> _collection = new List<T>();

        public void Clear()
        {
            _collection.Clear();
        }

        public int Create(T value)
        {
            _collection.Add(value);
            return _collection.Count + START_HANDLE - 1;
        }

        public bool TryGet(int handle, out T value)
        {
            if (handle < 0 || handle - START_HANDLE >= _collection.Count)
            {
                value = default;
                return false;
            }

            value = _collection[handle - START_HANDLE];
            return true;
        }

        public bool TryGetFirst(out T value)
        {
            if (IsEmpty)
            {
                value = default;
                return false;
            }

            value = _collection[0];
            return true;
        }

        public T this[int handle]
        {
            get
            {
                return _collection[handle - START_HANDLE];
            }
        }

        public bool Remove(int handle)
        {
            _collection.RemoveAt(handle - START_HANDLE);
            return true;
        }

        public bool IsEmpty
        {
            get
            {
                return _collection.Count == 0;
            }
        }
    }
}
