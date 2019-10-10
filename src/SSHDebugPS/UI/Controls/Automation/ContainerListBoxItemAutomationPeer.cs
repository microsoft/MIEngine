// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Microsoft.SSHDebugPS.UI
{
    public class ContainerListBoxItemAutomationPeer<T> : ListBoxItemAutomationPeer, IExpandCollapseProvider where T : IContainerViewModel
    {
        public ContainerListBoxItemAutomationPeer(object item, ContainerListBoxAutomationPeer ownerAutomationPeer)
            : base(item, ownerAutomationPeer)
        {

        }

        /// <summary>
        /// Item from the constructor is the viewmodel
        /// </summary>
        protected T ViewModel
        {
            get
            {
                return (T)Item;
            }
        }

        #region IExpandCollapseProvider
        public ExpandCollapseState ExpandCollapseState
        {
            get
            {
                if (ViewModel.IsExpanded)
                {
                    return ExpandCollapseState.Expanded;
                }
                else
                    return ExpandCollapseState.Collapsed;
            }
        }

        public void Expand()
        {
            ViewModel.IsExpanded = true;
        }

        public void Collapse()
        {
            ViewModel.IsExpanded = false;
        }
        #endregion
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ExpandCollapse)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        #region Core overrides
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
        }

        protected override string GetLocalizedControlTypeCore()
        {
            return ControlType.Button.LocalizedControlType;
        }

        protected override string GetNameCore()
        {
            return ViewModel.ContainerAutomationName;
        }

        protected override bool IsKeyboardFocusableCore()
        {
            return true;
        }

        #endregion
    }
}
