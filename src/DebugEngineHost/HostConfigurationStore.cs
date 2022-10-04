﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{
    public sealed class HostConfigurationStore
    {
        private const string DebuggerSectionName = "Debugger";
        private const string LaunchersSectionName = "MILaunchers";

        private string _engineId;
        private string _registryRoot;
        private RegistryKey _configKey;

        public HostConfigurationStore(string registryRoot)
        {
            if (string.IsNullOrEmpty(registryRoot))
                throw new ArgumentNullException(nameof(registryRoot));

            _registryRoot = registryRoot;
            _configKey = Registry.LocalMachine.OpenSubKey(registryRoot);
            if (_configKey == null)
            {
                throw new HostConfigurationException(registryRoot);
            }
        }

        /// <summary>
        /// Sets the Guid of the engine being hosted. This should only be set once for each HostConfigurationStore instance.
        /// </summary>
        /// <param name="value">The new engine GUID to set</param>
        public void SetEngineGuid(Guid value)
        {
            if (_engineId != null)
            {
                throw new InvalidOperationException();
            }

            _engineId = value.ToString("B", CultureInfo.InvariantCulture);
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
            if (_engineId == null)
            {
                throw new InvalidOperationException();
            }

            return GetOptionalValue(@"AD7Metrics\Engine\" + _engineId, metric);
        }

        public void GetExceptionCategorySettings(Guid categoryId, out HostConfigurationSection categoryConfigSection, out string categoryName)
        {
            string subKeyName = @"AD7Metrics\Exception\" + categoryId.ToString("B", CultureInfo.InvariantCulture);
            RegistryKey categoryKey = _configKey.OpenSubKey(subKeyName);
            if (categoryKey == null)
            {
                throw new HostConfigurationException("$RegRoot$\\" + subKeyName);
            }

            categoryConfigSection = new HostConfigurationSection(categoryKey);
            categoryName = categoryKey.GetSubKeyNames().Single();
        }

        public T GetDebuggerConfigurationSetting<T>(string settingName, T defaultValue)
        {
            return GetDebuggerConfigurationSetting(DebuggerSectionName, settingName, defaultValue);
        }

        public object GetCustomLauncher(string launcherTypeName)
        {
            string guidstr = GetDebuggerConfigurationSetting(LaunchersSectionName, launcherTypeName, Guid.Empty.ToString());
            Guid clsidLauncher = new Guid(guidstr);
            if (clsidLauncher == Guid.Empty)
            {
                return null;
            }
            return HostLoader.VsCoCreateManagedObject(this, clsidLauncher);
        }

        private T GetDebuggerConfigurationSetting<T>(string sectionName, string settingName, T defaultValue)
        {
            object valueObj = GetOptionalValue(sectionName, settingName);
            if (valueObj == null)
            {
                return defaultValue;
            }

            T result;
            try
            {
                result = (T)valueObj;
            }
            catch (InvalidCastException)
            {
                Debug.Fail(string.Format(CultureInfo.CurrentCulture, "Failed to convert {0} to {1}", valueObj, typeof(T).Name));
                result = defaultValue;
            }

            return result;
        }

        private object GetOptionalValue(string section, string valueName)
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
