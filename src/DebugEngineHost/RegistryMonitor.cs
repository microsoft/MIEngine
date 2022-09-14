// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace Microsoft.DebugEngineHost
{
    internal class RegistryMonitor : IDisposable
    {
        #region Native Methods

        /// <summary>
        /// Filter for notifications reported by <see cref="RegistryMonitor"/>.
        /// </summary>
        [Flags]
        public enum RegChangeNotifyFilter
        {
            /// <summary>Notify the caller if a subkey is added or deleted.</summary>
            Key = 1,
            /// <summary>Notify the caller of changes to the attributes of the key,
            /// such as the security descriptor information.</summary>
            Attribute = 2,
            /// <summary>Notify the caller of changes to a value of the key. This can
            /// include adding or deleting a value, or changing an existing value.</summary>
            Value = 4,
            /// <summary>Notify the caller of changes to the security descriptor
            /// of the key.</summary>
            Security = 8,
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegOpenKeyEx(IntPtr hKey, string subKey, uint options, int samDesired,
                                               out IntPtr phkResult);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree,
                                                          RegChangeNotifyFilter dwNotifyFilter, IntPtr hEvent,
                                                          bool fAsynchronous);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegCloseKey(IntPtr hKey);

        private const int KEY_QUERY_VALUE = 0x0001;
        private const int KEY_NOTIFY = 0x0010;
        private const int STANDARD_RIGHTS_READ = 0x00020000;

        private static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));

        #endregion

        private HostConfigurationSection _section;
        private readonly bool _watchSubtree;

        // Set when registry value is changed
        private AutoResetEvent m_changeEvent;

        // Set when monitoring is stopped
        private AutoResetEvent m_stoppedEvent;

        /// <summary>
        /// Occurs when the specified registry key has changed.
        /// </summary>
        public event EventHandler RegChanged;

        public RegistryMonitor(HostConfigurationSection section, bool watchSubtree)
        {
            _section = section;
            _watchSubtree = watchSubtree;
        }

        public void Start()
        {
            Thread registryMonitor = new Thread(Monitor);
            registryMonitor.IsBackground = true;
            registryMonitor.Name = "RegistryMonitor";
            registryMonitor.Start();
        }

        public void Stop()
        {
            if (m_stoppedEvent != null)
            {
                m_stoppedEvent.Set();
            }
        }

        // The handle is owned by change event instance which lives while we use the handle.
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
        private void Monitor()
        {
            bool stopped = false;

            m_stoppedEvent = new AutoResetEvent(false);
            m_changeEvent = new AutoResetEvent(false);

            IntPtr handle = m_changeEvent.SafeWaitHandle.DangerousGetHandle();

            int errorCode = RegNotifyChangeKeyValue(_section.YOLOHandle, _watchSubtree, RegChangeNotifyFilter.Key | RegChangeNotifyFilter.Attribute | RegChangeNotifyFilter.Value | RegChangeNotifyFilter.Security, handle, true);
            if (errorCode != 0) // 0 is ERROR_SUCCESS
            {
                throw new Win32Exception(errorCode);
            }
            try
            {
                while (!stopped)
                {
                    int waitResult = WaitHandle.WaitAny(new WaitHandle[] { m_stoppedEvent, m_changeEvent });

                    if (waitResult == 0)
                    {
                        stopped = true;
                    }
                    else
                    {
                        errorCode = RegNotifyChangeKeyValue(_section.YOLOHandle, _watchSubtree, RegChangeNotifyFilter.Key | RegChangeNotifyFilter.Attribute | RegChangeNotifyFilter.Value | RegChangeNotifyFilter.Security, handle, true);
                        if (errorCode != 0) // 0 is ERROR_SUCCESS
                        {
                            throw new Win32Exception(errorCode);
                        }
                        RegChanged.Invoke(this, null);
                    }
                }
            }
            finally
            {
                _section.Dispose();
                m_stoppedEvent?.Dispose();
                m_changeEvent?.Dispose();
            }
        }

        public void Dispose()
        {
            m_stoppedEvent.Set();
        }
    }
}
