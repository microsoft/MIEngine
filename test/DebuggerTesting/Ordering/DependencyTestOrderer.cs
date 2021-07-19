// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace DebuggerTesting.Ordering
{
    public class DependencyTestOrderer : DependencyOrderer<ITestCase, string>, ITestCaseOrderer
    {
        // These are used in attributes, so they must be constant.
        public const string TypeName = nameof(DebuggerTesting) + "." + nameof(Ordering) + "." + nameof(DependencyTestOrderer);
        public const string AssemblyName = nameof(DebuggerTesting);

        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
            where TTestCase : ITestCase
        {
            return OrderBasedOnDependencies(testCases.Cast<ITestCase>()).Cast<TTestCase>();
        }

        #region Dependency Helpers

        protected override int GetIndexOfDependency(IList<ITestCase> tests, string testName)
        {
            for (int i = tests.Count - 1; i >= 0; i--)
            {
                string currentTestName = tests[i].TestMethod.Method.Name;
                if (string.Equals(currentTestName, testName, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        protected override IEnumerable<string> GetDependencies(ITestCase testCase)
        {
            IMethodInfo testMethodInfo = testCase.TestMethod.Method;
            if (testMethodInfo == null)
                return null;
            IEnumerable<IAttributeInfo> attributes = testMethodInfo.GetCustomAttributes(typeof(DependsOnTestAttribute));
            return attributes.Select(x => x.GetConstructorArguments().OfType<string>().First());
        }

        #endregion

        protected override string GetItemName(ITestCase item)
        {
            return item.DisplayName;
        }
    }
}
