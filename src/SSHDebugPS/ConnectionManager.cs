// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using liblinux;
using liblinux.Persistence;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.SSH;
using Microsoft.SSHDebugPS.VS;
using Microsoft.VisualStudio.Linux.ConnectionManager;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.SSHDebugPS
{
    internal class ConnectionManager
    {
        public static Connection GetDockerConnection(string name)
        {
            Connection remoteConnection = null;
            DockerTransportSettings settings = null;
            // Assume format is <server>/<container> where if <server> is specified, it is for SSH
            string[] connectionStrings = name.Split('/');

            string displayName;
            string containerName;
            if (connectionStrings.Length == 1)
            {
                // local connection
                containerName = connectionStrings[0];
                settings = new DockerExecShellSettings(containerName, isUnix: false);
                displayName = name;
            }
            else if (connectionStrings.Length == 2)
            {
                string remoteConnectionString = connectionStrings[0];
                containerName = connectionStrings[1];
                remoteConnection = GetSSHConnection(remoteConnectionString);

                // If SSH connection dialog was cancelled, we should cancel this connection.
                if (remoteConnection == null)
                    return null;
                
                settings = new DockerExecShellSettings(containerName, isUnix: true); // assume all remote is Unix for now.
                displayName = remoteConnection.Name + '/' + containerName;
            }
            else
            {
                throw new ArgumentException("Argument format is incorrect");
            }

            return new DockerConnection(settings, remoteConnection, name, containerName);
        }

        public static Connection GetSSHConnection(string name)
        {
            UnixSystem remoteSystem = null;
            ConnectionInfoStore store = new ConnectionInfoStore();
            ConnectionInfo connectionInfo = null;

            StoredConnectionInfo storedConnectionInfo = store.Connections.FirstOrDefault(connection =>
                {
                    return name.Equals(SSHPortSupplier.GetFormattedSSHConnectionName((ConnectionInfo)connection), StringComparison.OrdinalIgnoreCase);
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
                    return new SSHConnection(remoteSystem);
                }
            }

            return null;
        }
    }
}