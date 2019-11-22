// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Interop;
using EnvDTE;
using liblinux;
using liblinux.Persistence;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.SSH;
using Microsoft.SSHDebugPS.UI;
using Microsoft.SSHDebugPS.Utilities;
using Microsoft.VisualStudio.Linux.ConnectionManager;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.SSHDebugPS
{
    internal class ConnectionManager
    {
        public static DockerConnection GetDockerConnection(string name, bool supportSSHConnections)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            DockerContainerTransportSettings settings;
            Connection remoteConnection;

            if (!DockerConnection.TryConvertConnectionStringToSettings(name, out settings, out remoteConnection) || settings == null)
            {
                string connectionString;

                bool success = ShowContainerPickerWindow(IntPtr.Zero, supportSSHConnections, out connectionString);
                if (success)
                {
                    success = DockerConnection.TryConvertConnectionStringToSettings(connectionString, out settings, out remoteConnection);
                }

                if (!success || settings == null)
                {
                    VSMessageBoxHelper.PostErrorMessage(StringResources.Error_ContainerConnectionStringInvalidTitle, StringResources.Error_ContainerConnectionStringInvalidMessage);
                    return null;
                }
            }

            string displayName = DockerConnection.CreateConnectionString(settings.ContainerName, remoteConnection?.Name, settings.HostName);
            if (DockerHelper.IsContainerRunning(settings.HostName, settings.ContainerName, remoteConnection))
            {
                return new DockerConnection(settings, remoteConnection, displayName);
            }
            else
            {
                VSMessageBoxHelper.PostErrorMessage(
                   StringResources.Error_ContainerUnavailableTitle,
                   StringResources.Error_ContainerUnavailableMessage.FormatCurrentCultureWithArgs(settings.ContainerName));
                return null;
            }
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
                    ParseSSHConnectionString(name, out string userName, out string hostName, out int port);

                    result = connectionManager.ShowDialog(new PasswordConnectionInfo(hostName, port, Timeout.InfiniteTimeSpan, userName, new System.Security.SecureString()));
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

        /// <summary>
        /// Open the ContainerPickerDialog
        /// </summary>
        /// <param name="hwnd">Parent hwnd or IntPtr.Zero</param>
        /// <param name="supportSSHConnections">SSHConnections are supported</param>
        /// <param name="connectionString">[out] connection string obtained by the dialog</param>
        public static bool ShowContainerPickerWindow(IntPtr hwnd, bool supportSSHConnections, out string connectionString)
        {
            ThreadHelper.ThrowIfNotOnUIThread("Microsoft.SSHDebugPS.ShowContainerPickerWindow");
            ContainerPickerDialogWindow dialog = new ContainerPickerDialogWindow(supportSSHConnections);

            if (hwnd == IntPtr.Zero) // get the VS main window hwnd
            {
                try
                {
                    // parent to the global VS window
                    DTE dte = (DTE)Package.GetGlobalService(typeof(SDTE));
                    hwnd = new IntPtr(dte?.MainWindow?.HWnd ?? 0);
                }
                catch // No DTE?
                {
                    Debug.Fail("No DTE?");
                }
            }

            if (hwnd != IntPtr.Zero)
            {
                WindowInteropHelper helper = new WindowInteropHelper(dialog);
                helper.Owner = hwnd;
            }

            bool? dialogResult = dialog.ShowModal();
            if (dialogResult.GetValueOrDefault(false))
            {
                connectionString = dialog.SelectedContainerConnectionString;
                return true;
            }

            connectionString = string.Empty;
            return false;
        }

        /// <summary>
        /// Parses the SSH connection string. Expected format is some permutation of username@hostname:portnumber.
        /// If not defined, will provide default values.
        /// </summary>
        internal static void ParseSSHConnectionString(string connectionString, out string userName, out string hostName, out int port)
        {
            userName = StringResources.UserName_PlaceHolder;
            hostName = StringResources.HostName_PlaceHolder;
            port = 22; // Default SSH port is 22

            int atSignIndex = connectionString.IndexOf('@');
            if (atSignIndex >= 0)
            {
                // Find if a username is specified
                if (atSignIndex > 0)
                {
                    userName = connectionString.Substring(0, atSignIndex);
                }

                // Find the beginning of the hostname section. 
                // Handle where the first character is '@'
                int hostNameStartPos = atSignIndex + 1;
                if (hostNameStartPos < connectionString.Length)
                {
                    hostName = connectionString.Substring(hostNameStartPos);
                }
                else
                {
                    // Using default hostName so we don't need to look for the port
                    return;
                }
            }
            else
            {
                hostName = connectionString;
            }

            // Find if a port is specified. Handle possible IPV6 by grabbing the last colon
            int lastColonIndex = hostName.LastIndexOf(':');
            if (lastColonIndex >= 0)
            {
                int portStartPos = lastColonIndex + 1;
                if (lastColonIndex > 0 && portStartPos < hostName.Length)
                {
                    string portString = hostName.Substring(portStartPos);
                    int tempPort;
                    if (Int32.TryParse(portString, out tempPort) && tempPort != port)
                    {
                        port = tempPort;
                    }
                }

                // remove everything after last ':'
                hostName = hostName.Substring(0, lastColonIndex);
                if(string.IsNullOrWhiteSpace(hostName))
                {
                    // handle case that its just a colon by replacing it with the default string
                    hostName = StringResources.HostName_PlaceHolder;
                }
            }
        }
    }
}