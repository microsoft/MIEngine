// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Provides support for loading dependant assemblies using information from the configuration store.
    /// </summary>
    public static class HostLoader
    {
        /// <summary>
        /// Looks up the specified CLSID in the VS registry and loads it
        /// </summary>
        /// <param name="configStore">Registry root to lookup the type</param>
        /// <param name="clsid">CLSID to CoCreate</param>
        /// <returns>[Optional] loaded object. Null if the type is not registered, or points to a type that doesn't exist</returns>
        public static object VsCoCreateManagedObject(HostConfigurationStore configStore, Guid clsid)
        {
            string assemblyNameString, className, codeBase;
            if (!GetManagedTypeInfoForCLSID(configStore, clsid, out assemblyNameString, out className, out codeBase))
            {
                return null;
            }

            if (codeBase != null && !File.Exists(codeBase))
            {
                return null;
            }

            AssemblyName assemblyName = new AssemblyName(assemblyNameString);
            if (codeBase != null)
            {
                assemblyName.CodeBase = "file:///" + codeBase;
            }

            Assembly assemblyObject = Assembly.Load(assemblyName);
            return assemblyObject.CreateInstance(className);
        }

        private static bool GetManagedTypeInfoForCLSID(HostConfigurationStore configStore, Guid clsid, out string assembly, out string className, out string codeBase)
        {
            assembly = null;
            className = null;
            codeBase = null;

            string keyPath = configStore.RegistryRoot + @"\CLSID\" + clsid.ToString("B");
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
            {
                if (key == null)
                    return false;

                object oAssembly = key.GetValue("Assembly");
                object oClassName = key.GetValue("Class");
                object oCodeBase = key.GetValue("CodeBase");

                if (oAssembly == null || !(oAssembly is string))
                    return false;
                if (oClassName == null || !(oClassName is string))
                    return false;

                // CodeBase is not required, but it is an error if it isn't a string
                if (oCodeBase != null && !(oCodeBase is string))
                    return false;

                assembly = (string)oAssembly;
                className = (string)oClassName;
                codeBase = (string)oCodeBase;

                return true;
            }
        }
    }
}
