// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
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
        public static DockerConnection GetDockerConnection(string name)
        {
            DockerConnection connection = null;

            if (!string.IsNullOrWhiteSpace(name))
            {
                connection = DockerConnection.Deserialize(name) as DockerConnection;
            }

            if (connection == null)
            {
                string connectionString;
                if (ConnectionManager.ShowContainerPickerWindow(IntPtr.Zero, out connectionString))
                {
                    connection =  DockerConnection.Deserialize(connectionString);
                    if (connection == null)
                    {
                        //show error message and return 
                    }
                }
                else // User canceled
                {
                    return null;
                }
            }
            
            return connection;
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
                        hostName = hostNameStartPos < name.Length ? name.Substring(hostNameStartPos) : StringResources.HostName_PlaceHolder;
                    }
                    else
                    {
                        userName = StringResources.UserName_PlaceHolder;
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

        /// <summary>
        /// Open the ContainerPickerDialog
        /// </summary>
        /// <param name="hwnd">Parent hwnd or IntPtr.Zero</param>
        /// <param name="connectionString">[out] connection string obtained by the dialog</param>
        /// <returns></returns>
        public static bool ShowContainerPickerWindow(IntPtr hwnd, out string connectionString)
        {
            ThreadHelper.ThrowIfNotOnUIThread("Microsoft.SSHDebugPS.ShowContainerPickerWindow");
            ContainerPickerDialogWindow dialog = new ContainerPickerDialogWindow();

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
    }
}