// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System.Globalization;

namespace Microsoft.MIDebugEngine
{
    public class VSWaitDialog
    {
        private readonly IVsThreadedWaitDialog2 _waitDialog;
        private const int m_delayShowDialogTimeInSeconds = 2;
        private bool _started;
        private string _format;
        private string _caption;

        public VSWaitDialog(string format, string caption)
        {
            _format = format;
            _caption = caption;
            var waitDialogFactory = (IVsThreadedWaitDialogFactory)Package.GetGlobalService(typeof(SVsThreadedWaitDialogFactory));
            if (waitDialogFactory == null)
            {
                return; // normal case in glass
            }

            IVsThreadedWaitDialog2 waitDialog;
            int hr = waitDialogFactory.CreateInstance(out waitDialog);
            if (hr != VSConstants.S_OK) return;

            _waitDialog = waitDialog;
            _started = false;
        }
        public void ShowWaitDialog(string item)
        {
            if (_waitDialog == null)
            {
                return;
            }
            lock (_waitDialog)
            {
                string message = String.Format(CultureInfo.CurrentCulture, _format, item);
                int hr;
                if (!_started)
                {
                    hr = _waitDialog.StartWaitDialog(
                        _caption,
                        message,
                        "",
                        "",
                        message,
                        m_delayShowDialogTimeInSeconds,
                        /*fIsCancelable*/ false,
                        /*fShowMarqueeProgress*/ true
                        );
                }
                else
                {
                    bool canceled;
                    hr = _waitDialog.UpdateProgress(message, "", message, 0, 0, /*fDisableCancel*/true, out canceled);
                }
                if (hr != VSConstants.S_OK) return;
                _started = true;
            }
        }

        public void EndWaitDialog()
        {
            if (_waitDialog == null)
            {
                return;
            }
            lock (_waitDialog)
            {
                int canceled;
                if (!_started) return;
                int hr = _waitDialog.EndWaitDialog(out canceled);
                if (hr != VSConstants.S_OK) return;
                _started = false;
            }
        }
    }
}
