// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.SSHDebugPS.Utilities;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.SSHDebugPS
{
    internal abstract class AD7Port : IDebugPort2, IDebugUnixShellPort, IDebugPortCleanup, IConnectionPointContainer, IConnectionPoint
    {
        private readonly object _lock = new object();
        private readonly AD7PortSupplier _portSupplier;
        private readonly Lazy<Guid> _id = new Lazy<Guid>(() => Guid.NewGuid(), LazyThreadSafetyMode.ExecutionAndPublication);
        private Connection _connection;
        private readonly Dictionary<uint, IDebugPortEvents2> _eventCallbacks = new Dictionary<uint, IDebugPortEvents2>();
        private uint _lastCallbackCookie;

        protected string Name { get; private set; }

        public AD7Port(AD7PortSupplier portSupplier, string name, bool isInAddPort)
        {
            _portSupplier = portSupplier;
            Name = name;

            if (isInAddPort)
            {
                GetConnection();
            }
        }

        protected Connection GetConnection()
        {
            if (_connection == null)
            {
                _connection = GetConnectionInternal();
                if (_connection != null)
                {
                    Name = _connection.Name;
                }
            }

            return _connection;
        }

        protected abstract Connection GetConnectionInternal();

        public void EnsureConnected()
        {
            GetConnection();
        }

        public bool IsConnected
        {
            get
            {
                return _connection != null;
            }
        }

        public int EnumProcesses(out IEnumDebugProcesses2 processEnum)
        {
            int hr = HR.S_OK;
            IEnumDebugProcesses2 result = null;
            var connection = GetConnection();

            if (connection == null)
            {
                // Don't return a failure to prevent vsdebug.dll from showing an error message
                processEnum = new AD7ProcessEnum(Array.Empty<IDebugProcess2>());
                return HR.S_OK;
            }

            VS.VSOperationWaiter.Wait(StringResources.WaitingOp_ExecutingPS, throwOnCancel: true, action: (cancellationToken) =>
            {
                List<Process> processList = connection.ListProcesses();
                IDebugProcess2[] processes = processList.Select((proc) => new AD7Process(this, proc)).ToArray();
                result = new AD7ProcessEnum(processes);
            });

            processEnum = result;
            return hr;
        }

        public int GetPortId(out Guid guidPort)
        {
            guidPort = _id.Value;
            return HR.S_OK;
        }

        public int GetPortName(out string name)
        {
            name = Name;
            return HR.S_OK;
        }

        public int GetPortRequest(out IDebugPortRequest2 ppRequest)
        {
            throw new NotImplementedException();
        }

        public int GetPortSupplier(out IDebugPortSupplier2 portSupplier)
        {
            portSupplier = _portSupplier;
            return HR.S_OK;
        }

        public int GetProcess(AD_PROCESS_ID ProcessId, out IDebugProcess2 ppProcess)
        {
            throw new NotImplementedException();
        }

        void IDebugUnixShellPort.ExecuteSyncCommand(string commandDescription, string commandText, out string commandOutput, int timeout, out int exitCode)
        {
            int code = -1;
            string output = null;
            string errorMessage;

            string waitPrompt = StringResources.WaitingOp_ExecutingCommand.FormatCurrentCultureWithArgs(commandDescription);
            VS.VSOperationWaiter.Wait(waitPrompt, throwOnCancel: true, action: (cancellationToken) =>
            {
                code = GetConnection().ExecuteCommand(commandText, timeout, out output, out errorMessage);
            });

            exitCode = code;
            commandOutput = output;
        }

        void IDebugUnixShellPort.BeginExecuteAsyncCommand(string commandText, bool runInShell, IDebugUnixShellCommandCallback callback, out IDebugUnixShellAsyncCommand asyncCommand)
        {
            GetConnection().BeginExecuteAsyncCommand(commandText, runInShell, callback, out asyncCommand);
        }

        void IConnectionPointContainer.EnumConnectionPoints(out IEnumConnectionPoints ppEnum)
        {
            throw new NotImplementedException();
        }

        void IConnectionPointContainer.FindConnectionPoint(ref Guid iid, out IConnectionPoint connectionPoint)
        {
            if (iid != typeof(IDebugPortEvents2).GUID)
            {
                throw new NotImplementedException();
            }

            connectionPoint = this;
        }

        void IConnectionPoint.GetConnectionInterface(out Guid iid)
        {
            iid = typeof(IDebugPortEvents2).GUID;
        }

        void IConnectionPoint.GetConnectionPointContainer(out IConnectionPointContainer connectionPointContainer)
        {
            connectionPointContainer = this;
        }

        void IConnectionPoint.Advise(object sinkInterface, out uint cookie)
        {
            IDebugPortEvents2 eventCallback = sinkInterface as IDebugPortEvents2;
            if (eventCallback == null)
            {
                throw new ArgumentOutOfRangeException("sinkIterface");
            }

            lock (_lock)
            {
                _lastCallbackCookie++;
                if (_lastCallbackCookie == 0)
                {
                    _lastCallbackCookie++;
                }
                _eventCallbacks.Add(_lastCallbackCookie, eventCallback);
                cookie = _lastCallbackCookie;
            }
        }

        void IConnectionPoint.Unadvise(uint cookie)
        {
            lock (_lock)
            {
                _eventCallbacks.Remove(cookie);
            }
        }

        void IConnectionPoint.EnumConnections(out IEnumConnections ppEnum)
        {
            throw new NotImplementedException();
        }

        public void CopyFile(string sourcePath, string destinationPath)
        {
            GetConnection().CopyFile(sourcePath, destinationPath);
        }

        public string MakeDirectory(string path)
        {
            return GetConnection().MakeDirectory(path);
        }

        public string GetUserHomeDirectory()
        {
            return GetConnection().GetUserHomeDirectory();
        }

        public bool IsOSX()
        {
            return GetConnection().IsOSX();
        }

        public bool IsLinux()
        {
            return GetConnection().IsLinux();
        }

        public void Clean()
        {
            try
            {
                _connection?.Close();
            }
            // Dev15 632648: Liblinux sometimes throws exceptions on shutdown - we are shutting down anyways, so ignore to not crash
            catch (Exception) { }
        }
    }
}
