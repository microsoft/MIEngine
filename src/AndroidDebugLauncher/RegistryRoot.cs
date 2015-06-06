// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidDebugLauncher
{
    internal static class RegistryRoot
    {
        private static string s_value;

        static public string Value
        {
            get
            {
                if (s_value == null)
                {
                    Debug.Fail("RegistryRoot.Value queried before it is set");
                    throw new InvalidOperationException();
                }

                return s_value;
            }
        }

        internal static void Set(string registryRoot)
        {
            // Initialize the registry root the first time this is called. Subsequent calls are a no-op.
            Interlocked.CompareExchange<string>(ref s_value, registryRoot, null);
            Debug.Assert(s_value == registryRoot);
        }
    }
}
