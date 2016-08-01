// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SSHDebugPS.VS;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Globalization;
using System.Linq;
using liblinux;
using liblinux.Persistence;

namespace Microsoft.SSHDebugPS
{
    internal class ConnectionManager
    {
        internal static Connection GetInstance(string name, ConnectionReason reason)
        {
            UnixSystem remoteSystem = null;
            ConnectionInfoStore store = new ConnectionInfoStore();
            StoredConnectionInfo storedConnectionInfo = store.Connections.FirstOrDefault(
                connection => name.Equals(((ConnectionInfo)connection).HostNameOrAddress, StringComparison.OrdinalIgnoreCase));

            if (storedConnectionInfo != null)
            {
                remoteSystem = new UnixSystem();
                try
                {
                    VSOperationWaiter.Wait(string.Format(CultureInfo.CurrentCulture, StringResources.WaitingOp_Connecting, name), throwOnCancel: false, action: () =>
                    {
                        remoteSystem.Connect((ConnectionInfo)storedConnectionInfo);
                    });
                }
                catch (Exception ex)
                {
                    VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, ex.Message, null, 
                        OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return null;
                }

                // NOTE: This will be null if connect is canceled
                if (remoteSystem != null)
                {
                    return new Connection(remoteSystem);
                }
            }
            else
            {
                // TODO: Replace this with credentials dialog from Linux extension
                Connection connection = null;

                try
                {
                    connection = VS.CredentialsDialog.Show(name, reason);
                    store.Add(connection.ConnectionInfo);
                }
                catch (AD7ConnectCanceledException)
                {
                    return null;
                }

                return connection;
            }

            return null;
        }
    }
}