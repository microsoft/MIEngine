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

        private readonly IMessageSink _diagnosticMessageSink;

        // xUnit will pick the constructor with the most parameters it can satisfy
        // and inject the diagnostic message sink when available. The parameterless
        // constructor is retained so the orderer still works in hosts that don't
        // supply a sink.
        public DependencyTestOrderer()
        {
        }

        public DependencyTestOrderer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
            where TTestCase : ITestCase
        {
            return OrderBasedOnDependencies(testCases.Cast<ITestCase>()).Cast<TTestCase>();
        }

        protected override void LogDiagnostic(string message)
        {
            // Route through xUnit's diagnostic message sink so the message shows up
            // in `dotnet test` output (with `<DiagnosticMessages>true</DiagnosticMessages>`
            // in xunit.runner.json or `-diagnostics` on the runner) and in the VS
            // test output pane. Fall back to Console.Error so the message isn't lost
            // when no sink is available (e.g. unit-testing the orderer directly).
            if (_diagnosticMessageSink != null)
            {
                _diagnosticMessageSink.OnMessage(new DiagnosticMessage(message));
            }
            else
            {
                Console.Error.WriteLine(message);
            }
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
