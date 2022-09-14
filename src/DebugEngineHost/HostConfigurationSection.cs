// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{
    public sealed class HostConfigurationSection : IDisposable
    {
        private readonly RegistryKey _key;

        internal HostConfigurationSection(RegistryKey key)
        {
            _key = key;
        }

        public void Dispose()
        {
            _key.Close();
        }

        /// <summary>
        /// Obtains the value of the specified valueName
        /// </summary>
        /// <param name="valueName">Name of the value to obtain</param>
        /// <returns>[Optional] null if the value doesn't exist, otherwise the value</returns>
        public object GetValue(string valueName)
        {
            return _key.GetValue(valueName);
        }

        /// <summary>
        /// Enumerates the names of all the values defined in this section
        /// </summary>
        /// <returns>Enumerator of strings</returns>
        public IEnumerable<string> GetValueNames()
        {
            return _key.GetValueNames();
        }

        public IntPtr YOLOHandle => _key.Handle.DangerousGetHandle();
    }
}
