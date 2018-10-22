// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DebugEngineHost.VSCode;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace OpenDebugAD7
{
    internal class VariableEvaluationData
    {
        internal IDebugProperty2 DebugProperty;
        internal enum_DEBUGPROP_INFO_FLAGS propertyInfoFlags;
    }

    internal class VariableManager
    {
        // NOTE: The value being stored can be a IDebugStackFrame2 or a VariableEvaluationData
        private readonly HandleCollection<Object> m_variableHandles;

        internal VariableManager()
        {
            m_variableHandles = new HandleCollection<Object>();
        }

        internal void Reset()
        {
            m_variableHandles.Reset();
        }

        internal Boolean IsEmpty()
        {
            return m_variableHandles.IsEmpty;
        }

        internal bool TryGet(int handle, out object value)
        {
            return m_variableHandles.TryGet(handle, out value);
        }

        internal int Create(IDebugStackFrame2 frame)
        {
            return m_variableHandles.Create(frame);
        }

        internal Variable CreateVariable(IDebugProperty2 property, enum_DEBUGPROP_INFO_FLAGS propertyInfoFlags)
        {
            DEBUG_PROPERTY_INFO[] propertyInfo = new DEBUG_PROPERTY_INFO[1];
            property.GetPropertyInfo(propertyInfoFlags, Constants.EvaluationRadix, Constants.EvaluationTimeout, null, 0, propertyInfo);

            return CreateVariable(ref propertyInfo[0], propertyInfoFlags);
        }

        internal Variable CreateVariable(ref DEBUG_PROPERTY_INFO propertyInfo, enum_DEBUGPROP_INFO_FLAGS propertyInfoFlags)
        {
            string name = propertyInfo.bstrName;
            string val = propertyInfo.bstrValue;
            string type = null;

            // If we have a type string, and the value isn't just the type string in brackets, encode the shorthand for the type in the name value.
            if (!string.IsNullOrEmpty(propertyInfo.bstrType))
            {
                type = propertyInfo.bstrType;
            }

            int handle = GetVariableHandle(propertyInfo, propertyInfoFlags);
            return new Variable
            {
                Name = name,
                Value = val,
                Type = type,
                VariablesReference = handle,
                EvaluateName = propertyInfo.bstrFullName,
            };
        }

        internal int GetVariableHandle(DEBUG_PROPERTY_INFO propertyInfo, enum_DEBUGPROP_INFO_FLAGS propertyInfoFlags)
        {
            int handle = 0;
            if (propertyInfo.dwAttrib.HasFlag(enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE))
            {
                handle = m_variableHandles.Create(new VariableEvaluationData { DebugProperty = propertyInfo.pProperty, propertyInfoFlags = propertyInfoFlags });
            }

            return handle;
        }
    }
}
