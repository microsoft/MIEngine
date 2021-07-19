// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
                    return string.Equals(name, SSHPortSupplier.GetFormattedSSHConnectionName((ConnectionInfo)connection), StringComparison.OrdinalIgnoreCase);
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
                    hwnd = dte?.MainWindow?.HWnd ?? IntPtr.Zero;
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

        // Default SSH port is 22
        internal const int DefaultSSHPort = 22;
        /// <summary>
        /// Parses the SSH connection string. Expected format is some permutation of username@hostname:portnumber.
        /// If not defined, will provide default values.
        /// </summary>
        internal static void ParseSSHConnectionString(string connectionString, out string userName, out string hostName, out int port)
        {
            userName = StringResources.UserName_PlaceHolder;
            hostName = StringResources.HostName_PlaceHolder;
            port = DefaultSSHPort;

            const string TempUriPrefix = "ssh://";

            try
            {
                // In order for Uri to parse, it needs to have a protocol in front.
                Uri connectionUri = new Uri(TempUriPrefix + connectionString);

                if (!string.IsNullOrWhiteSpace(connectionUri.UserInfo))
                    userName = connectionUri.UserInfo;

                if (!string.IsNullOrWhiteSpace(connectionUri.Host))
                    hostName = connectionUri.Host;

                if (!connectionUri.IsDefaultPort)
                    port = connectionUri.Port;
            }
            catch (UriFormatException)
            { }

            // If Uri sets anything to empty string, set it back to the placeholder
            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = StringResources.UserName_PlaceHolder;
            }

            if (string.IsNullOrWhiteSpace(hostName))
            {
                // handle case that its just a colon by replacing it with the default string
                hostName = StringResources.HostName_PlaceHolder;
            }
        }
    }
}