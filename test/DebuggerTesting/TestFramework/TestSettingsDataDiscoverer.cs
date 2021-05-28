// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.Abstractions;
using Xunit.Sdk;

namespace DebuggerTesting.TestFramework
{
    /// <summary>
    /// Custom data discovered for xUnit theory tests that overrides the default behavior of data discovery.
    /// </summary>
    public class TestSettingsDataDiscoverer :
        DataDiscoverer
    {
        #region Methods

        public override bool SupportsDiscoveryEnumeration(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            // Return false to force xUnit to enumerate the test data during the execution phase of the test
            // rather than during the discovery phase. This is required because test data that is enuemrated
            // during the discovery phase must be serializable, but xUnit cannot serialize arbitrary complex
            // objects such as the ITestSettings implementation.
            return false;
        }

        #endregion
    }
}