// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using DebuggerTesting.Attribution;
using DebuggerTesting.TestFramework;
using DebuggerTesting.Utilities;
using Xunit;

namespace DebuggerTesting
{
    public static class TestSettingsHelper
    {
        #region Methods

        internal static IEnumerable<ITestSettings> FilterSettings(this IEnumerable<ITestSettings> settings, MethodInfo methodInfo, SupportedPlatform platform, SupportedArchitecture platformArchitecture)
        {
            // Makre sure that the test runs for the platform and platform architecture
            SupportedPlatformAttribute platformAttribute = methodInfo.GetCustomAttribute<SupportedPlatformAttribute>();
            // If platform attribute is not specified, implicitly assume that all platforms are supported
            if (null != platformAttribute)
            {
                if (!platformAttribute.Platform.HasFlag(platform))
                    return Enumerable.Empty<ITestSettings>();
                if (!platformAttribute.Architecture.HasFlag(platformArchitecture))
                    return Enumerable.Empty<ITestSettings>();
            }

            IReadOnlyCollection<SupportedCompilerAttribute> supportedCompilers = methodInfo.GetCustomAttributes<SupportedCompilerAttribute>().ToArray();
            IReadOnlyCollection<SupportedDebuggerAttribute> supportedDebuggers = methodInfo.GetCustomAttributes<SupportedDebuggerAttribute>().ToArray();
            IReadOnlyCollection<UnsupportedDebuggerAttribute> unsupportedDebuggers = methodInfo.GetCustomAttributes<UnsupportedDebuggerAttribute>().ToArray();

            // Get the subset of test settings that match the requirements of the test.
            // The test will be run for each test setting in the set. If an empty set is returned,
            // the test is not run.
            return settings.Where(s =>
                (supportedCompilers.Count == 0 || supportedCompilers.Matches(s.CompilerSettings)) &&
                (supportedDebuggers.Count == 0 || supportedDebuggers.Matches(s.DebuggerSettings)) &&
                !unsupportedDebuggers.Matches(s.DebuggerSettings))
                .Select(x => TestSettings.CloneWithName(x, methodInfo.Name))
                .ToArray();
        }

        private static SupportedArchitecture GetArchitecture(this XElement element, string attributeName)
        {
            return element.GetAttributeEnum<SupportedArchitecture>(attributeName);
        }

        private static SupportedCompiler GetCompiler(this XElement element, string attributeName)
        {
            return element.GetAttributeEnum<SupportedCompiler>(attributeName);
        }

        private static SupportedDebugger GetDebugger(this XElement element, string attributeName)
        {
            return element.GetAttributeEnum<SupportedDebugger>(attributeName);
        }

        internal static IEnumerable<ITestSettings> GetSettings(MethodInfo testMethod, SupportedPlatform platform, SupportedArchitecture platformArchitecture)
        {
            Parameter.AssertIfNull(testMethod, nameof(testMethod));

            // Find the attribute in the context of the test method.
            // Currently, only look at the assembly of the test method.
            TestSettingsProviderAttribute providerAttribute = testMethod.DeclaringType?.GetTypeInfo().Assembly?.GetCustomAttribute<TestSettingsProviderAttribute>();
            UDebug.AssertNotNull(providerAttribute, "Unable to locate TestSettingsProviderAttribute.");
            if (null == providerAttribute)
                return Enumerable.Empty<ITestSettings>();

            UDebug.AssertNotNull(providerAttribute.ProviderType, "TestSettingsProviderAttribute.ProviderType is null");
            if (null == providerAttribute.ProviderType)
                return Enumerable.Empty<ITestSettings>();

            ITestSettingsProvider provider = null;
            lock (s_providerMapping)
            {
                // Get the provider from the attribute if the provider was not already created
                if (!s_providerMapping.TryGetValue(providerAttribute.ProviderType, out provider))
                {
                    // If not cached, create the provide from the attribute and cache it
                    provider = (ITestSettingsProvider)Activator.CreateInstance(providerAttribute.ProviderType);
                    s_providerMapping.Add(providerAttribute.ProviderType, provider);
                }
            }

            // Get the list of settings from the provider
            IEnumerable<ITestSettings> settings = provider.GetSettings(testMethod);
            UDebug.AssertNotNull(settings, "Settings were not provided by the test settings provider.");
            if (null == settings)
                return Enumerable.Empty<ITestSettings>();

            // Filter the settings according to the attribution on the test method
            return settings.FilterSettings(testMethod, platform, platformArchitecture);
        }

