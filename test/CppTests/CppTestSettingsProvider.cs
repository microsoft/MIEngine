// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DebuggerTesting;
using DebuggerTesting.Settings;
using DebuggerTesting.TestFramework;

namespace CppTests
{
    public sealed class CppTestSettingsProvider :
        ITestSettingsProvider
    {
        #region ITestSettingsProvider Member

        IEnumerable<ITestSettings> ITestSettingsProvider.GetSettings(MethodInfo testMethod)
        {
            return this.settingsLazy.Value;
        }

        #endregion

        #region Methods

        private static IEnumerable<ITestSettings> GetSettings()
        {
            return TestSettingsHelper.LoadSettingsFromConfig(PathSettings.TestConfigurationFilePath);
        }

        #endregion

        #region Fields

        private Lazy<IEnumerable<ITestSettings>> settingsLazy = new Lazy<IEnumerable<ITestSettings>>(
            CppTestSettingsProvider.GetSettings,
            LazyThreadSafetyMode.ExecutionAndPublication);

        #endregion
    }
}