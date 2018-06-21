// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Microsoft.DebugEngineHost.VSCode;
using OpenDebug;
using System;

namespace OpenDebugAD7
{
    internal class EngineFactory
    {
        public static IDebugSession CreateDebugSession(string adapterID, DebugProtocolCallbacks protocolCallbacks)
        {
            EngineConfiguration config = EngineConfiguration.TryGet(adapterID);
            if (config != null)
            {
                return new AD7DebugSession(protocolCallbacks, config);
            }
            return null;
        }
    }
}
