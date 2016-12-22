// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.SSHDebugPS
{
    internal class CommandFailedException : Exception
    {
        public CommandFailedException(string message) : base(message)
        {
            // We don't currently have a good way to return a meaningful error
            this.HResult = HR.E_FAIL;
        }
    }
}