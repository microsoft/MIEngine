// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using liblinux;
using liblinux.Persistence;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.SSH;
using Microsoft.VisualStudio.Linux.ConnectionManager;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.SSHDebugPS
{
    internal class ConnectionManager
    {
        public static DockerConnection GetDockerConnection(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

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
                settings = new DockerExecShellSettings(containerName, hostIsUnix: false);
                displayName = name;
                // TODO: Verify container exists on local machine
            }
            else if (connectionStrings.Length == 2)
            {
                // SSH connection
                string remoteConnectionString = connectionStrings[0];
                containerName = connectionStrings[1];
                remoteConnection = GetSSHConnection(remoteConnectionString);

                // If SSH connection dialog was cancelled, we should cancel this connection.
                if (remoteConnection == null)
                    return null;

                // TODO: Verify container exists on remote machine.
                //string output;
                //int exitCode;
                //remoteConnection.ExecuteSyncCommand("verify docker exists", $"docker ps -f {containerName} --filter {{.Names}}", out output, , out exitCode);

                settings = new DockerExecShellSettings(containerName, hostIsUnix: true); // assume all remote is Unix for now.
                displayName = remoteConnection.Name + '/' + containerName;
            }
            else
            {
                // TODO: This will be replaced by the docker container selection dialog 
                return null;
            }

            return new DockerConnection(settings, remoteConnection, displayName, containerName);
        }

        public static SSHConnection GetSSHConnection(string name)
        {
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
                IConnectionManagerResult result;
                if (string.IsNullOrWhiteSpace(name))
                {
                    result = connectionManager.ShowDialog();
                }
                else
                {
                    string userName;
                    string hostName;

                    int atSignIndex = name.IndexOf('@');
                    if (atSignIndex > 0)
                    {
                        userName = name.Substring(0, atSignIndex);

                        int hostNameStartPos = atSignIndex + 1;
                        hostName = hostNameStartPos <  name.Length ? name.Substring(hostNameStartPos) : StringResources.HostName_PlaceHolder;
                    }
                    else
                    {
                        userName = string.Format(CultureInfo.CurrentCulture, StringResources.UserName_PlaceHolder);
                        hostName = name;
                    }
                    result = connectionManager.ShowDialog(new PasswordConnectionInfo(hostName, userName, new System.Security.SecureString()));
                }

                if ((result.DialogResult & ConnectionManagerDialogResult.Succeeded) == ConnectionManagerDialogResult.Succeeded)
                {
                    // Retrieve the newly added connection
                    store.Load();
                    connectionInfo = store.Connections.First(info => info.Id == result.StoredConnectionId);
                }
            }

            return SSHHelper.CreateSSHConnectionFromConnectionInfo(connectionInfo);
        }
    }
}