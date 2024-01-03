// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Microsoft.SSHDebugPS.UI
{
    /// <summary>
    /// This automationpeer class is here to support the custom expand/collapse action on the listbox item
    /// </summary>
    /// <typeparam name="T">Must be IContainerViewModel</typeparam>
    public class ContainerListViewItemAutomationPeer<T> : 
        ListBoxItemAutomationPeer, 
        IExpandCollapseProvider 
        where T : IContainerViewModel
    {
        public ContainerListViewItemAutomationPeer(object item, ContainerListViewAutomationPeer ownerAutomationPeer)
            : base(item, ownerAutomationPeer)
        { }

        /// <summary>
        /// Item is from the constructor and is the IContainerViewModel, not the listboxitem
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
                return ViewModel.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
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
            return AutomationControlType.ListItem;
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
