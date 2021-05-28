// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DebuggerTesting.Settings;
using DebuggerTesting.Utilities;
using Xunit.Sdk;

namespace DebuggerTesting
{
    /// <summary>
    /// Attribute used for providing an xUnit theory test with test setting data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [DataDiscoverer("DebuggerTesting.TestFramework.TestSettingsDataDiscoverer", "DebuggerTesting")]
    public class RequiresTestSettingsAttribute :
        DataAttribute
    {
        #region Constructor

        static RequiresTestSettingsAttribute()
        {
            if (PlatformUtilities.IsLinux)
                s_platform = SupportedPlatform.Linux;
            else if (PlatformUtilities.IsOSX)
                s_platform = SupportedPlatform.MacOS;
            else if (PlatformUtilities.IsWindows)
                s_platform = SupportedPlatform.Windows;
            else
                throw new PlatformNotSupportedException();

            s_platformArchitecture = PlatformUtilities.Is64Bit ? SupportedArchitecture.x64 : SupportedArchitecture.x86;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Retrieves the parameters that will be passed into the test method.
        /// </summary>
        /// <param name="testMethod">The method information of the test method.</param>
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            return TestSettingsHelper.GetSettings(testMethod, s_platform, s_platformArchitecture)
                .Select(settings => new object[] { settings });
        }

        #endregion

        #region Fields

        private static SupportedPlatform s_platform;
        private static SupportedArchitecture s_platformArchitecture;

        #endregion
    }
}
