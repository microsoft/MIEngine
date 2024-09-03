// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace DebuggerTesting.OpenDebug
{
    /// <summary>
    /// Provides a serializable exception for errors that occur when running
    /// the debug adapter runner.
    /// </summary>

#if !CORECLR
    [Serializable]
#endif
    public class RunnerException : Exception
    {
        // Required for serialization
        public RunnerException() { }

        // Required for serialization
        public RunnerException(string message)
            : base(message) { }

        public RunnerException(string messageFormat, params object[] messageArgs)
            : base(string.Format(CultureInfo.CurrentCulture, messageFormat, messageArgs)) { }

        public RunnerException(string message, Exception innerException)
            : base(message, innerException) { }

        public RunnerException(Exception innerException, string messageFormat, params object[] messageArgs)
            : base(string.Format(CultureInfo.CurrentCulture, messageFormat, messageArgs), innerException) { }
    }
}
