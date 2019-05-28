// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Shell;
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
            try
            {
                //TODO: Once 'action' is cancelable, provide token to the action instead.
                ThreadHelper.JoinableTaskFactory.Run(
                    actionName,
                    async (progress, cancellationToken) =>
                    {
                        await Task.Run(action).WithCancellation(cancellationToken);
                    }
                );
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            return true;
        }
    }
}
