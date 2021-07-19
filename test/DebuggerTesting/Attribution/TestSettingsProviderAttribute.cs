// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using DebuggerTesting.TestFramework;

namespace DebuggerTesting.Attribution
{
    /// <summary>
    /// Attribute used to specify which class is used from providing test settings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class TestSettingsProviderAttribute :
        Attribute
    {
        #region Constructor

        public TestSettingsProviderAttribute(Type providerType)
        {
            Parameter.AssertIfNotOfType<ITestSettingsProvider>(providerType, nameof(providerType));

            this.ProviderType = providerType;
        }

        #endregion

        #region Properties

        public Type ProviderType { get; private set; }

        #endregion
    }
}