// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace DebuggerTesting.Utilities
{
    public static class EnumerableExtensions
    {
        public static ISet<TKey> ToKeySet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return new HashSet<TKey>(dictionary.Keys);
        }
    }
}
