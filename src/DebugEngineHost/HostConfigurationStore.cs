// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

        /// <summary>
        /// Checks if logging is enabled, and if so returns a logger object.
        /// </summary>
        /// <param name="enableLoggingSettingName">[Optional] In VS, the name of the settings key to check if logging is enabled. If not specified, this will check 'Logging' in the AD7 Metrics.</param>
        /// <param name="logFileName">[Required] name of the log file to open if logging is enabled.</param>
        /// <returns>[Optional] If logging is enabled, the logging object.</returns>
        public HostLogger GetLogger(string enableLoggingSettingName, string logFileName)
        {
            if (string.IsNullOrEmpty(logFileName))
            {
                throw new ArgumentNullException("logFileName");
            }

            object enableLoggingValue;
            if (!string.IsNullOrEmpty(enableLoggingSettingName))
            {
                enableLoggingValue = GetOptionalValue("Debugger", enableLoggingSettingName);
            }
            else
            {
                enableLoggingValue = GetEngineMetric("EnableLogging");
            }

            if (enableLoggingValue == null ||
                !(enableLoggingValue is int) ||
                ((int)enableLoggingValue) == 0)
            {
                return null;
            }

            string tempDirectory = Path.GetTempPath();
            if (!string.IsNullOrEmpty(tempDirectory) && Directory.Exists(tempDirectory))
            {
                string filePath = Path.Combine(tempDirectory, logFileName);

                try
                {
                    FileStream stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    return new HostLogger(new StreamWriter(stream));
                }
                catch (IOException)
                {
                    // ignore failures from the log being in use by another process
                }
            }

            return null;
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
