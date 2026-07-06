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

        public static string GetMemoryReferenceFromIDebugProperty(IDebugProperty2 property)
        {
            if (property == null)
            {
                return null;
            }

            // Only a pointer or an array holds an address a memoryReference can point at.
            // For any other type the value is not an address, so a memoryReference would
            // make the client show a memory view that navigates to the value itself. The
            // engine's GetMemoryContext still resolves an address for any type, which the
            // Visual Studio Memory and Disassembly windows rely on; the restriction here
            // applies only to the reference reported over DAP.
            if (!IsPointerOrArray(property))
            {
                return null;
            }

            if (property.GetMemoryContext(out IDebugMemoryContext2 memoryContext) == HRConstants.S_OK)
            {
                CONTEXT_INFO[] contextInfo = new CONTEXT_INFO[1];
                if (memoryContext.GetInfo(enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS, contextInfo) == HRConstants.S_OK)
                {
                    if (contextInfo[0].dwFields.HasFlag(enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS))
                    {
                        return contextInfo[0].bstrAddress;
                    }
                }
            }

            return null;
        }

        private static bool IsPointerOrArray(IDebugProperty2 property)
        {
            DEBUG_PROPERTY_INFO[] propertyInfo = new DEBUG_PROPERTY_INFO[1];
            if (property.GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE, Constants.EvaluationRadix, Constants.EvaluationTimeout, null, 0, propertyInfo) != HRConstants.S_OK)
            {
                return false;
            }

            if (!propertyInfo[0].dwFields.HasFlag(enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE))
            {
                return false;
            }

            string typeName = propertyInfo[0].bstrType;
            if (string.IsNullOrEmpty(typeName))
            {
                return false;
            }

            typeName = typeName.TrimEnd();
            return typeName.EndsWith("*", StringComparison.Ordinal)   // pointer, e.g. "int *"
                || typeName.EndsWith("]", StringComparison.Ordinal);  // array, e.g. "int [10]"
        }
    }
}
