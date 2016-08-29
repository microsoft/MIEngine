// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Microsoft.VisualStudio;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.DebugEngineHost.VSImpl
{
    // Seperate class for waiting using VS interfaces
    // ************************************************************
    // * IMPORTANT: This needs to be a seperate class as when the JIT'er tries to JIT anything in this
    // * class it may through a FileNotFound exception for the VS assemblies. Do _NOT_ reference the
    // * VS assemblies from another class.
    // ************************************************************
    internal class VsWaitLoop
    {
        private readonly IVsCommonMessagePump _messagePump;

        private VsWaitLoop(string text)
        {
            int hr;

            var messagePumpFactory = (IVsCommonMessagePumpFactory)Package.GetGlobalService(typeof(SVsCommonMessagePumpFactory));
            if (messagePumpFactory == null)
            {
                return; // normal case in glass
            }

            IVsCommonMessagePump messagePump;
            hr = messagePumpFactory.CreateInstance(out messagePump);
            if (hr != 0) return;

            hr = messagePump.SetAllowCancel(true);
            if (hr != 0) return;

            hr = messagePump.SetWaitText(text);
            if (hr != 0) return;

            hr = messagePump.SetStatusBarText(string.Empty);
            if (hr != 0) return;

            _messagePump = messagePump;
        }

        static public VsWaitLoop TryCreate(string text)
        {
            VsWaitLoop waitLoop = new VsWaitLoop(text);
            if (waitLoop._messagePump == null)
                return null;

            return waitLoop;
        }

        /// <summary>
        /// Waits on the specified handle using the VS wait UI.
        /// </summary>
        /// <param name="launchCompleteHandle">[Required] handle to wait on</param>
        /// <param name="cancellationSource">[Required] Object to signal cancellation if cancellation is requested</param>
        /// <returns>true if we were able to successfully wait, false if we failed to wait and should fall back to the CLR provided wait function</returns>
        /// <exception cref="FileNotFoundException">Thrown by the JIT if Visual Studio is not installed</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
        public void Wait(WaitHandle launchCompleteHandle, CancellationTokenSource cancellationSource)
        {
            int hr;

            SafeWaitHandle safeWaitHandle = launchCompleteHandle.SafeWaitHandle;
            bool addRefSucceeded = false;

            try
            {
                safeWaitHandle.DangerousAddRef(ref addRefSucceeded);
                if (!addRefSucceeded)
                {
                    throw new ObjectDisposedException("launchCompleteHandle");
                }

                IntPtr nativeHandle = safeWaitHandle.DangerousGetHandle();
                IntPtr[] handles = { nativeHandle };
                uint waitResult;

                hr = _messagePump.ModalWaitForObjects(handles, (uint)handles.Length, out waitResult);
                if (hr == 0)
                {
                    return;
                }
                else if (hr == VSConstants.E_PENDING || hr == VSConstants.E_ABORT)
                {
                    // E_PENDING: user canceled
                    // E_ABORT: application exit
                    cancellationSource.Cancel();

                    throw new OperationCanceledException();
                }
                else
                {
                    Debug.Fail("Unexpected result from ModalWaitForObjects");
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
            finally
            {
                if (addRefSucceeded)
                {
                    safeWaitHandle.DangerousRelease();
                }
            }
        }

        public void SetProgress(int totalSteps, int currentStep, string progressText)
        {
            _messagePump.SetProgressInfo(totalSteps, currentStep, progressText);
        }

        public void SetText(string text)
        {
            _messagePump.SetWaitText(text);
        }
    }
}
