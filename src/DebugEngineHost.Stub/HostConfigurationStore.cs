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

        public object GetOptionalValue(string section, string valueName)
        {
            throw new NotImplementedException();
        }
    }
}
