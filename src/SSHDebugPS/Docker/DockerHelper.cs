// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.SSHDebugPS.SSH;
using Microsoft.SSHDebugPS.Utilities;

namespace Microsoft.SSHDebugPS.Docker
{
    /// <summary>
    /// Base class for container runtime helpers (Docker, Podman, etc.).
    /// Subclasses override <see cref="CreateCommandSettings"/> to provide runtime-specific command settings.
    /// </summary>
    internal abstract class ContainerHelper
    {
        private const string psCommand = "ps";
        // --no-trunc avoids parameter truncation
        private const string psArgs = "-f status=running --no-trunc --format \"{{json .}}\"";
        private const string infoCommand = "info";
        private const string infoArgs = "-f {{.Driver}}";
        private const string versionCommand = "version";
        private const string versionArgs = "-f {{.Server.Os}}";
        private const string inspectCommand = "inspect";
        private const string inspectArgs = "-f \"{{json .Platform}}\" ";
        private static char[] charsToTrim = { ' ', '\"' };

        /// <summary>
        /// Creates the command settings for the container runtime (determines the executable name).
        /// </summary>
        protected abstract ContainerCommandSettingsBase CreateCommandSettings(string hostname, bool hostIsUnix);

        protected void RunCommand(ContainerCommandSettingsBase settings, Action<string> callback)
        {
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
                        args = args.Trim(charsToTrim);
                        callback(args);
                    }
                });

                commandRunner.Start();

                bool cancellationRequested = false;
                VS.VSOperationWaiter.Wait(UIResources.QueryingForContainersMessage, false, (cancellationToken) =>
                {
                    while (!resetEvent.WaitOne(2000) && !cancellationToken.IsCancellationRequested) { }
                    cancellationRequested = cancellationToken.IsCancellationRequested;
                });

                if (!cancellationRequested)
                {
                    if (exitCode.GetValueOrDefault(-1) != 0)
                    {
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
                }
            }
            catch (Win32Exception ex)
            {
                string errorMessage = UIResources.CommandExecutionErrorFormat.FormatCurrentCultureWithArgs(settings.CommandArgs, ex.Message);
                throw new CommandFailedException(errorMessage, ex);
            }
        }

        // LCOW is the abbreviation for Linux Containers on Windows
        internal bool TryGetLCOW(string hostname, out bool lcow)
        {
            lcow = false;
            bool delegateLCOW = false;

            ContainerCommandSettingsBase settings = CreateCommandSettings(hostname, false);
            settings.SetCommand(infoCommand, infoArgs);

            try
            {
                RunCommand(settings, delegate (string args)
                {
                    if (args.Contains("lcow"))
                    {
                        delegateLCOW = true;
                    }
                });
            }
            catch (CommandFailedException)
            {
                // only care whether call to obtain LCOW succeeded
                return false;
            }
        
            lcow = delegateLCOW;
            return true;
        }

        internal bool TryGetServerOS(string hostname, out string serverOS)
        {
            serverOS = string.Empty;
            string delegateServerOS = string.Empty;

            ContainerCommandSettingsBase settings = CreateCommandSettings(hostname, false);
            settings.SetCommand(versionCommand, versionArgs);

            try
            {
                RunCommand(settings, delegate (string args)
                {
                    delegateServerOS = args;
                });
            }
            catch (CommandFailedException)
            {
                // only care whether call to obtain server OS succeeded
                return false;
            }

            serverOS = delegateServerOS;
            return true;
        }

        internal bool TryGetContainerPlatform(string hostname, string containerName, out string containerPlatform)
        {
            containerPlatform = string.Empty;
            string delegateContainerPlatform = string.Empty;

            ContainerCommandSettingsBase settings = CreateCommandSettings(hostname, false);
            settings.SetCommand(inspectCommand, string.Concat(inspectArgs, containerName));

            try
            {
                RunCommand(settings, delegate (string args)
                {
                    delegateContainerPlatform = args;
                });
            }
            catch (CommandFailedException)
            {
                // only care whether call to obtain container platform succeeded
                return false;
            }

            containerPlatform = delegateContainerPlatform;
            return true;
        }

        /// <summary>
        /// Tries to create a container instance from a JSON string. Override for different JSON formats.
        /// </summary>
        protected virtual bool TryCreateContainerInstance(string json, out DockerContainerInstance instance)
        {
            return DockerContainerInstance.TryCreate(json, out instance);
        }

        internal IEnumerable<DockerContainerInstance> GetLocalContainers(string hostname, out int totalContainers)
        {
            totalContainers = 0;
            int containerCount = 0;
            List<DockerContainerInstance> containers = new List<DockerContainerInstance>();

            ContainerCommandSettingsBase settings = CreateCommandSettings(hostname, false);
            settings.SetCommand(psCommand, psArgs);

            RunCommand(settings, delegate (string args)
            {
                if (args.Trim()[0] == '{')
                {
                    if (TryCreateContainerInstance(args, out DockerContainerInstance containerInstance))
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
        internal bool IsContainerRunning(string hostName, string containerName, Connection remoteConnection)
        {
            IEnumerable<DockerContainerInstance> containers;
            if (remoteConnection != null)
            {
                containers = GetRemoteContainers(remoteConnection, hostName, out _);
            }
            else
            {
                containers = GetLocalContainers(hostName, out _);
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

        internal IEnumerable<DockerContainerInstance> GetRemoteContainers(IConnection connection, string hostname, out int totalContainers)
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

            ContainerCommandSettingsBase settings = CreateCommandSettings(hostname, true);
            settings.SetCommand(psCommand, psArgs);

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

                    // If it isn't json, assume its an error message
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
                    // if the exit code is not zero, then the output we received possibly is the error message
                    string exceptionMessage = UIResources.CommandExecutionErrorWithExitCodeFormat.FormatCurrentCultureWithArgs(
                            "{0} {1}".FormatInvariantWithArgs(settings.Command, settings.CommandArgs),
                            exitCode,
                            errorSB.ToString());

                    throw new CommandFailedException(exceptionMessage);
                }

                foreach (var item in outputLines)
                {
                    if (TryCreateContainerInstance(item, out DockerContainerInstance containerInstance))
                    {
                        containers.Add(containerInstance);
                    }
                    totalContainers++;
                }
            }

            return containers;
        }
    }

    /// <summary>
    /// Docker-specific container helper. Provides static convenience methods for backward compatibility.
    /// </summary>
    internal class DockerHelper : ContainerHelper
    {
        private static readonly DockerHelper _instance = new DockerHelper();
        private static ContainerHelper Base => _instance;

        protected override ContainerCommandSettingsBase CreateCommandSettings(string hostname, bool hostIsUnix)
        {
            return new DockerCommandSettings(hostname, hostIsUnix);
        }

        // Static convenience methods for backward compatibility
        internal static bool TryGetLCOW(string hostname, out bool lcow) => Base.TryGetLCOW(hostname, out lcow);
        internal static bool TryGetServerOS(string hostname, out string serverOS) => Base.TryGetServerOS(hostname, out serverOS);
        internal static bool TryGetContainerPlatform(string hostname, string containerName, out string containerPlatform)
            => Base.TryGetContainerPlatform(hostname, containerName, out containerPlatform);
        internal static IEnumerable<DockerContainerInstance> GetLocalDockerContainers(string hostname, out int totalContainers)
            => Base.GetLocalContainers(hostname, out totalContainers);
        internal static bool IsContainerRunning(string hostName, string containerName, Connection remoteConnection)
            => Base.IsContainerRunning(hostName, containerName, remoteConnection);
        internal static IEnumerable<DockerContainerInstance> GetRemoteDockerContainers(IConnection connection, string hostname, out int totalContainers)
            => Base.GetRemoteContainers(connection, hostname, out totalContainers);
    }
}
