// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DebugEngineHost.VSCode;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.DebugEngineHost
{
    public sealed class HostConfigurationStore
    {
        private readonly EngineConfiguration _config;

        public HostConfigurationStore(string adapterId)
        {
            _config = EngineConfiguration.TryGet(adapterId);
            if (_config == null)
            {
                throw new ArgumentOutOfRangeException("adapterId");
            }
        }

        public void SetEngineGuid(Guid value)
        {
            // nothing to do
        }

        public string RegistryRoot
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public object GetCustomLauncher(string launcherTypeName)
        {
            throw new NotImplementedException();
        }

        public object GetEngineMetric(string metric)
        {
            if (string.CompareOrdinal("GlobalVisualizersDirectory", metric) == 0)
            {
                string openDebugPath = EngineConfiguration.GetAdapterDirectory();

                return Path.Combine(openDebugPath, "Visualizers");
            }

            return null;
        }

        public void GetExceptionCategorySettings(Guid categoryId, out HostConfigurationSection categoryConfigSection, out string categoryName)
        {
            var category = _config.ExceptionSettings.Categories.FirstOrDefault((x) => x.Id == categoryId);
            if (category == null)
            {
                throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, HostResources.Error_ExceptionCategoryMissing, categoryId));
            }

            categoryName = category.Name;
            categoryConfigSection = new HostConfigurationSection(category.DefaultTriggers);
        }

        /// <summary>
        /// Checks if logging is enabled, and if so returns a logger object. 
        /// 
        /// In VS, this is wired up to read from the registry and return a logger which writes a log file to %TMP%\log-file-name.
        /// In VS Code, this will check if the '--engineLogging' switch is enabled, and if so return a logger that wil write to the logger output.
        /// </summary>
        /// <param name="enableLoggingSettingName">[Optional] In VS, the name of the settings key to check if logging is enabled. If not specified, this will check 'EnableLogging' in the AD7 Metrics.</param>
        /// <param name="logFileName">[Required] name of the log file to open if logging is enabled. This is ignored for VSCode.</param>
        /// <returns>[Optional] If logging is enabled, the logging object.</returns>
        public HostLogger GetLogger(string enableLoggingSettingName, string logFileName)
        {
            return HostLogger.Instance;
        }

        /// <summary>
        /// Read the debugger setting
        /// </summary>
        public T GetDebuggerConfigurationSetting<T>(string settingName, T defaultValue)
        {
            // TODO: check the configuration store for these?
            return defaultValue;
        }
    }
}
