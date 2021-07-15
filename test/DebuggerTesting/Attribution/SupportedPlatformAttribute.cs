// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting
{
    /// <summary>
    /// Attribute used by xUnit theory test for specifying which platforms and platform architectures
    /// are required by the test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class SupportedPlatformAttribute :
        Attribute
    {
        #region Constructor

        public SupportedPlatformAttribute(SupportedPlatform platform, SupportedArchitecture architecture)
        {
            this.Architecture = architecture;
            this.Platform = platform;
        }

        #endregion

        #region Properties

        public SupportedArchitecture Architecture { get; private set; }

        public SupportedPlatform Platform { get; private set; }

        #endregion
    }
}