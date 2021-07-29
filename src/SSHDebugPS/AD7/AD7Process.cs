// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.SSHDebugPS
{
    [System.Diagnostics.DebuggerDisplay("{_processId}: {_commandLine}")]
    internal class AD7Process : IDebugProcess2, IDebugProcessSecurity2, IDebugProcessEx2, IDebugUnixProcess
    {
        private readonly AD7Port _port;
        private readonly uint _processId;
        private readonly string _systemArch;
        private readonly string _commandLine;
        private readonly string _userName;
        private readonly bool _isSameUser;
        private readonly Lazy<Guid> _uniqueId = new Lazy<Guid>(() => Guid.NewGuid(), LazyThreadSafetyMode.ExecutionAndPublication);
        private IDebugProgram2 _program;

        /// <summary>
        /// Flags are only used in ps command scenarios. It will be set to 0 for others.
        /// </summary>
        private readonly uint _flags;

        /// <summary>
        /// Returns true if _commandLine appears to hold a real file name + args rather than just a description
        /// </summary>
        private bool HasRealCommandLine { get { return _commandLine[0] != '['; } }

        public AD7Process(AD7Port port, Process psProcess)
        {
            _port = port;
            _processId = psProcess.Id;
            _commandLine = psProcess.CommandLine;
            _userName = psProcess.UserName;
            _isSameUser = psProcess.IsSameUser;
            _systemArch = psProcess.SystemArch;

            _flags = psProcess.Flags;
        }

        public uint Id => _processId;

        public int Attach(IDebugEventCallback2 pCallback, Guid[] rgguidSpecificEngines, uint celtSpecificEngines, int[] rghrEngineAttach)
        {
            throw new NotImplementedException();
        }

        public int CanDetach()
        {
            throw new NotImplementedException();
        }

        public int CauseBreak()
        {
            throw new NotImplementedException();
        }

        public int Detach()
        {
            throw new NotImplementedException();
        }

        public int EnumPrograms(out IEnumDebugPrograms2 @enum)
        {
            if (_program == null)
            {
                @enum = null;
                return HR.S_FALSE;
            }
            else
            {
                @enum = new AD7ProgramEnum(new IDebugProgram2[] { _program });
                return HR.S_OK;
            }
        }

        public int EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
            throw new NotImplementedException();
        }

        public int GetAttachedSessionName(out string pbstrSessionName)
        {
            throw new NotImplementedException();
        }

        public int GetInfo(enum_PROCESS_INFO_FIELDS fields, PROCESS_INFO[] pProcessInfo)
        {
            if ((fields & enum_PROCESS_INFO_FIELDS.PIF_FILE_NAME) != 0)
            {
                pProcessInfo[0].bstrFileName = GetFileName();
                pProcessInfo[0].Fields |= enum_PROCESS_INFO_FIELDS.PIF_FILE_NAME;
            }

            if ((fields & enum_PROCESS_INFO_FIELDS.PIF_BASE_NAME) != 0)
            {
                pProcessInfo[0].bstrBaseName = GetBaseName();
                pProcessInfo[0].Fields |= enum_PROCESS_INFO_FIELDS.PIF_BASE_NAME;
            }

            if ((fields & enum_PROCESS_INFO_FIELDS.PIF_TITLE) != 0)
            {
                string title = GetTitle();
                if (title != null)
                {
                    pProcessInfo[0].bstrTitle = title;
                    pProcessInfo[0].Fields |= enum_PROCESS_INFO_FIELDS.PIF_TITLE;
                }
            }

            if ((fields & enum_PROCESS_INFO_FIELDS.PIF_PROCESS_ID) != 0)
            {
                GetADProcessId(out pProcessInfo[0].ProcessId);
                pProcessInfo[0].Fields |= enum_PROCESS_INFO_FIELDS.PIF_PROCESS_ID;
            }

            if ((fields & enum_PROCESS_INFO_FIELDS.PIF_FLAGS) != 0)
            {
                pProcessInfo[0].Flags = 0;

                if (!_isSameUser || !this.HasRealCommandLine)
                {
                    pProcessInfo[0].Flags |= enum_PROCESS_INFO_FLAGS.PIFLAG_SYSTEM_PROCESS;
                }

                pProcessInfo[0].Fields |= enum_PROCESS_INFO_FIELDS.PIF_FLAGS;
            }

            return HR.S_OK;
        }

        public int GetName(enum_GETNAME_TYPE gnType, out string name)
        {
            switch (gnType)
            {
                case enum_GETNAME_TYPE.GN_NAME:
                case enum_GETNAME_TYPE.GN_BASENAME:
                    name = GetBaseName();
                    break;
                case enum_GETNAME_TYPE.GN_FILENAME:
                    name = GetFileName();
                    break;
                case enum_GETNAME_TYPE.GN_URL:
                    name = GetFileName();
                    if (string.IsNullOrEmpty(name) || name[0] != '/')
                        name = null;
                    else
                        name = "file:/" + name;
                    break;
                case enum_GETNAME_TYPE.GN_TITLE:
                    name = GetTitle();
                    break;
                case enum_GETNAME_TYPE.GN_STARTPAGEURL:
                case enum_GETNAME_TYPE.GN_MONIKERNAME:
                default:
                    name = null;
                    break;
            }

            if (name == null)
            {
                return HR.S_FALSE;
            }
            return HR.S_OK;
        }

        private string GetFileName()
        {
            if (!this.HasRealCommandLine)
                return _commandLine;

            char[] spaceTab = { ' ', '\t' };
            int startIndex = 0;
            while (true)
            {
                int indexOfSpace = _commandLine.IndexOfAny(spaceTab, startIndex);
                if (indexOfSpace < 0)
                    return _commandLine; // entire command line seems to be a single path

                if (indexOfSpace > 0 && indexOfSpace != _commandLine.Length - 1 && _commandLine[indexOfSpace - 1] == '\\')
                {
                    // space was escaped, loop again
                    startIndex = indexOfSpace + 1;
                    continue;
                }

                return _commandLine.Substring(0, indexOfSpace);
            }
        }

        private string GetBaseName()
        {
            if (!this.HasRealCommandLine)
                return _commandLine;

            string fileName = GetFileName();
            int lastSlash = fileName.LastIndexOf('/');
            if (lastSlash < 0)
                return fileName;

            return fileName.Substring(lastSlash + 1);
        }

        private string GetTitle()
        {
            // We don't have real titles, so we will use the full command line unless we don't have a real command line to show
            if (!this.HasRealCommandLine || GetBaseName().Equals(_commandLine, StringComparison.Ordinal))
                return null;

            return _commandLine;
        }

        public int GetPhysicalProcessId(AD_PROCESS_ID[] pProcessId)
        {
            GetADProcessId(out pProcessId[0]);
            return HR.S_OK;
        }

        private void GetADProcessId(out AD_PROCESS_ID processId)
        {
            processId = new AD_PROCESS_ID();
            processId.ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_SYSTEM;
            processId.dwProcessId = _processId;
        }

        public int GetPort(out IDebugPort2 port)
        {
            port = _port;
            return HR.S_OK;
        }

        public int GetProcessId(out Guid guidProcessId)
        {
            guidProcessId = _uniqueId.Value;
            return HR.S_OK;
        }

        public int GetServer(out IDebugCoreServer2 ppServer)
        {
            throw new NotImplementedException();
        }

        public int Terminate()
        {
            throw new NotImplementedException();
        }

        int IDebugProcessSecurity2.QueryCanSafelyAttach()
        {
            return HR.S_OK;
        }

        int IDebugProcessSecurity2.GetUserName(out string userName)
        {
            if (string.IsNullOrEmpty(_userName))
            {
                userName = null;
                return HR.S_FALSE;
            }

            userName = _userName;
            return HR.S_OK;
        }

        int IDebugProcessEx2.Attach(IDebugSession2 pSession)
        {
            return HR.S_OK;
        }

        int IDebugProcessEx2.Detach(IDebugSession2 pSession)
        {
            _program = null;
            return HR.S_OK;
        }

        int IDebugProcessEx2.AddImplicitProgramNodes(ref Guid guidLaunchingEngine, Guid[] engineFilter, uint celtSpecificEngines)
        {
            // The SSH port supplier expects the engine to do all the real work for attach. Add a basic program
            // object so that the SDM will know to invoke the engine to have it attach.

            Debug.Assert(_program == null, "AddImplicitProgramNodes should probably only be called once");
            if (guidLaunchingEngine == Guid.Empty && engineFilter != null && engineFilter.Length > 0)
            {
                _program = new AD7Program(this, engineFilter[0]);
            }

            return HR.S_OK;
        }

        string IDebugUnixProcess.GetProcessArchitecture()
        {
            // For Apple Silicon M1, it is possible that the process we are attaching to is being emulated as x86_64. 
            // The process is emulated if it has process flags has P_TRANSLATED (0x20000).
            if (_port.IsOSX() && _systemArch == "arm64")
            {
                if ((_flags & 0x20000) != 0)
                {
                    return "x86_64";
                }
                else
                {
                    return "arm64";
                }
            }
            else
            {
                // Process architecture is the system architecture.
                return _systemArch;
            }
        }
    }
}