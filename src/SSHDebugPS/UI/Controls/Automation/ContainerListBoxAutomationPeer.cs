// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows.Automation.Peers;

namespace Microsoft.SSHDebugPS.UI
{
    public class ContainerListBoxAutomationPeer : ListBoxAutomationPeer
    {
        public ContainerListBoxAutomationPeer(ContainerListBox owner)
            : base(owner)
        { }

        protected override ItemAutomationPeer CreateItemAutomationPeer(object item)
        {
            return new ContainerListBoxItemAutomationPeer<DockerContainerViewModel>(item, this);
        }
    }
}
