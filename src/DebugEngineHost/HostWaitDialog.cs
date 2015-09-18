// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Provides an optional wait dialog to allow long running operations to be canceled. Some hosts
    /// may not implement the dialog, in which case these methods will be a nop.
    /// </summary>
    public class HostWaitDialog : IDisposable
    {
        private VSImpl.VSWaitDialog _theDialog;
        public HostWaitDialog(string format, string caption)
        {
            try
            {
                _theDialog = new VSImpl.VSWaitDialog(format, caption);
            }
            catch (FileNotFoundException)
            {
                _theDialog = null;
            }
        }
        public void ShowWaitDialog(string item)
        {
            if (_theDialog != null)
            {
                _theDialog.ShowWaitDialog(item);
            }
        }

        public void EndWaitDialog()
        {
            if (_theDialog != null)
            {
                _theDialog.EndWaitDialog();
            }
        }

        public void Dispose()
        {
            EndWaitDialog();
        }
    }
}
