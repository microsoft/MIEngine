// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDebugAD7
{
    internal static class AD7ExtensionMethods
    {
        public static int Id(this IDebugThread2 thread)
        {
            uint unsignedId;
            thread.GetThreadId(out unsignedId);
            return (unchecked((int)unsignedId));
        }
    }
}
