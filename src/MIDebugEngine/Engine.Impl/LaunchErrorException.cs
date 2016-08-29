// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.MIDebugEngine
{
    internal class LaunchErrorException : Exception
    {
        public LaunchErrorException(string message) : base(message)
        {
        }
    }
}