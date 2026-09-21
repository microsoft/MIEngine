// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SSHDebugPS.Docker;

namespace Microsoft.SSHDebugPS.Podman
{
    internal sealed class PodmanExecutionManager : DockerExecutionManager
    {
        public PodmanExecutionManager(PodmanContainerTransportSettings baseSettings, Connection outerConnection)
            : base(baseSettings, outerConnection)
        { }

        protected override ContainerExecSettings CreateExecSettings(ContainerTargetTransportSettings baseSettings, string command, bool runInShell, bool makeInteractive)
        {
            return new PodmanExecSettings((PodmanContainerTransportSettings)baseSettings, command, runInShell, makeInteractive);
        }
    }
}
