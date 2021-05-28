// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebuggerTesting.Ordering
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class DependsOnTestAttribute : Attribute
    {
        public DependsOnTestAttribute(string testName)
        {
            Parameter.ThrowIfNull(testName, nameof(testName));
            this.TestName = testName;
        }

        public string TestName { get; set; }
    }
}
