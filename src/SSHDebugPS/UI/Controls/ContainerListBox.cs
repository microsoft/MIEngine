// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Microsoft.SSHDebugPS.UI
{
    public class ContainerListBox : ListBox
    {
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ContainerListBoxAutomationPeer(this);
        }
    }
}
