// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Microsoft.SSHDebugPS.UI
{
    public interface IContainerUICommand : ICommand
    {
        string Label { get; }
        string ToolTip { get; }
        void NotifyCanExecuteChanged();
    }

    internal class ContainerUICommand : IContainerUICommand
    {
        private Func<object, bool> canExecuteFunc;
        private Action<object> executeFunc;

        internal ContainerUICommand(Action<object> execFunc, string label, string tooltip)
        {
            executeFunc = execFunc;
            Label = label;
            ToolTip = tooltip;
        }

        internal ContainerUICommand(Action<object> execFunc, Func<object, bool> canExecFunc, string label, string tooltip)
            : this(execFunc, label, tooltip)
        {
            canExecuteFunc = canExecFunc;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (canExecuteFunc != null)
            {
                return canExecuteFunc(parameter);
            }

            return true;
        }

        public void Execute(object parameter)
        {
            if (executeFunc != null)
            {
                executeFunc(parameter);
            }
            else
            {
                Debug.Fail("Why is the executeFunc null?");
            }
        }

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public string Label { get; }
        public string ToolTip { get; }
    }
}
