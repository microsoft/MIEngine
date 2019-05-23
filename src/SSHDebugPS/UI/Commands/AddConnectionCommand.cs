// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Windows.Input;

namespace Microsoft.SSHDebugPS.UI
{
    internal class BaseCommand : ICommand
    {
        private Func<bool> canExecuteFunc;
        private Action executeFunc;

        internal BaseCommand(Action execFunc)
        {
            executeFunc = execFunc;
        }

        internal BaseCommand(Action execFunc, Func<bool> canExecFunc)
            : this(execFunc)
        {
            canExecuteFunc = canExecFunc;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (canExecuteFunc != null)
            {
                return canExecuteFunc();
            }

            return true;
        }

        public void Execute(object parameter)
        {
            if (executeFunc != null)
            {
                executeFunc();
            }
            else
            {
                Debug.Fail("Why is the executeFunc null?");
            }
        }
    }
}
