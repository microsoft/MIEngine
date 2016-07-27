// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.SSHDebugPS.VS
{
    internal static class VSOperationWaiter
    {
        /// <summary>
        /// Executes the specified action on a background thread, showing a wait dialog if it is available
        /// </summary>
        /// <param name="actionName">[Required] description of the action to show in the wait dialog</param>
        /// <param name="action">[Required] action to run</param>
        /// <param name="throwOnCancel">If specified, an OperationCanceledException is thrown if the operation is canceled</param>
        /// <returns>True if the operation succeed and wasn't canceled</returns>
        /// <exception cref="OperationCanceledException">Wait was canceled and 'throwOnCancel' is true</exception>
        public static bool Wait(string actionName, bool throwOnCancel, Action action)
        {
            Task t = Task.Run(action);

            WaiterImpl waiterImpl = null;
            try
            {
                waiterImpl = WaiterImpl.TryCreate(actionName);
            }
            catch (FileNotFoundException)
            {
                // Visual Studio is not installed on this box
            }

            if (waiterImpl != null)
            {
                bool success = waiterImpl.Wait(t);
                if (!success)
                {
                    if (throwOnCancel)
                    {
                        throw new OperationCanceledException();
                    }
                    return false;
                }
            }
            else
            {
                t.Wait();
            }

            if (t.IsFaulted)
            {
                throw t.Exception.InnerException;
            }

            return true;
        }

        private class WaiterImpl
        {
            private readonly IVsCommonMessagePump _messagePump;

            private WaiterImpl(string text)
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

            static public WaiterImpl TryCreate(string text)
            {
                WaiterImpl waitLoop = new WaiterImpl(text);
                if (waitLoop._messagePump == null)
                    return null;

                return waitLoop;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Runtime.InteropServices.SafeHandle.DangerousGetHandle")]
            public bool Wait(System.Threading.Tasks.Task task)
            {
                int hr;

                ManualResetEvent completeEvent = new ManualResetEvent(initialState: false);

                task.ContinueWith((System.Threading.Tasks.Task unused) => completeEvent.Set(), TaskContinuationOptions.ExecuteSynchronously);

                SafeWaitHandle safeWaitHandle = completeEvent.SafeWaitHandle;
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
                        return true;
                    }
                    else if (hr == VSConstants.E_PENDING || hr == VSConstants.E_ABORT)
                    {
                        // E_PENDING: user canceled
                        // E_ABORT: application exit
                        return false;
                    }
                    else
                    {
                        Debug.Fail("Unexpected result from ModalWaitForObjects");
                        Marshal.ThrowExceptionForHR(hr);
                        return false;
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
        }
    }
}
