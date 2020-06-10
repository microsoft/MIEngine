// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Threading;

// Disable: 'Field 'EngineOptions.EngineClass' is never assigned to' since these fields are assigned by reflection
#pragma warning disable 649

namespace Microsoft.DebugEngineHost.VSCode
{
    public sealed class EngineConfiguration
    {
        private static string s_adapterDirectory;
        private static readonly Dictionary<string, EngineConfiguration> s_dict = new Dictionary<string, EngineConfiguration>();

        public string AdapterId { get; private set; }

        private bool _isReadOnly;
        private string _assemblyName;
        private string _engineClass;
        private readonly ExceptionSettings _exceptionSettings = new ExceptionSettings();
        private bool _conditionalBP;
        private bool _functionBP;
        private bool _clipboardContext;

        // NOTE: CoreCLR doesn't support providing a code base when loading assemblies. So all debug engines
        // must be placed in the directory of OpenDebugAD7.exe
        [JsonRequired]
        public string EngineAssemblyName
        {
            get { return _assemblyName; }
            set { SetProperty(out _assemblyName, value); }
        }


        [JsonRequired]
        public string EngineClassName
        {
            get { return _engineClass; }
            set { SetProperty(out _engineClass, value); }
        }

        public ExceptionSettings ExceptionSettings
        {
            get { return _exceptionSettings; }
        }

        public bool ConditionalBP
        {
            get { return _conditionalBP; }
            set { SetProperty(out _conditionalBP, value); }
        }

        public bool FunctionBP
        {
            get { return _functionBP; }
            set { SetProperty(out _functionBP, value); }
        }

        public bool ClipboardContext { 
            get { return _clipboardContext; } 
            set { SetProperty(out _clipboardContext, value); }
        }


        /// <summary>
        /// Provides the directory of the debug adapter. This is the directory where
        /// configuration files are read from.
        /// </summary>
        /// <returns>Path to the directory</returns>
        public static string GetAdapterDirectory()
        {
            if (s_adapterDirectory == null)
            {
                // Configuration goes in the directory of this assembly
                string thisModulePath = typeof(EngineConfiguration).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName;
                Interlocked.CompareExchange(ref s_adapterDirectory, Path.GetDirectoryName(thisModulePath), null);
            }

            return s_adapterDirectory;
        }

        /// <summary>
        /// Overrides the default adapter directory with a different directory. This
        /// must be called immediately on startup. It is wired up to a command line
        /// option in OpenDebugAD7.
        /// </summary>
        /// <param name="adapterDirectory">The directory to use.</param>
        public static void SetAdapterDirectory(string adapterDirectory)
        {
            if (adapterDirectory == null)
            {
                throw new ArgumentNullException("adapterDirectory");
            }

            if (Interlocked.CompareExchange(ref s_adapterDirectory, adapterDirectory, null) != null)
            {
                throw new InvalidOperationException("AdapterDirectory cannot be set after it has been inialized");
            }
        }

        public static EngineConfiguration TryGet(string adapterId)
        {
            lock (s_dict)
            {
                EngineConfiguration result;
                if (s_dict.TryGetValue(adapterId, out result))
                {
                    return result;
                }

                string engineConfigPath = Path.Combine(GetAdapterDirectory(), adapterId + ".ad7Engine.json");
                string engineConfigText = File.ReadAllText(engineConfigPath);
                result = JsonConvert.DeserializeObject<EngineConfiguration>(engineConfigText);
                result.AdapterId = adapterId;
                result.ExceptionSettings.MakeReadOnly();
                result._isReadOnly = true;
                s_dict.Add(adapterId, result);

                return result;
            }
        }

        /// <summary>
        /// Loads the engine assembly + class
        /// </summary>
        public object LoadEngine()
        {
            AssemblyName assemblyName = new System.Reflection.AssemblyName(this.EngineAssemblyName);
            Assembly engineAssembly = Assembly.Load(assemblyName);
            Type engineClass = engineAssembly.GetType(this.EngineClassName);
            if (engineClass == null)
            {
                throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, HostResources.Error_ClassNotFound, this.EngineClassName, this.EngineAssemblyName));
            }

            object instance = Activator.CreateInstance(engineClass);

            if (instance == null)
            {
                throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, HostResources.Error_ConstructorNotFound, this.EngineClassName, this.EngineAssemblyName));
            }

            return instance;
        }

        private void SetProperty(out string propertyStore, string newValue)
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException();
            }

            propertyStore = newValue;
        }

        private void SetProperty(out bool propertyStore, bool newValue)
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException();
            }

            propertyStore = newValue;
        }
    }
}
