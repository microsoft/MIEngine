// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private int _sessionRefCount;
        private int _activeAsyncCommands;

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
            lock (_lock)
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
                lock (_lock)
                {
                    return _connection != null;
                }
            }
        }

        public int EnumProcesses(out IEnumDebugProcesses2 processEnum)
        {
            IDebugProcess2[] processes = EnumProcessesInternal();
            processEnum = new AD7ProcessEnum(processes);

            return HR.S_OK;
        }

        private AD7Process[] EnumProcessesInternal()
        {
            var connection = GetConnection();
            if (connection == null)
            {
                // Don't return a failure to prevent vsdebug.dll from showing an error message
                return Array.Empty<AD7Process>();
            }

            AD7Process[] result = null;
            VS.VSOperationWaiter.Wait(StringResources.WaitingOp_ExecutingPS, throwOnCancel: true, action: (cancellationToken) =>
            {
                List<Process> processList = connection.ListProcesses();
                result = processList.Select((proc) => new AD7Process(this, proc)).ToArray();
            });

            CloseConnectionIfIdle();

            return result;
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

        public int GetProcess(AD_PROCESS_ID ad7ProcessId, out IDebugProcess2 ad7Processs)
        {
            // This method is called if a request is made to attach to a process using LaunchDebugTargets. It is
            // not used by the attach to process dialog.

            if (ad7ProcessId.ProcessIdType != (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_SYSTEM)
            {
                throw new NotImplementedException();
            }
            uint processId = ad7ProcessId.dwProcessId;

            AD7Process[] processes = EnumProcessesInternal();
            AD7Process process = processes.FirstOrDefault((x) => x.Id == processId);
            if (process == null)
            {
                ad7Processs = null;
                return HR.E_PROCESS_DESTROYED;
            }

            ad7Processs = process;
            return HR.S_OK;
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
            var wrappedCallback = new AsyncCommandCallback(this, callback);
            var connection = GetConnection();
            lock (_lock)
            {
                _activeAsyncCommands++;
            }
            connection.BeginExecuteAsyncCommand(commandText, runInShell, wrappedCallback, out asyncCommand);
            asyncCommand = new AsyncCommandWrapper(this, asyncCommand);
        }

        private class AsyncCommandWrapper : IDebugUnixShellAsyncCommand
        {
            private readonly AD7Port _port;
            private readonly IDebugUnixShellAsyncCommand _inner;
            private int _notified;

            public AsyncCommandWrapper(AD7Port port, IDebugUnixShellAsyncCommand inner)
            {
                _port = port;
                _inner = inner;
            }

            public void Write(string text) => _inner.Write(text);
            public void WriteLine(string text) => _inner.WriteLine(text);

            public void Abort()
            {
                _inner.Abort();
                // If OnExit didn't fire (abort path), notify the port now
                if (Interlocked.CompareExchange(ref _notified, 1, 0) == 0)
                {
                    _port.OnAsyncCommandExited();
                }
            }
        }

        private void OnAsyncCommandExited()
        {
            lock (_lock)
            {
                _activeAsyncCommands--;
            }
            CloseConnectionIfIdle();
        }

        private void CloseConnectionIfIdle()
        {
            lock (_lock)
            {
                if (_sessionRefCount > 0 || _activeAsyncCommands > 0 || _connection == null)
                {
                    return;
                }

                var conn = _connection;
                _connection = null;
                try { conn.Close(); } catch (Exception) { }
            }
        }

        /// <summary>
        /// Wraps an IDebugUnixShellCommandCallback to detect when an async command exits,
        /// allowing the port to close idle connections for engines that do not call
        /// IDebugPortCleanup.AddSessionRef/Clean (e.g., vsdbg).
        /// </summary>
        private class AsyncCommandCallback : IDebugUnixShellCommandCallback
        {
            private readonly AD7Port _port;
            private readonly IDebugUnixShellCommandCallback _inner;

            public AsyncCommandCallback(AD7Port port, IDebugUnixShellCommandCallback inner)
            {
                _port = port;
                _inner = inner;
            }

            public void OnOutputLine(string line)
            {
                _inner.OnOutputLine(line);
            }

            public void OnExit(string exitCode)
            {
                _inner.OnExit(exitCode);
                _port.OnAsyncCommandExited();
            }
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
                throw new ArgumentOutOfRangeException(nameof(sinkInterface));
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

        public void AddSessionRef()
        {
            lock (_lock)
            {
                _sessionRefCount++;
            }
            EnsureConnected();
        }

        public void Clean()
        {
            lock (_lock)
            {
                Debug.Assert(_sessionRefCount > 0, "Unbalanced call to Clean -- no matching AddSessionRef");
                _sessionRefCount--;

                if (_sessionRefCount > 0)
                {
                    return;
                }

                var conn = _connection;
                _connection = null;

                try
                {
                    conn?.Close();
                }
                // Dev15 632648: Liblinux sometimes throws exceptions on shutdown - we are shutting down anyways, so ignore to not crash
                catch (Exception) { }
            }
        }
    }
}