        public static IEnumerable<ITestSettings> LoadSettingsFromConfig(string configPath)
        {
            Assert.True(File.Exists(configPath), "Test configuration should exist at '{0}'".FormatInvariantWithArgs(configPath));

            using (XmlReader machineReader = XmlHelper.CreateXmlReader(configPath))
            {
                XDocument machineConfigDoc = null;
                try
                {
                    machineConfigDoc = XDocument.Load(machineReader);
                }
                catch (XmlException) { }
                Assert.True(null != machineConfigDoc, "Object loaded from '{0}' is not a TestMachineConfiguration. Invalid Xml.".FormatInvariantWithArgs(configPath));

                XElement machineConfig = machineConfigDoc.Element("TestMachineConfiguration");
                Assert.True(null != machineConfig, "Object loaded from '{0}' is not a TestMachineConfiguration. Missing TestMachineConfiguration.".FormatInvariantWithArgs(configPath));

                // Get the list of compilers
                var compilers = machineConfig
                    .Element("Compilers")
                    .Elements("Compiler")
                    .ToDictionary(
                        x => x.GetAttributeValue("Name"),
                        x => new
                        {
                            Name = x.GetAttributeValue("Name"),
                            Type = x.GetCompiler("Type"),
                            Path = x.GetAttributeValue("Path"),
                            Properties = x.GetPropertiesDictionary()
                        });
                Assert.True(null != compilers && compilers.Count != 0, "Object loaded from '{0}' is not a TestMachineConfiguration. Missing Compilers.".FormatInvariantWithArgs(configPath));

                // Get the list of debuggers
                var debuggers = machineConfig
                    .Element("Debuggers")
                    .Elements("Debugger")
                    .ToDictionary(
                        x => x.GetAttributeValue("Name"),
                        x => new
                        {
                            Name = x.GetAttributeValue("Name"),
                            Type = x.GetDebugger("Type"),
                            Path = x.GetAttributeValue("Path"),
                            AdapterPath = x.GetAttributeValue("AdapterPath"),
                            MIMode = x.GetAttributeValue("MIMode"),
                            Properties = x.GetPropertiesDictionary()
                        });
                Assert.True(null != debuggers && debuggers.Count != 0, "Object loaded from '{0}' is not a TestMachineConfiguration. Missing Debuggers.".FormatInvariantWithArgs(configPath));

                // Get the list of test configurations
                var testConfigurations = machineConfig
                    .Element("TestConfigurations")
                    .Elements("TestConfiguration")
                    .Select(x => new
                    {
                        DebuggeeArchitecture = x.GetArchitecture("DebuggeeArchitecture"),
                        CompilerName = x.GetAttributeValue("Compiler"),
                        DebuggerName = x.GetAttributeValue("Debugger")
                    });
                Assert.True(null != testConfigurations && testConfigurations.Count() != 0, "Object loaded from '{0}' is not a TestMachineConfiguration. Missing TestConfigurations.".FormatInvariantWithArgs(configPath));

                // Create a TestSettings for each combination of architectures, compilers, and debuggers
                var testSettings =
                    from testConfiguration in testConfigurations
                    where compilers.ContainsKey(testConfiguration.CompilerName)
                    where debuggers.ContainsKey(testConfiguration.DebuggerName)
                    select new
                    {
                        DebuggeeArchitecture = testConfiguration.DebuggeeArchitecture,
                        Compiler = compilers[testConfiguration.CompilerName],
                        Debugger = debuggers[testConfiguration.DebuggerName]
                    }
                    into config
                    select new TestSettings(
                        config.DebuggeeArchitecture,
                        config.Compiler.Name,
                        config.Compiler.Type,
                        SafeExpandEnvironmentVariables(config.Compiler.Path),
                        config.Compiler.Properties,
                        config.Debugger.Name,
                        config.Debugger.Type,
                        SafeExpandEnvironmentVariables(config.Debugger.Path),
                        SafeExpandEnvironmentVariables(config.Debugger.AdapterPath),
                        config.Debugger.MIMode,
                        config.Debugger.Properties);

                // Force evaluation
                return testSettings.ToArray();
            }
        }

        /// <summary>
        /// Reads a property bag in the XML and turns it to a dictionary.
        /// </summary>
        private static IDictionary<string, string> GetPropertiesDictionary(this XElement element)
        {
            return element
                .Element("Properties")
                ?.Elements("Property")
                ?.ToDictionary(p => p.GetAttributeValue("Name"), p => SafeExpandEnvironmentVariables(p.Value), StringComparer.Ordinal);
        }

        private static bool Matches(this IEnumerable<SupportedCompilerAttribute> compilers, ICompilerSettings settings)
        {
            return compilers.Any(ca => ca.Compiler.HasFlag(settings.CompilerType) && ca.Architecture.HasFlag(settings.DebuggeeArchitecture));
        }

        private static bool Matches(this IEnumerable<DebuggerAttribute> debuggers, IDebuggerSettings settings)
        {
            return debuggers.Any(da => da.Debugger.HasFlag(settings.DebuggerType) && da.Architecture.HasFlag(settings.DebuggeeArchitecture));
        }

        private static string SafeExpandEnvironmentVariables(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                value = Environment.ExpandEnvironmentVariables(value);
            }
            return value;
        }

        #endregion

        #region Fields

        private static IDictionary<Type, ITestSettingsProvider> s_providerMapping =
            new Dictionary<Type, ITestSettingsProvider>();

        #endregion
    }
}