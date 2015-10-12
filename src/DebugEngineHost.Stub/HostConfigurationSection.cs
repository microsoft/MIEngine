// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DebugEngineHost
{
    public class HostConfigurationSection : IDisposable
    {
        public void Dispose() { }

        public object GetValue(string valueName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetValueNames()
        {
            throw new NotImplementedException();
        }
    }
}