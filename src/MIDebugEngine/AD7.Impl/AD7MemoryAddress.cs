// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.Debugger.Interop;
using MICore;

namespace Microsoft.MIDebugEngine
{
    // And implementation of IDebugCodeContext2 and IDebugMemoryContext2. 
    // IDebugMemoryContext2 represents a position in the address space of the machine running the program being debugged.
    // IDebugCodeContext2 represents the starting position of a code instruction. 
    // For most run-time architectures today, a code context can be thought of as an address in a program's execution stream.
    internal sealed class AD7MemoryAddress : IDebugCodeContext2
    {
        private readonly AD7Engine _engine;
        private readonly ulong _address;
        /*OPTIONAL*/
        private string _functionName;
        private IDebugDocumentContext2 _documentContext;

        public AD7MemoryAddress(AD7Engine engine, ulong address, /*OPTIONAL*/ string functionName)
        {
            _engine = engine;
            _address = address;
            _functionName = functionName;
        }

        internal ulong Address { get { return _address; } }
        internal AD7Engine Engine { get { return _engine; } }

        public void SetDocumentContext(IDebugDocumentContext2 docContext)
        {
            _documentContext = docContext;
        }

        #region IDebugMemoryContext2 Members

        // Adds a specified value to the current context's address to create a new context.
        public int Add(ulong dwCount, out IDebugMemoryContext2 newAddress)
        {
            // NB: this is not correct for IDebugCodeContext2 according to the docs
            // https://docs.microsoft.com/en-us/visualstudio/extensibility/debugger/reference/idebugcodecontext2#remarks
            // But it's not used in practice (instead: IDebugDisassemblyStream2.Seek)
            newAddress = new AD7MemoryAddress(_engine, (uint)dwCount + _address, null);
            return Constants.S_OK;
        }

