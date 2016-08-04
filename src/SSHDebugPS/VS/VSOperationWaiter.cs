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

            DebugEngineHost.HostWaitLoop waiterImpl = null;
            
            try
            {
                waiterImpl = new DebugEngineHost.HostWaitLoop(actionName);
            }
            catch (FileNotFoundException)
            {
                // Visual Studio is not installed on this box
            }

            if (waiterImpl != null)
            {
                using (ManualResetEvent completeEvent = new ManualResetEvent(false))
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                    t.ContinueWith((System.Threading.Tasks.Task unused) => completeEvent.Set(), TaskContinuationOptions.ExecuteSynchronously);

                    try
                    {
                        waiterImpl.Wait(completeEvent, cancellationTokenSource);
                    }
                    catch (OperationCanceledException) // VS Wait dialog implementation always throws on cancel
                    {
                        if (throwOnCancel)
                        {
                            throw;
                        }

                        return false;
                    }

                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        if (throwOnCancel)
                        {
                            throw new OperationCanceledException();
                        }

                        return false;
                    }

                    if (t.IsFaulted)
                    {
                        throw t.Exception.InnerException;
                    }
                }
            }

            return true;
        }
    }
}
