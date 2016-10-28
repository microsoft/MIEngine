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
using Microsoft.DebugEngineHost;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Linux.ConnectionManager;

namespace Microsoft.SSHDebugPS
{
    internal class ConnectionManager
    {
        internal static Connection GetInstance(string name, ConnectionReason reason)
        {
            UnixSystem remoteSystem = null;
            ConnectionInfoStore store = new ConnectionInfoStore();
            ConnectionInfo connectionInfo = null;

            StoredConnectionInfo storedConnectionInfo = store.Connections.FirstOrDefault(connection =>
                {
                    return name.Equals(GetFormattedConnectionName((ConnectionInfo)connection), StringComparison.OrdinalIgnoreCase);
                });

            if (storedConnectionInfo != null)
                connectionInfo = (ConnectionInfo)storedConnectionInfo;

            if (connectionInfo == null)
            {
                IVsConnectionManager connectionManager = (IVsConnectionManager)ServiceProvider.GlobalProvider.GetService(typeof(IVsConnectionManager));

                string userName;
                string hostName;

                int atSignIndex = name.IndexOf('@');
                if (atSignIndex > 0)
                {
                    userName = name.Substring(0, atSignIndex);
                    hostName = name.Substring(atSignIndex + 1);
                }
                else
                {
                    userName = string.Format(CultureInfo.CurrentCulture, StringResources.UserName_PlaceHolder);
                    hostName = name;
                }

                PasswordConnectionInfo newConnectionInfo = new PasswordConnectionInfo(hostName, userName, new System.Security.SecureString());

                IConnectionManagerResult result = connectionManager.ShowDialog(newConnectionInfo);

                if ((result.DialogResult & ConnectionManagerDialogResult.Succeeded) == ConnectionManagerDialogResult.Succeeded)
                {
                    // Retrieve the newly added connection
                    store.Load();
                    connectionInfo = store.Connections.First(info => info.Id == result.StoredConnectionId);
                }
            }

            if (connectionInfo != null)
            {
                remoteSystem = new UnixSystem();

                while (true)
                {
                    try
                    {
                        VSOperationWaiter.Wait(string.Format(CultureInfo.CurrentCulture, StringResources.WaitingOp_Connecting, name), throwOnCancel: false, action: () =>
                        {
                            remoteSystem.Connect(connectionInfo);
                        });
                        break;
                    }
                    catch (RemoteAuthenticationException)
                    {
                        IVsConnectionManager connectionManager = (IVsConnectionManager)ServiceProvider.GlobalProvider.GetService(typeof(IVsConnectionManager));
                        IConnectionManagerResult result = connectionManager.ShowDialog(StringResources.AuthenticationFailureHeader, StringResources.AuthenticationFailureDescription, connectionInfo);

                        if ((result.DialogResult & ConnectionManagerDialogResult.Succeeded) == ConnectionManagerDialogResult.Succeeded)
                        {
                            connectionInfo = result.ConnectionInfo;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    catch (Exception ex)
                    {
                        VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, ex.Message, null,
                            OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        return null;
                    }
                }

                // NOTE: This will be null if connect is canceled
                if (remoteSystem != null)
                {
                    return new Connection(remoteSystem);
                }
            }

            return null;
        }

        internal static string GetFormattedConnectionName(ConnectionInfo connectionInfo)
        {
            return connectionInfo.UserName + "@" + connectionInfo.HostNameOrAddress;
        }
    }
}