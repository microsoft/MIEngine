// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private const string dockerPSArgs = "--no-trunc --format \"{{json .}}\"";
        public static IEnumerable<IContainerInstance> GetLocalDockerContainers(string hostname)
        {
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
                            errorSB.AppendLine(args);
                        }
                        else
                        {
                            var containerInstance = DockerContainerInstance.Create(args);
                            if (containerInstance != null)
                                containers.Add(containerInstance);
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

        internal static IEnumerable<IContainerInstance> GetRemoteDockerContainers(IConnection connection, string hostname)
        {
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
                    containers.Add(DockerContainerInstance.Create(item));
                }
            }

            return containers;
        }
    }
}
