// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.SSHDebugPS.Docker;
using Microsoft.SSHDebugPS.SSH;
using Microsoft.SSHDebugPS.Utilities;

namespace Microsoft.SSHDebugPS.Podman
{
    public class PodmanHelper
    {
        private const string podmanPSCommand = "ps";
        private const string podmanPSArgs = "-f status=running --no-trunc --format \"{{json .}}\"";


        internal static IEnumerable<PodmanContainerInstance> GetLocalPodmanContainers(string hostname, out int totalContainers)
        {
            totalContainers = 0;
            int containerCount = 0;
            List<PodmanContainerInstance> containers = new List<PodmanContainerInstance>();

            PodmanCommandSettings settings = new PodmanCommandSettings(hostname, false);
            settings.SetCommand(podmanPSCommand, podmanPSArgs);

            DockerHelper.RunContainerCommand(settings, delegate (string args)
            {
                if (args.Trim()[0] == '{')
                {
                    if (PodmanContainerInstance.TryCreate(args, out PodmanContainerInstance containerInstance))
                    {
                        containers.Add(containerInstance);
                    }
                    containerCount++;
                }
            });

            totalContainers = containerCount;
            return containers;
        }

        /// <summary>
        /// Checks if the specified container is in the list of containers from the target host.
        /// </summary>
        internal static bool IsContainerRunning(string hostName, string containerName, Connection remoteConnection)
        {
            IEnumerable<PodmanContainerInstance> containers;
            if (remoteConnection != null)
            {
                containers = GetRemotePodmanContainers(remoteConnection, hostName, out _);
            }
            else
            {
                containers = GetLocalPodmanContainers(hostName, out _);
            }

            if (containers != null)
            {
                if (containers.Any(container => string.Equals(container.Name, containerName, StringComparison.Ordinal)
                        || container.Id.StartsWith(containerName, StringComparison.Ordinal)))
                {
                    return true;
                }
            }

            return false;
        }

        internal static IEnumerable<PodmanContainerInstance> GetRemotePodmanContainers(IConnection connection, string hostname, out int totalContainers)
        {
            totalContainers = 0;
            SSHConnection sshConnection = connection as SSHConnection;
            List<string> outputLines = new List<string>();
            StringBuilder errorSB = new StringBuilder();
            if (sshConnection == null)
            {
                return null;
            }

            List<PodmanContainerInstance> containers = new List<PodmanContainerInstance>();

            PodmanCommandSettings settings = new PodmanCommandSettings(hostname, true);
            settings.SetCommand(podmanPSCommand, podmanPSArgs);

            RemoteCommandRunner commandRunner = new RemoteCommandRunner(settings, sshConnection, handleRawOutput: false);

            ManualResetEvent resetEvent = new ManualResetEvent(false);
            int exitCode = 0;
            commandRunner.ErrorOccured += ((sender, args) =>
            {
                errorSB.Append(args);
            });

            commandRunner.Closed += ((sender, args) =>
            {
                exitCode = args;
                resetEvent.Set();
            });

            commandRunner.OutputReceived += ((sender, line) =>
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Debug.Assert(line.IndexOf('\n') < 0, "Why does `line` have embedded newline characters?");

                    if (line.Trim()[0] != '{')
                    {
                        errorSB.Append(line);
                    }

                    outputLines.Add(line);
                }
            });

            commandRunner.Start();

            bool cancellationRequested = false;
            VS.VSOperationWaiter.Wait(UIResources.QueryingForContainersMessage, false, (cancellationToken) =>
            {
                while (!resetEvent.WaitOne(2000) && !cancellationToken.IsCancellationRequested)
                { }
                cancellationRequested = cancellationToken.IsCancellationRequested;
            });

            if (!cancellationRequested)
            {
                if (exitCode != 0)
                {
                    string exceptionMessage = UIResources.CommandExecutionErrorWithExitCodeFormat.FormatCurrentCultureWithArgs(
                            "{0} {1}".FormatInvariantWithArgs(settings.Command, settings.CommandArgs),
                            exitCode,
                            errorSB.ToString());

                    throw new CommandFailedException(exceptionMessage);
                }

                foreach (var item in outputLines)
                {
                    if (PodmanContainerInstance.TryCreate(item, out PodmanContainerInstance containerInstance))
                    {
                        containers.Add(containerInstance);
                        totalContainers++;
                    }
                }
            }

            return containers;
        }
    }
}
