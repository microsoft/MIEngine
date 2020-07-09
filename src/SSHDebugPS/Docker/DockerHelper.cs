// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.SSHDebugPS.SSH;
using Microsoft.SSHDebugPS.Utilities;

namespace Microsoft.SSHDebugPS.Docker
{
    public class DockerHelper
    {
        private const string dockerPSCommand = "ps";
        // --no-trunc avoids parameter truncation
        private const string dockerPSArgs = "-f status=running --no-trunc --format \"{{json .}}\"";
        public static IEnumerable<DockerContainerInstance> GetLocalDockerContainers(string hostname, out int totalContainers)
        {
            totalContainers = 0;
            int containerCount = 0;
            List<DockerContainerInstance> containers = new List<DockerContainerInstance>();

            DockerCommandSettings settings = new DockerCommandSettings(hostname, false);
            settings.SetCommand(dockerPSCommand, dockerPSArgs);

            LocalCommandRunner commandRunner = new LocalCommandRunner(settings);

            StringBuilder errorSB = new StringBuilder();
            int? exitCode = null;

            try
            {
                ManualResetEvent resetEvent = new ManualResetEvent(false);
                commandRunner.ErrorOccured += ((sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.ErrorMessage))
                    {
                        errorSB.AppendLine(args.ErrorMessage);
                    }
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
                            errorSB.Append(args);
                        }
                        else
                        {
                            if (DockerContainerInstance.TryCreate(args, out DockerContainerInstance containerInstance))
                            {
                                containers.Add(containerInstance);
                            }
                            containerCount++;
                        }
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
                    // might need to throw an exception here too??
                    if (exitCode.GetValueOrDefault(-1) != 0)
                    {
                        // if the exit code is not zero, then the output we received possibly is the error message
                        string exceptionMessage = UIResources.CommandExecutionErrorWithExitCodeFormat.FormatCurrentCultureWithArgs(
                                "{0} {1}".FormatInvariantWithArgs(settings.Command, settings.CommandArgs),
                                exitCode,
                                errorSB.ToString());

                        throw new CommandFailedException(exceptionMessage);
                    }

                    if (errorSB.Length > 0)
                    {
                        throw new CommandFailedException(errorSB.ToString());
                    }

                    totalContainers = containerCount;
                    return containers;
                }

                return new List<DockerContainerInstance>();
            }
            catch (Win32Exception ex)
            {
                // docker doesn't exist 
                string errorMessage = UIResources.CommandExecutionErrorFormat.FormatCurrentCultureWithArgs(settings.CommandArgs, ex.Message);
                throw new CommandFailedException(errorMessage, ex);
            }
        }

        /// <summary>
        /// Checks if the specified container is in the list of containers from the target host.
        /// </summary>
        // Another fallback option would be to: docker inspect <containerName> --format {{.State.Status}} which should return "running"
        internal static bool IsContainerRunning(string hostName, string containerName, Connection remoteConnection)
        {
            IEnumerable<DockerContainerInstance> containers;
            if (remoteConnection != null)
            {
                containers = GetRemoteDockerContainers(remoteConnection, hostName, out _);
            }
            else
            {
                containers = GetLocalDockerContainers(hostName, out _);
            }

            if (containers != null)
            {
                // Check if the user entered the containerName or possibly part of the Id. 
                if (containers.Any(container => string.Equals(container.Name, containerName, StringComparison.Ordinal) ? true
                        : containers.Any(item => item.Id.StartsWith(containerName, StringComparison.Ordinal))))
                {
                    return true;
                }
            }

            return false;
        }

        internal static IEnumerable<DockerContainerInstance> GetRemoteDockerContainers(IConnection connection, string hostname, out int totalContainers)
        {
            totalContainers = 0;
            SSHConnection sshConnection = connection as SSHConnection;
            List<string> outputLines = new List<string>();
            StringBuilder errorSB = new StringBuilder();
            if (sshConnection == null)
            {
                return null;
            }

            List<DockerContainerInstance> containers = new List<DockerContainerInstance>();

            DockerCommandSettings settings = new DockerCommandSettings(hostname, true);
            settings.SetCommand(dockerPSCommand, dockerPSArgs);

            RemoteCommandRunner commandRunner = new RemoteCommandRunner(settings, sshConnection);

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
                    // if the exit code is not zero, then the output we received possibly is the error message
                    string exceptionMessage = UIResources.CommandExecutionErrorWithExitCodeFormat.FormatCurrentCultureWithArgs(
                            "{0} {1}".FormatInvariantWithArgs(settings.Command, settings.CommandArgs),
                            exitCode,
                            errorSB.ToString());

                    throw new CommandFailedException(exceptionMessage);
                }

                foreach (var item in outputLines)
                {
                    if (DockerContainerInstance.TryCreate(item, out DockerContainerInstance containerInstance))
                    {
                        containers.Add(containerInstance);
                    }
                    totalContainers++;
                }
            }

            return containers;
        }
    }
}
