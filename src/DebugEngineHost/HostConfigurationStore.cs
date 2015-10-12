// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{
    public sealed class HostConfigurationStore
    {
        private string _engineId;
        private string _registryRoot;
        private RegistryKey _configKey;

        public HostConfigurationStore(string registryRoot, string engineId)
        {
            if (string.IsNullOrEmpty(registryRoot))
                throw new ArgumentNullException("registryRoot");
            if (string.IsNullOrEmpty("engineId"))
                throw new ArgumentNullException("engineId");

            _registryRoot = registryRoot;
            _engineId = engineId;
            _configKey = Registry.LocalMachine.OpenSubKey(registryRoot);
            if (_configKey == null)
            {
                throw new HostConfirigurationException(registryRoot);
            }
        }

        // TODO: This should be removed. It is here only to make the Android launcher work for now
        public string RegistryRoot
        {
            get
            {
                return _registryRoot;
            }
        }

        public object GetEngineMetric(string metric)
        {
            return GetOptionalValue(@"AD7Metrics\Engine\" + _engineId.ToUpper(CultureInfo.InvariantCulture), metric);
        }

        public void GetExceptionCategorySettings(Guid categoryId, out HostConfigurationSection categoryConfigSection, out string categoryName)
        {
            string subKeyName = @"AD7Metrics\Exception\" + categoryId.ToString("B", CultureInfo.InvariantCulture);
            RegistryKey categoryKey = _configKey.OpenSubKey(subKeyName);
            if (categoryKey == null)
            {
                throw new HostConfirigurationException("$RegRoot$\\" + subKeyName);
            }

            categoryConfigSection = new HostConfigurationSection(categoryKey);
            categoryName = categoryKey.GetSubKeyNames().Single();
        }

        public object GetOptionalValue(string section, string valueName)
        {
            using (RegistryKey key = _configKey.OpenSubKey(section))
            {
                if (key == null)
                {
                    return null;
                }

                return key.GetValue(valueName);
            }
        }
    }
}
