// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DebugAdapterRunner;
using DebuggerTesting;
using DebuggerTesting.OpenDebug;
using DebuggerTesting.OpenDebug.CrossPlatCpp;
using System;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace CppTests
{
    public abstract class TestBase : ILoggingComponent
    {
        #region ILoggingComponent Members

        public ITestOutputHelper OutputHelper { get; }

        #endregion

        protected TestBase(ITestOutputHelper outputHelper)
        {
            this.OutputHelper = outputHelper;
        }

        protected IDebuggerRunner CreateDebugAdapterRunner(ITestSettings settings)
        {
            return DebuggerRunner.Create(this, settings, GetCallbackHandlers());
        }

        protected virtual IEnumerable<Tuple<string, CallbackRequestHandler>> GetCallbackHandlers()
        {
            return null;
        }
    }
}
