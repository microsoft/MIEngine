// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.MIDebugEngine
{
    // Wrapper class for VSWaitDialog
    public class WaitDialog
    {
        private VSWaitDialog _theDialog;
        public WaitDialog(string format, string caption)
        {
            try
            {
                _theDialog = new VSWaitDialog(format, caption);
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
