// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Interop;

namespace OpenDebugAD7
{
    internal class AD7Utils
    {
        public static bool IsAnnotatedFrame(ref FRAMEINFO frameInfo)
        {
            enum_FRAMEINFO_FLAGS_VALUES flags = unchecked((enum_FRAMEINFO_FLAGS_VALUES)frameInfo.m_dwFlags);
            return flags.HasFlag(enum_FRAMEINFO_FLAGS_VALUES.FIFV_ANNOTATEDFRAME);
        }
    }
}
