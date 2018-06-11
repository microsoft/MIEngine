// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DebugEngineHost.VSCode;
using System;
using System.Collections.Generic;

namespace Microsoft.DebugEngineHost
{
    public class HostConfigurationSection : IDisposable
    {
        private readonly IReadOnlyDictionary<string, ExceptionSettings.TriggerState> _defaultTriggers;

        internal HostConfigurationSection(IReadOnlyDictionary<string, ExceptionSettings.TriggerState> defaultTriggers)
        {
            _defaultTriggers = defaultTriggers;
        }

        public void Dispose() { }

        public object GetValue(string valueName)
        {
            ExceptionSettings.TriggerState state;
            if (_defaultTriggers.TryGetValue(valueName, out state))
            {
                return (int)state;
            }

            return null;
        }

        public IEnumerable<string> GetValueNames()
        {
            return _defaultTriggers.Keys;
        }
    }
}