        // Compares the memory context to each context in the given array in the manner indicated by compare flags, 
        // returning an index of the first context that matches.
        public int Compare(enum_CONTEXT_COMPARE contextCompare, IDebugMemoryContext2[] compareToItems, uint compareToLength, out uint foundIndex)
        {
            foundIndex = uint.MaxValue;

            try
            {
                for (uint c = 0; c < compareToLength; c++)
                {
                    AD7MemoryAddress compareTo = compareToItems[c] as AD7MemoryAddress;
                    if (compareTo == null)
                    {
                        continue;
                    }

                    if (!AD7Engine.ReferenceEquals(_engine, compareTo._engine))
                    {
                        continue;
                    }

                    bool result;

                    switch (contextCompare)
                    {
                        case enum_CONTEXT_COMPARE.CONTEXT_EQUAL:
                            result = (_address == compareTo._address);
                            break;

                        case enum_CONTEXT_COMPARE.CONTEXT_LESS_THAN:
                            result = (_address < compareTo._address);
                            break;

                        case enum_CONTEXT_COMPARE.CONTEXT_GREATER_THAN:
                            result = (_address > compareTo._address);
                            break;

                        case enum_CONTEXT_COMPARE.CONTEXT_LESS_THAN_OR_EQUAL:
                            result = (_address <= compareTo._address);
                            break;

                        case enum_CONTEXT_COMPARE.CONTEXT_GREATER_THAN_OR_EQUAL:
                            result = (_address >= compareTo._address);
                            break;

                        // The debug engine doesn't understand scopes
                        case enum_CONTEXT_COMPARE.CONTEXT_SAME_SCOPE:
                            result = (_address == compareTo._address);
                            break;

                        case enum_CONTEXT_COMPARE.CONTEXT_SAME_FUNCTION:
                            if (_address == compareTo._address)
                            {
                                result = true;
                                break;
                            }
                            string funcThis = Engine.GetAddressDescription(_address);
                            if (string.IsNullOrEmpty(funcThis))
                            {
                                result = false;
                                break;
                            }
                            string funcCompareTo = Engine.GetAddressDescription(compareTo._address);
                            result = (funcThis == funcCompareTo);
                            break;

                        case enum_CONTEXT_COMPARE.CONTEXT_SAME_MODULE:
                            result = (_address == compareTo._address);
                            if (result == false)
                            {
                                DebuggedModule module = _engine.DebuggedProcess.ResolveAddress(_address);

                                if (module != null)
                                {
                                    result = module.AddressInModule(compareTo._address);
                                }
                            }
                            break;

                        case enum_CONTEXT_COMPARE.CONTEXT_SAME_PROCESS:
                            result = true;
                            break;

                        default:
                            // A new comparison was invented that we don't support
                            return Constants.E_NOTIMPL;
                    }

                    if (result)
                    {
                        foundIndex = c;
                        return Constants.S_OK;
                    }
                }

                return Constants.S_FALSE;
            }
            catch (MIException e)
            {
                return e.HResult;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Gets information that describes this context.
        public int GetInfo(enum_CONTEXT_INFO_FIELDS dwFields, CONTEXT_INFO[] pinfo)
        {
            try
            {
                pinfo[0].dwFields = 0;

                if ((dwFields & (enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS | enum_CONTEXT_INFO_FIELDS.CIF_ADDRESSABSOLUTE)) != 0)
                {
                    string addr = EngineUtils.AsAddr(_address, _engine.DebuggedProcess.Is64BitArch);
                    if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS) != 0)
                    {
                        pinfo[0].bstrAddress = addr;
                        pinfo[0].dwFields |= enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS;
                    }
                    if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_ADDRESSABSOLUTE) != 0)
                    {
                        pinfo[0].bstrAddressAbsolute = addr;
                        pinfo[0].dwFields |= enum_CONTEXT_INFO_FIELDS.CIF_ADDRESSABSOLUTE;
                    }
                }
                // Fields not supported by the sample
                if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_ADDRESSOFFSET) != 0) { }
                if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_MODULEURL) != 0)
                {
                    DebuggedModule module = _engine.DebuggedProcess.ResolveAddress(_address);
                    if (module != null)
                    {
                        pinfo[0].bstrModuleUrl = module.Name;
                        pinfo[0].dwFields |= enum_CONTEXT_INFO_FIELDS.CIF_MODULEURL;
                    }
                }
                if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_FUNCTION) != 0)
                {
                    if (string.IsNullOrEmpty(_functionName))
                    {
                        _functionName = Engine.GetAddressDescription(_address);
                    }

                    if (!(string.IsNullOrEmpty(_functionName)))
                    {
                        pinfo[0].bstrFunction = _functionName;
                        pinfo[0].dwFields |= enum_CONTEXT_INFO_FIELDS.CIF_FUNCTION;
                    }
                }

                return Constants.S_OK;
            }
            catch (MIException e)
            {
                return e.HResult;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Gets the user-displayable name for this context
        public int GetName(out string pbstrName)
        {
            pbstrName = _functionName ?? Engine.GetAddressDescription(_address);
            return Constants.S_OK;
        }

        // Subtracts a specified value from the current context's address to create a new context.
        public int Subtract(ulong dwCount, out IDebugMemoryContext2 ppMemCxt)
        {
            ppMemCxt = new AD7MemoryAddress(_engine, _address - (uint)dwCount, null);
            return Constants.S_OK;
        }

        #endregion

        #region IDebugCodeContext2 Members

        // Gets the document context for this code-context. If no document context is available, return S_FALSE.
        public int GetDocumentContext(out IDebugDocumentContext2 ppSrcCxt)
        {
            int hr = Constants.S_OK;
            if (_documentContext == null)
                hr = Constants.S_FALSE;

            ppSrcCxt = _documentContext;
            return hr;
        }

        // Gets the language information for this code context.
        public int GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage)
        {
            if (_documentContext != null)
            {
                return _documentContext.GetLanguageInfo(ref pbstrLanguage, ref pguidLanguage);
            }
            else
            {
                return Constants.S_FALSE;
            }
        }

        #endregion
    }
}
