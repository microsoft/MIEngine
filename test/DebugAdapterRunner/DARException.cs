// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace DebugAdapterRunner
{
    public class DARException : Exception
    {
        public DARException(string message) : base(message)
        {
        }
    }
}
