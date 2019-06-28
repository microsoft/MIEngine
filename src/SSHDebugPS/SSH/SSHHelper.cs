// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using liblinux;
using liblinux.Persistence;
using Microsoft.SSHDebugPS.Utilities;
using Microsoft.SSHDebugPS.VS;
using Microsoft.VisualStudio.Linux.ConnectionManager;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.SSHDebugPS.SSH
{
    public class SSHHelper
    {
        internal static SSHConnection CreateSSHConnectionFromConnectionInfo(ConnectionInfo connectionInfo)
        {
            if (connectionInfo != null)
            {
                UnixSystem remoteSystem = new UnixSystem();
                string name = SSHPortSupplier.GetFormattedSSHConnectionName(connectionInfo);

                while (true)
                {
                    try
                    {
                        VSOperationWaiter.Wait(
                            StringResources.WaitingOp_Connecting.FormatCurrentCultureWithArgs(name), 
                            throwOnCancel: false, 
                            action: (cancellationToken) =>
                                remoteSystem.Connect(connectionInfo));
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

        public static IEnumerable<ConnectionInfo> GetAvailableSSHConnectionInfos()
        {
            ConnectionInfoStore store = new ConnectionInfoStore();

            return store.Connections.ToList().Select(item => (ConnectionInfo)item);
        }
    }
}
