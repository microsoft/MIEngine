using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    internal class BaseCommand<T> : ICommand
    {
        private Func<T, bool> canExecuteFunc;
        private Action<T> executeFunc;

        internal BaseCommand(Action<T> execFunc)
        {
            executeFunc = execFunc;
        }

        internal BaseCommand(Action<T> execFunc, Func<T, bool> canExecFunc)
            : this(execFunc)
        {
            canExecuteFunc = canExecFunc;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (canExecuteFunc != null)
            {
                return canExecuteFunc((T)parameter);
            }

            return true;
        }

        public void Execute(object parameter)
        {
            if (executeFunc != null)
            {
                executeFunc((T)parameter);
            }
            else
            {
                Debug.Fail("Why is the executeFunc null?");
            }
        }
    }
}
