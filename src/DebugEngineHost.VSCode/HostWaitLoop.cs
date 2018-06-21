// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Nop implementation of the HostWaitLoop for VSCode
    /// </summary>
    public sealed class HostWaitLoop
    {
        public HostWaitLoop(string message)
        {
        }

        public void Wait(WaitHandle launchCompleteHandle, CancellationTokenSource cancellationSource)
        {
            // Note: at least for now we don't have at sort of UI to allow cancellation, so
            // ignore the cancelation source.
            launchCompleteHandle.WaitOne();
        }

        public void SetProgress(int totalSteps, int currentStep, string progressText)
        {
        }

        public void SetText(string text)
        {
        }
    }
}
