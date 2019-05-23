// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using liblinux;
using liblinux.Persistence;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.SSH;
using Microsoft.SSHDebugPS.VS;
using Microsoft.VisualStudio.Linux.ConnectionManager;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

                // Verify container exists on remote machine.
                //string output;
                //int exitCode;
                //remoteConnection.ExecuteSyncCommand("verify docker exists", $"docker ps -f {containerName} --filter {{.Names}}", out output, , out exitCode);

                settings = new DockerExecShellSettings(containerName, hostIsUnix: true); // assume all remote is Unix for now.
                displayName = remoteConnection.Name + '/' + containerName;
            }
            else
            {
                // This will be replaced by the docker container selection dialog 
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
                        hostName = name.Substring(atSignIndex + 1);
                    }
                    else
                    {
                        userName = string.Format(CultureInfo.CurrentCulture, StringResources.UserName_PlaceHolder);
                        hostName = name;
                    }

                    PasswordConnectionInfo newConnectionInfo = new PasswordConnectionInfo(hostName, userName, new System.Security.SecureString());
                    result = connectionManager.ShowDialog(newConnectionInfo);
                }

                if ((result.DialogResult & ConnectionManagerDialogResult.Succeeded) == ConnectionManagerDialogResult.Succeeded)
                {
                    // Retrieve the newly added connection
                    store.Load();
                    connectionInfo = store.Connections.First(info => info.Id == result.StoredConnectionId);
                }
            }

            return CreateSSHConnectionFromConnectionInfo(connectionInfo);
        }

        public static SSHConnection CreateSSHConnectionFromConnectionInfo(ConnectionInfo connectionInfo)
        {
            if (connectionInfo != null)
            {
                UnixSystem remoteSystem = new UnixSystem();
                string name = SSHPortSupplier.GetFormattedSSHConnectionName(connectionInfo);

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

        public static IEnumerable<ConnectionInfo> GetAvailableSSHConnectionInfos()
        {
            ConnectionInfoStore store = new ConnectionInfoStore();

            return store.Connections.ToList().Select(item => (ConnectionInfo)item);
        }

        private const string dockerCommand = "docker";
        // --no-trunc avoids parameter truncation
        private const string dockerPSArgs = "ps --no-trunc --format \"{{json .}}\"";
        public static IEnumerable<IContainerInstance> GetLocalDockerContainers()
        {
            List<DockerContainerInstance> containers = new List<DockerContainerInstance>();
            LocalSingleCommandRunner commandRunner = new LocalSingleCommandRunner(dockerCommand, dockerPSArgs);
            StringBuilder errorSB = new StringBuilder();
            int exitCode = 0;

            try
            {
                ManualResetEvent resetEvent = new ManualResetEvent(false);
                commandRunner.ErrorOccured += ((sender, args) =>
                {
                    resetEvent.Set();
                });

                commandRunner.Closed += ((sender, args) =>
                {
                    exitCode = args;
                    resetEvent.Set();
                });

                commandRunner.OutputReceived += ((sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args))
                    {
                        if (args.Trim()[0] != '{')
                        {
                            // output isn't json, command Error
                            string errorMessage = string.Format(CultureInfo.CurrentCulture, UIResources.CommandExecutionErrorFormat, dockerCommand, args);
                            throw new CommandFailedException(errorMessage);
                        }

                        var containerInstance = DockerContainerInstance.Create(args);
                        if (containerInstance != null)
                            containers.Add(containerInstance);
                    }
                });

                commandRunner.Run();
                resetEvent.WaitOne();

                // might need to throw an exception here too??
                if (exitCode != 0)
                {
                    Debug.Fail($"Exit Code: {exitCode}");
                    return null;
                }

                return containers;
            }
            catch (Win32Exception ex)
            {
                // docker doesn't exist 
                string errorMessage = string.Format(CultureInfo.CurrentCulture, UIResources.CommandExecutionErrorFormat, dockerCommand, ex.Message);
                throw new CommandFailedException(errorMessage, ex);
            }
        }

        public static IEnumerable<IContainerInstance> GetRemoteDockerContainers(IConnection connection)
        {
            SSHConnection sshConnection = connection as SSHConnection;
            List<string> outputLines = new List<string>();
            StringBuilder errorSB = new StringBuilder();
            if (sshConnection == null)
            {
                return null;
            }

            List<DockerContainerInstance> containers = new List<DockerContainerInstance>();
            RemoteCommandRunner commandRunner = new RemoteCommandRunner(dockerCommand, dockerPSArgs, sshConnection);

            ManualResetEvent resetEvent = new ManualResetEvent(false);
            int exitCode = 0;
            commandRunner.ErrorOccured += ((sender, args) =>
            {
                resetEvent.Set();
            });

            commandRunner.Closed += ((sender, args) =>
            {
                exitCode = args;
                resetEvent.Set();
            });

            commandRunner.OutputReceived += ((sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args))
                {
                    // If it isn't json, assume its an error message
                    if (args.Trim()[0] != '{')
                    {
                        errorSB.Append(args);
                    }

                    // Unix line endings are '\n' so split on that for json items.
                    foreach (var item in args.Split('\n').ToList())
                    {
                        if (!string.IsNullOrWhiteSpace(item))
                            outputLines.Add(item);
                    }
                }
            });

            resetEvent.WaitOne();

            if (exitCode != 0)
            {
                // if the exit code is not zero, then the output we received possibly is the error message
                string exceptionMessage = string.Format(CultureInfo.CurrentCulture,
                    UIResources.CommandExecutionErrorWithExitCodeFormat,
                    dockerCommand,
                    exitCode,
                    errorSB.ToString());

                throw new CommandFailedException(exceptionMessage);
            }

            foreach (var item in outputLines)
            {
                containers.Add(DockerContainerInstance.Create(item));
            }

            return containers;
        }
    }
}