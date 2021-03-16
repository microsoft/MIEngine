// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.MIDebugEngine
{
    // An implementation of IDebugProperty2
    // This interface represents a stack frame property, a program document property, or some other property. 
    // The property is usually the result of an expression evaluation. 
    //
    // The sample engine only supports locals and parameters for functions that have symbols loaded.
    internal class AD7Property : IDebugProperty3, IDebugProperty160
    {
        private static uint s_maxChars = 1000000;
        private byte[] _bytes;

        private AD7Engine _engine;
        private IVariableInformation _variableInformation;

        public AD7Property(AD7Engine engine, IVariableInformation vi)
        {
            _engine = engine;
            _variableInformation = vi;
        }

        private static ulong DBG_ATTRIB_HAS_DATA_BREAKPOINT = 0x1000000000000000;

        // Construct a DEBUG_PROPERTY_INFO representing this local or parameter.
        public DEBUG_PROPERTY_INFO ConstructDebugPropertyInfo(enum_DEBUGPROP_INFO_FLAGS dwFields)
        {
            IVariableInformation variable = _variableInformation;
            if ((dwFields & (enum_DEBUGPROP_INFO_FLAGS)enum_DEBUGPROP_INFO_FLAGS100.DEBUGPROP100_INFO_NOSIDEEFFECTS) != 0)
            {
                if ((variable = _engine.DebuggedProcess.Natvis.Cache.VisualizeOnRefresh(_variableInformation)) == null)
                {
                    return AD7ErrorProperty.ConstructErrorPropertyInfo(dwFields, _variableInformation.Name, ResourceStrings.NoSideEffectsVisualizerMessage, this, _variableInformation.FullName());
                }
            }

            DEBUG_PROPERTY_INFO propertyInfo = new DEBUG_PROPERTY_INFO();
            string fullName = variable.FullName();

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME) != 0)
            {
                propertyInfo.bstrFullName = fullName;
                if (propertyInfo.bstrFullName != null)
                {
                    propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME;
                }
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME) != 0)
            {
                propertyInfo.bstrName = variable.Name;
                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME;
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE) != 0 && !string.IsNullOrEmpty(variable.TypeName))
            {
                propertyInfo.bstrType = variable.TypeName;
                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE;
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE) != 0)
            {
                propertyInfo.bstrValue = _engine.DebuggedProcess.Natvis.FormatDisplayString(variable);
                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE;
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB) != 0)
            {
                if (variable.CountChildren != 0)
                {
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
                }

                if (variable.Error)
                {
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR;
                } else
                {
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_DATA;
                    if (!string.IsNullOrEmpty(fullName))
                    {
                        lock (_engine.DebuggedProcess.DataBreakpointVariables)
                        {
                            if (_engine.DebuggedProcess.DataBreakpointVariables.Any(candidate =>
                                candidate.Length > fullName.Length
                                && candidate.EndsWith(fullName, StringComparison.Ordinal)
                                && candidate[candidate.Length - fullName.Length - 1] == ','))
                            {
                                try
                                {
                                    if (_engine.DebuggedProcess.DataBreakpointVariables.Contains(variable.Address() + "," + fullName))
                                    {
                                        propertyInfo.dwAttrib |= (enum_DBG_ATTRIB_FLAGS)DBG_ATTRIB_HAS_DATA_BREAKPOINT;
                                    }
                                }
                                catch (Exception e) { }
                            }
                        }
                    }
                }

                if (variable.IsStringType)
                {
                    propertyInfo.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_RAW_STRING;
                }
                propertyInfo.dwAttrib |= variable.Access;
            }

            // If the debugger has asked for the property, or the property has children (meaning it is a pointer in the sample)
            // then set the pProperty field so the debugger can call back when the children are enumerated.
            if (((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP) != 0) ||
                (variable.CountChildren != 0))
            {
                propertyInfo.pProperty = (IDebugProperty2)this;
                propertyInfo.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP;
            }

            return propertyInfo;
        }

        #region IDebugProperty2 Members

        // Enumerates the children of a property. This provides support for dereferencing pointers, displaying members of an array, or fields of a class or struct.
        public int EnumChildren(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, ref Guid guidFilter, enum_DBG_ATTRIB_FLAGS dwAttribFilter, string pszNameFilter, uint dwTimeout, out IEnumDebugPropertyInfo2 ppEnum)
        {
            ppEnum = null;

            _variableInformation.PropertyInfoFlags = dwFields;
            _variableInformation.EnsureChildren();

            if (_variableInformation.CountChildren != 0)
            {
                try
                {
                    _engine.DebuggedProcess.Natvis.WaitDialog.ShowWaitDialog(_variableInformation.Name);
                    var children = _engine.DebuggedProcess.Natvis.Expand(_variableInformation);
                    DEBUG_PROPERTY_INFO[] properties = new DEBUG_PROPERTY_INFO[children.Length];
                    for (int i = 0; i < children.Length; i++)
                    {
                        properties[i] = (new AD7Property(_engine, children[i])).ConstructDebugPropertyInfo(dwFields);
                    }
                    ppEnum = new AD7PropertyEnum(properties);
                    return Constants.S_OK;
                }
                finally
                {
                    _engine.DebuggedProcess.Natvis.WaitDialog.EndWaitDialog();
                }
            }

            return Constants.S_FALSE;
        }

        // Returns the property that describes the most-derived property of a property
        // This is called to support object oriented languages. It allows the debug engine to return an IDebugProperty2 for the most-derived 
        // object in a hierarchy. This engine does not support this.
        public int GetDerivedMostProperty(out IDebugProperty2 ppDerivedMost)
        {
            throw new NotImplementedException();
        }

        // This method exists for the purpose of retrieving information that does not lend itself to being retrieved by calling the IDebugProperty2::GetPropertyInfo 
        // method. This includes information about custom viewers, managed type slots and other information.
        // The sample engine does not support this.
        public int GetExtendedInfo(ref System.Guid guidExtendedInfo, out object pExtendedInfo)
        {
            throw new NotImplementedException();
        }

        // Returns the memory bytes for a property value.
        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            throw new NotImplementedException();
        }

        // Returns the memory context for a property value.
        public int GetMemoryContext(out IDebugMemoryContext2 ppMemory)
        {
            ppMemory = null;
            if (_variableInformation.Error)
                return AD7_HRESULT.S_GETMEMORYCONTEXT_NO_MEMORY_CONTEXT;
            // try to interpret the result as an address
            string v = _variableInformation.Value;
            if (string.IsNullOrWhiteSpace(v))
            {
                return AD7_HRESULT.S_GETMEMORYCONTEXT_NO_MEMORY_CONTEXT;
            }
            v = v.Trim();
            if (v[0] == '{')
            {
                // strip type name and trailing spaces
                v = v.Substring(v.IndexOf('}') + 1);
                v = v.Trim();
            }
            int i = v.IndexOf(' ');
            if (i > 0)
            {
                v = v.Substring(0, i);
            }
            ulong addr;
            if (!UInt64.TryParse(v, System.Globalization.NumberStyles.Any, null, out addr))
            {
                if (v.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    v = v.Substring(2);
                    if (!UInt64.TryParse(v, System.Globalization.NumberStyles.AllowHexSpecifier, null, out addr))
                    {
                        return AD7_HRESULT.S_GETMEMORYCONTEXT_NO_MEMORY_CONTEXT;
                    }
                }
                else
                {
                    return AD7_HRESULT.S_GETMEMORYCONTEXT_NO_MEMORY_CONTEXT;
                }
            }
            ppMemory = new AD7MemoryAddress(_engine, addr, null);
            return Constants.S_OK;
        }

        // Returns the parent of a property.
        // The sample engine does not support obtaining the parent of properties.
        public int GetParent(out IDebugProperty2 ppParent)
        {
            throw new NotImplementedException();
        }

        // Fills in a DEBUG_PROPERTY_INFO structure that describes a property.
        public int GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, uint dwTimeout, IDebugReference2[] rgpArgs, uint dwArgCount, DEBUG_PROPERTY_INFO[] pPropertyInfo)
        {
            pPropertyInfo[0] = new DEBUG_PROPERTY_INFO();
            rgpArgs = null;
            pPropertyInfo[0] = ConstructDebugPropertyInfo(dwFields);
            return Constants.S_OK;
        }

        //  Return an IDebugReference2 for this property. An IDebugReference2 can be thought of as a type and an address.
        public int GetReference(out IDebugReference2 ppReference)
        {
            throw new NotImplementedException();
        }

        // Returns the size, in bytes, of the property value.
        public int GetSize(out uint pdwSize)
        {
            throw new NotImplementedException();
        }

        // The debugger will call this when the user tries to edit the property's values
        // the sample has set the read-only flag on its properties, so this should not be called.
        public int SetValueAsReference(IDebugReference2[] rgpArgs, uint dwArgCount, IDebugReference2 pValue, uint dwTimeout)
        {
            throw new NotImplementedException();
        }

        // The debugger will call this when the user tries to edit the property's values in one of the debugger windows.
        // the sample has set the read-only flag on its properties, so this should not be called.
        public int SetValueAsString(string pszValue, uint dwRadix, uint dwTimeout)
        {
            string error;
            return SetValueAsStringWithError(pszValue, dwRadix, dwTimeout, out error);
        }

        #endregion

        public int CreateObjectID()
        {
            throw new NotImplementedException();
        }

        public int DestroyObjectID()
        {
            throw new NotImplementedException();
        }

        public int GetCustomViewerCount(out uint pcelt)
        {
            pcelt = 0;
            return Constants.S_OK;
        }

        public int GetCustomViewerList(uint celtSkip, uint celtRequested, DEBUG_CUSTOM_VIEWER[] rgViewers, out uint pceltFetched)
        {
            throw new NotImplementedException();
        }

        private void InitializeBytes()
        {
            if (_bytes != null)
                return;

            uint fetched = 0;
            _bytes = new byte[0];

            IDebugMemoryContext2 memAddr;
            if (GetMemoryContext(out memAddr) != Constants.S_OK)
            {
                // no address in the expression value, try casting to a char*
                VariableInformation v = new VariableInformation("(char*)(" + _variableInformation.FullName() + ")", (VariableInformation)_variableInformation);
                v.SyncEval();
                if (v.Error)
                {
                    return;
                }
                AD7Property p = new AD7Property(_engine, v);
                uint pLen;
                if (p.GetStringCharLength(out pLen) == Constants.S_OK)
                {
                    _bytes = new byte[pLen];
                    p.GetStringRawBytes(pLen, _bytes, out fetched);
                }
                return;
            }

            IDebugMemoryBytes2 memContent;
            if (((AD7MemoryAddress)memAddr).Engine.GetMemoryBytes(out memContent) != Constants.S_OK)
            {
                return;
            }

            fetched = 0;
            bool eos = false;
            byte[] bytes = new byte[s_maxChars + 1];
            byte[] chunk = new byte[2048];
            while (!eos)
            {
                // fetched is count of bytes in string so far
                // eos == false => fetch < s_maxChars
                // eos == true => string is terminated, that is bytes[fetched-1] == 0

                uint bytesRead;
                uint bytesUnreadable = 0;
                // get next chunk
                if (memContent.ReadAt(memAddr, (uint)chunk.Length, chunk, out bytesRead, ref bytesUnreadable) != Constants.S_OK)
                {
                    break;
                }
                // copy chunk to bytes
                for (uint i = 0; i < bytesRead; ++i)
                {
                    bytes[fetched++] = chunk[i];
                    if (bytes[fetched - 1] == 0)
                    {
                        eos = true; // end of string
                        break;
                    }
                    if (fetched == s_maxChars)    // buffer is full
                    {
                        bytes[fetched++] = 0;   // end the string
                        eos = true;
                        break;
                    }
                }
                if (bytesRead != chunk.Length)
                {
                    // read to end of available memory
                    break;
                }
                // advance to next chunk
                memAddr.Add(bytesRead, out memAddr);
            }
            if (!eos)
            {
                Debug.Assert(fetched < bytes.Length);
                bytes[fetched++] = 0;
            }
            if (fetched < bytes.Length)
            {
                _bytes = new byte[fetched];
                Array.Copy(bytes, _bytes, (int)fetched);
            }
            else
            {
                _bytes = bytes;
            }
        }

        public int GetStringCharLength(out uint pLen)
        {
            InitializeBytes();
            pLen = (uint)_bytes.Length;
            return Constants.S_OK;
        }
        private int GetStringRawBytes(uint buflen, byte[] rgString, out uint pceltFetched)
        {
            InitializeBytes();
            for (pceltFetched = 0; pceltFetched < Math.Min(_bytes.Length, buflen); ++pceltFetched)
            {
                rgString[pceltFetched] = _bytes[pceltFetched];
            }
            return Constants.S_OK;
        }

        public int GetStringChars(uint buflen, ushort[] rgString, out uint pceltFetched)
        {
            pceltFetched = 0;
            if (_bytes == null)
            {
                return Constants.E_FAIL;
            }
            for (pceltFetched = 0; pceltFetched < Math.Min(_bytes.Length, buflen); ++pceltFetched)
            {
                rgString[pceltFetched] = _bytes[pceltFetched];
            }
            return Constants.S_OK;
        }

        public int SetValueAsStringWithError(string pszValue, uint dwRadix, uint dwTimeout, out string errorString)
        {
            errorString = null;
            try
            {
                if (_variableInformation is VariableInformation)
                {
                    ((VariableInformation)_variableInformation).Assign(pszValue);
                    return Constants.S_OK;
                }
                else
                {
                    errorString = ResourceStrings.InvalidAssignment;
                }
            }
            catch (MICore.UnexpectedMIResultException e)
            {
                if (!string.IsNullOrEmpty(e.MIError))
                {
                    errorString = e.MIError;
                }
                else
                {
                    errorString = e.Message;
                }
            }
            return Constants.E_FAIL;
        }

        public int GetDataBreakpointInfo160(out string pbstrAddress, out uint pSize, out string pbstrDisplayName, out string pbstrError)
        {
            try
            {
                pbstrAddress = _variableInformation.Address() + "," + _variableInformation.FullName();
                pSize = _variableInformation.Size();
                pbstrDisplayName = _variableInformation.FullName();
                pbstrError = "";
                return Constants.S_OK;
            }
            catch (Exception e)
            {
                pbstrAddress = null;
                pSize = 0;
                pbstrDisplayName = null;
                pbstrError = e.Message;
            }
            return Constants.E_FAIL;
        }
    }

    internal class AD7ErrorProperty : IDebugProperty3
    {
        private readonly string _name;
        private readonly string _message;
        public AD7ErrorProperty(string name, string message)
        {
            _name = name;
            _message = message;
        }
        public int CreateObjectID()
        {
            throw new NotImplementedException();
        }

        public int DestroyObjectID()
        {
            throw new NotImplementedException();
        }

        public int EnumChildren(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, ref Guid guidFilter, enum_DBG_ATTRIB_FLAGS dwAttribFilter, string pszNameFilter, uint dwTimeout, out IEnumDebugPropertyInfo2 ppEnum)
        {
            ppEnum = null;
            return Constants.S_FALSE;
        }

        public int GetCustomViewerCount(out uint pcelt)
        {
            pcelt = 0;
            return Constants.S_OK;
        }

        public int GetCustomViewerList(uint celtSkip, uint celtRequested, DEBUG_CUSTOM_VIEWER[] rgViewers, out uint pceltFetched)
        {
            throw new NotImplementedException();
        }

        public int GetDerivedMostProperty(out IDebugProperty2 ppDerivedMost)
        {
            throw new NotImplementedException();
        }

        public int GetExtendedInfo(ref Guid guidExtendedInfo, out object pExtendedInfo)
        {
            throw new NotImplementedException();
        }

        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            throw new NotImplementedException();
        }

        public int GetMemoryContext(out IDebugMemoryContext2 ppMemory)
        {
            ppMemory = null;
            return AD7_HRESULT.S_GETMEMORYCONTEXT_NO_MEMORY_CONTEXT;
        }

        public int GetParent(out IDebugProperty2 ppParent)
        {
            throw new NotImplementedException();
        }

        public static DEBUG_PROPERTY_INFO ConstructErrorPropertyInfo(enum_DEBUGPROP_INFO_FLAGS dwFields, string name, string error, IDebugProperty2 prop, string varFullName)
        {
            DEBUG_PROPERTY_INFO property = new DEBUG_PROPERTY_INFO();
            // Add the parent fullname to fullname so when it is refreshed, it will evaluate.
            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME) != 0)
            {
                property.bstrFullName = varFullName;
                property.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_FULLNAME;
            }
            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME) != 0)
            {
                property.bstrName = name;
                property.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME;
            }
            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE) != 0)
            {
                property.bstrValue = error;
                property.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE;
            }
            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB) != 0)
            {
                property.dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_ERROR;
            }
            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP) != 0)
            {
                property.pProperty = prop;
                property.dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP;
            }
            return property;
        }

        public int GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, uint dwTimeout, IDebugReference2[] rgpArgs, uint dwArgCount, DEBUG_PROPERTY_INFO[] pPropertyInfo)
        {
            pPropertyInfo[0] = ConstructErrorPropertyInfo(dwFields, _name, _message, this, null);
            rgpArgs = null;
            return Constants.S_OK;
        }

        public int GetReference(out IDebugReference2 ppReference)
        {
            throw new NotImplementedException();
        }

        public int GetSize(out uint pdwSize)
        {
            throw new NotImplementedException();
        }

        public int GetStringCharLength(out uint pLen)
        {
            pLen = 0;
            return Constants.S_OK;
        }

        public int GetStringChars(uint buflen, ushort[] rgString, out uint pceltFetched)
        {
            pceltFetched = 0;
            return Constants.E_FAIL;
        }

        public int SetValueAsReference(IDebugReference2[] rgpArgs, uint dwArgCount, IDebugReference2 pValue, uint dwTimeout)
        {
            throw new NotImplementedException();
        }

        public int SetValueAsString(string pszValue, uint dwRadix, uint dwTimeout)
        {
            return Constants.E_FAIL;
        }

        public int SetValueAsStringWithError(string pszValue, uint dwRadix, uint dwTimeout, out string errorString)
        {
            errorString = null;
            return Constants.E_FAIL;
        }
    }
}
