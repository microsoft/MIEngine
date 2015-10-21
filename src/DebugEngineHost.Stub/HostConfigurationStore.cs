// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DebugEngineHost
{
    public sealed class HostConfigurationStore
    {
        public HostConfigurationStore(string registryRoot, string engineId)
        {
            throw new NotImplementedException();
        }

        public string RegistryRoot
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public object GetEngineMetric(string metric)
        {
            throw new NotImplementedException();
        }

        public void GetExceptionCategorySettings(Guid categoryId, out HostConfigurationSection categoryConfigSection, out string categoryName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks if logging is enabled, and if so returns a logger object. 
        /// 
        /// In VS, this is wired up to read from the registry and return a logger which writes a log file to %TMP%\log-file-name.
        /// In VS Code, this will check if the '--engineLogging' switch is enabled, and if so return a logger that wil write to the Console.
        /// </summary>
        /// <param name="enableLoggingSettingName">[Optional] In VS, the name of the settings key to check if logging is enabled. If not specified, this will check 'EnableLogging' in the AD7 Metrics.</param>
        /// <param name="logFileName">[Required] name of the log file to open if logging is enabled.</param>
        /// <returns>[Optional] If logging is enabled, the logging object.</returns>
        public HostLogger GetLogger(string enableLoggingSettingName, string logFileName)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class HostLogger
    {
        private HostLogger()
        {
            throw new NotImplementedException();
        }

        public void WriteLine(string line)
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }
    }
}
