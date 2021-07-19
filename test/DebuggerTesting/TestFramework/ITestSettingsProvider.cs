// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Reflection;

namespace DebuggerTesting.TestFramework
{
    /// <summary>
    /// Interface use for providing test settings based on reflection of a test method.
    /// </summary>
    public interface ITestSettingsProvider
    {
        #region Methods

        /// <summary>
        /// Gets the test settings associated with the test method.
        /// </summary>
        IEnumerable<ITestSettings> GetSettings(MethodInfo testMethod);

        #endregion
    }
}