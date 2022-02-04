// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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

    internal enum VariableCategory
    {
        Locals,
        Registers
    }

    internal class VariableScope
    {
        internal IDebugStackFrame2 StackFrame;
        internal VariableCategory Category;
    }

    internal class VariableManager
    {
        // NOTE: The value being stored can be a VariableScope or a VariableEvaluationData
        private readonly HandleCollection<Object> m_variableHandles;

        // NOTE: ((VariablesReference | IDebugStackFrame2), Name) -> IDebugProperty2
        private readonly Dictionary<Tuple<object, string>, IDebugProperty2> m_variableProperties;

        public const string VariableNameFormat = "{0} #{1}";

        internal VariableManager()
        {
            m_variableHandles = new HandleCollection<Object>();
            m_variableProperties = new Dictionary<Tuple<object, string>, IDebugProperty2>();
        }

        internal void Reset()
        {
            m_variableHandles.Reset();
            m_variableProperties.Clear();
        }

        internal Boolean IsEmpty()
        {
            return m_variableHandles.IsEmpty;
        }

        internal bool TryGetProperty((object, string) key, out IDebugProperty2 prop)
        {
            return m_variableProperties.TryGetValue(Tuple.Create(key.Item1, key.Item2), out prop);
        }

        internal bool TryGet(int handle, out object value)
        {
            return m_variableHandles.TryGet(handle, out value);
        }

        internal int Create(VariableScope scope)
        {
            return m_variableHandles.Create(scope);
        }

        public void AddVariableProperty((object, string) key, IDebugProperty2 prop)
        {
            m_variableProperties[Tuple.Create(key.Item1, key.Item2)] = prop;
        }

        internal Variable CreateVariable(IDebugProperty2 property, enum_DEBUGPROP_INFO_FLAGS propertyInfoFlags)
        {
            var propertyInfo = new DEBUG_PROPERTY_INFO[1];
            property.GetPropertyInfo(propertyInfoFlags, Constants.EvaluationRadix, Constants.EvaluationTimeout, null, 0, propertyInfo);

            string memoryReference = AD7Utils.GetMemoryReferenceFromIDebugProperty(property);

            return CreateVariable(ref propertyInfo[0], propertyInfoFlags, memoryReference);
        }

        internal Variable CreateVariable(ref DEBUG_PROPERTY_INFO propertyInfo, enum_DEBUGPROP_INFO_FLAGS propertyInfoFlags, string memoryReference)
        {
            string name = propertyInfo.bstrName;
            string val = propertyInfo.bstrValue ?? "";
            string type = null;

            // If we have a type string, and the value isn't just the type string in brackets, encode the shorthand for the type in the name value.
            if (!string.IsNullOrEmpty(propertyInfo.bstrType))
            {
                type = propertyInfo.bstrType;
            }

            int handle = GetVariableHandle(propertyInfo, propertyInfoFlags);
            var v = new Variable
            {
                Name = name,
                Value = val,
                Type = type,
                VariablesReference = handle,
                EvaluateName = propertyInfo.bstrFullName,
                MemoryReference = memoryReference
            };

            if (propertyInfo.dwAttrib.HasFlag(enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_READONLY))
                v.PresentationHint = new VariablePresentationHint() { Attributes = VariablePresentationHint.AttributesValue.ReadOnly };

            return v;
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
