// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using System.Threading;
using MICore;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.MIDebugEngine
{
    // this class represents a module loaded in the debuggee process to the debugger. 
    internal class AD7Module : IDebugModule2, IDebugModule3
    {
        public readonly DebuggedProcess Process;
        public readonly DebuggedModule DebuggedModule;

        public AD7Module(DebuggedModule debuggedModule, DebuggedProcess process)
        {
            Debug.Assert(debuggedModule != null, "No module provided?");
            Debug.Assert(debuggedModule.Client == null, "Why is the client of the module already set?");
            Debug.Assert(process != null, "No process provided?");

            this.DebuggedModule = debuggedModule;
            this.Process = process;
        }

        #region IDebugModule2 Members

        // Gets the MODULE_INFO that describes this module.
        // This is how the debugger obtains most of the information about the module.
        int IDebugModule2.GetInfo(enum_MODULE_INFO_FIELDS dwFields, MODULE_INFO[] infoArray)
        {
            try
            {
                MODULE_INFO info = new MODULE_INFO();

                if ((dwFields & enum_MODULE_INFO_FIELDS.MIF_NAME) != 0)
                {
                    info.m_bstrName = System.IO.Path.GetFileName(this.DebuggedModule.Name);
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_NAME;
                }
                if ((dwFields & enum_MODULE_INFO_FIELDS.MIF_URL) != 0)
                {
                    info.m_bstrUrl = this.DebuggedModule.Name;
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_URL;
                }
                if ((dwFields & enum_MODULE_INFO_FIELDS.MIF_LOADADDRESS) != 0)
                {
                    info.m_addrLoadAddress = (ulong)this.DebuggedModule.BaseAddress;
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_LOADADDRESS;
                }
                if ((dwFields & enum_MODULE_INFO_FIELDS.MIF_PREFFEREDADDRESS) != 0)
                {
                    // A debugger that actually supports showing the preferred base should read the PE header and get 
                    // that field. This debugger does not do that, so assume the module loaded where it was suppose to.                   
                    info.m_addrPreferredLoadAddress = (ulong)this.DebuggedModule.BaseAddress;
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_PREFFEREDADDRESS; ;
                }
                if ((dwFields & enum_MODULE_INFO_FIELDS.MIF_SIZE) != 0)
                {
                    info.m_dwSize = (uint)this.DebuggedModule.Size;
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_SIZE;
                }
                if ((dwFields & enum_MODULE_INFO_FIELDS.MIF_LOADORDER) != 0)
                {
                    info.m_dwLoadOrder = this.DebuggedModule.GetLoadOrder();
                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_LOADORDER;
                }
                if ((dwFields & enum_MODULE_INFO_FIELDS.MIF_URLSYMBOLLOCATION) != 0)
                {
                    if (this.DebuggedModule.SymbolsLoaded)
                    {
                        info.m_bstrUrlSymbolLocation = this.DebuggedModule.SymbolPath;
                        info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_URLSYMBOLLOCATION;
                    }
                }
                if ((dwFields & enum_MODULE_INFO_FIELDS.MIF_FLAGS) != 0)
                {
                    info.m_dwModuleFlags = 0;
                    if (this.DebuggedModule.SymbolsLoaded)
                    {
                        info.m_dwModuleFlags |= (enum_MODULE_FLAGS.MODULE_FLAG_SYMBOLS);
                    }

                    if (this.Process.Is64BitArch)
                    {
                        info.m_dwModuleFlags |= enum_MODULE_FLAGS.MODULE_FLAG_64BIT;
                    }

                    info.dwValidFields |= enum_MODULE_INFO_FIELDS.MIF_FLAGS;
                }


                infoArray[0] = info;

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

        #endregion

        #region Deprecated interface methods
        // These methods are not called by the Visual Studio debugger, so they don't need to be implemented

        int IDebugModule2.ReloadSymbols_Deprecated(string urlToSymbols, out string debugMessage)
        {
            debugMessage = null;
            Debug.Fail("This function is not called by the debugger.");
            return Constants.E_NOTIMPL;
        }

        int IDebugModule3.ReloadSymbols_Deprecated(string pszUrlToSymbols, out string pbstrDebugMessage)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDebugModule3 Members

        // IDebugModule3 represents a module that supports alternate locations of symbols and JustMyCode states.
        // The sample does not support alternate symbol locations or JustMyCode, but it does not display symbol load information 

        // Gets the MODULE_INFO that describes this module.
        // This is how the debugger obtains most of the information about the module.
        int IDebugModule3.GetInfo(enum_MODULE_INFO_FIELDS dwFields, MODULE_INFO[] pinfo)
        {
            return ((IDebugModule2)this).GetInfo(dwFields, pinfo);
        }

        // Returns a list of paths searched for symbols and the results of searching each path.
        int IDebugModule3.GetSymbolInfo(enum_SYMBOL_SEARCH_INFO_FIELDS dwFields, MODULE_SYMBOL_SEARCH_INFO[] pinfo)
        {
            // This engine only supports loading symbols at the location specified in the binary's symbol path location in the PE file and
            // does so only for the primary exe of the debuggee.
            // Therefore, it only displays if the symbols were loaded or not. If symbols were loaded, that path is added.
            pinfo[0] = new MODULE_SYMBOL_SEARCH_INFO();
            pinfo[0].dwValidFields = 1; // SSIF_VERBOSE_SEARCH_INFO;

            if (this.DebuggedModule.SymbolsLoaded)
            {
                pinfo[0].bstrVerboseSearchInfo = string.Format(CultureInfo.CurrentUICulture, ResourceStrings.SymbolsLoadedInfo, this.DebuggedModule.SymbolPath);
            }
            else
            {
                pinfo[0].bstrVerboseSearchInfo = ResourceStrings.SymbolsNotLoadedInfo;
            }
            return Constants.S_OK;
        }

        // Used to support the JustMyCode features of the debugger.
        // the sample debug engine does not support JustMyCode and therefore all modules
        // are considered "My Code"
        int IDebugModule3.IsUserCode(out int pfUser)
        {
            pfUser = 1;
            return Constants.S_OK;
        }

        // Loads and initializes symbols for the current module when the user explicitly asks for them to load.
        // The sample engine only supports loading symbols from the location pointed to by the PE file which will load
        // when the module is loaded.
        int IDebugModule3.LoadSymbols()
        {
            Process.LoadSymbols(DebuggedModule);
            return Constants.S_OK;
        }

        // Used to support the JustMyCode features of the debugger.
        // The sample engine does not support JustMyCode so this is not implemented
        int IDebugModule3.SetJustMyCodeState(int fIsUserCode)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
