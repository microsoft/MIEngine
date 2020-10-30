using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using ProtocolMessages = Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace OpenDebugAD7
{
    internal class ModuleManager
    {
        private int m_nextModuleId = 1;
        private readonly ConcurrentDictionary<IDebugModule2, int> m_moduleMap = new ConcurrentDictionary<IDebugModule2, int>();

        #region ModuleMap Methods

        internal int? RegisterDebugModule(IDebugModule2 debugModule)
        {
            Debug.Assert(!m_moduleMap.ContainsKey(debugModule), "Trying to register a module that we have already seen.");

            int moduleId = m_nextModuleId;
            if (debugModule != null && m_moduleMap.TryAdd(debugModule, moduleId))
            {
                m_moduleMap[debugModule] = moduleId;
                m_nextModuleId++;

                return moduleId;
            }

            return null;
        }
        internal int? ReleaseDebugModule(IDebugModule2 debugModule)
        {
            if (debugModule != null)
            {
                if (m_moduleMap.TryRemove(debugModule, out int moduleId))
                {
                    return moduleId;
                }
                else
                {
                    Debug.Fail("Trying to unload a module that has not been registered.");
                }
            }

            return null;
        }

        #endregion

        internal int? GetModuleId(IDebugModule2 module)
        {
            if (module != null && m_moduleMap.TryGetValue(module, out int moduleId))
            {
                return moduleId;
            }

            return null;
        }

        #region DebugAdapterProtocol

        internal ProtocolMessages.Module ConvertToModule(in IDebugModule2 module, int moduleId)
        {
            var debugModuleInfos = new MODULE_INFO[1];
            if (module != null && module.GetInfo(enum_MODULE_INFO_FIELDS.MIF_ALLFIELDS, debugModuleInfos) == HRConstants.S_OK)
            {
                MODULE_INFO debugModuleInfo = debugModuleInfos[0];

                string vsTimestampUTC = null;
                if ((debugModuleInfo.dwValidFields & enum_MODULE_INFO_FIELDS.MIF_TIMESTAMP) != 0)
                {
                    vsTimestampUTC = FileTimeToPosix(debugModuleInfo.m_TimeStamp).ToString(CultureInfo.InvariantCulture);
                }

                var vsModuleSize = (int)debugModuleInfo.m_dwSize;
                var vsLoadOrder = (int)debugModuleInfo.m_dwLoadOrder;
                bool vsIs64Bit = (debugModuleInfo.m_dwModuleFlags & enum_MODULE_FLAGS.MODULE_FLAG_64BIT) != 0;

                // IsOptimized and IsUserCode are not set by gdb
                bool? isOptimized = null;
                if ((debugModuleInfo.m_dwModuleFlags & enum_MODULE_FLAGS.MODULE_FLAG_OPTIMIZED) != 0)
                {
                    isOptimized = true;
                }
                else if ((debugModuleInfo.m_dwModuleFlags & enum_MODULE_FLAGS.MODULE_FLAG_UNOPTIMIZED) != 0)
                {
                    isOptimized = false;
                }

                bool? isUserCode = null;
                if (module is IDebugModule3 module3 && module3.IsUserCode(out int userCode) == HRConstants.S_OK)
                {
                    isUserCode = userCode == 0;
                }

                return new ProtocolMessages.Module(moduleId, debugModuleInfo.m_bstrName)
                {
                    Path = debugModuleInfo.m_bstrUrl,
                    VsTimestampUTC = vsTimestampUTC,
                    Version = debugModuleInfo.m_bstrVersion,
                    VsLoadAddress = debugModuleInfo.m_addrLoadAddress.ToString(CultureInfo.InvariantCulture),
                    VsPreferredLoadAddress = debugModuleInfo.m_addrPreferredLoadAddress.ToString(CultureInfo.InvariantCulture),
                    VsModuleSize = vsModuleSize,
                    VsLoadOrder = vsLoadOrder,
                    SymbolFilePath = debugModuleInfo.m_bstrUrlSymbolLocation,
                    SymbolStatus = debugModuleInfo.m_bstrDebugMessage,
                    VsIs64Bit = vsIs64Bit,
                    IsOptimized = isOptimized,
                    IsUserCode = isUserCode
                };
            }

            return null;
        }

        #endregion

        #region Utilities

        private static long FileTimeToPosix(FILETIME ft)
        {
            long date = ((long)ft.dwHighDateTime << 32) + ft.dwLowDateTime;
            // removes the diff between 1970 and 1601
            // 100-nanoseconds = milliseconds * 10000
            date -= 11644473600000L * 10000;

            // converts back from 100-nanoseconds to seconds
            return date / 10000000;
        }

        #endregion
    }
}
