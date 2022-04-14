// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DebugEngineHost.VSCode;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using System;
using System.Threading;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// Implementation of the HostWaitLoop for VSCode
    /// using ProgressEvents
    /// </summary>
    public sealed class HostWaitLoop
    {
        private Guid _id = Guid.NewGuid();
        private int _lastPercentage;

        public HostWaitLoop(string message)
        {
            _lastPercentage = 0;
            ProgressEventManager.SendProgressStartEvent(new ProgressStartEvent()
            {
                ProgressId = _id.ToString(),
                Title = message,
                Percentage = _lastPercentage
            });
        }

        public void Wait(WaitHandle launchCompleteHandle, CancellationTokenSource cancellationSource)
        {
            // Note: at least for now we don't have at sort of UI to allow cancellation, so
            // ignore the cancelation source.
            launchCompleteHandle.WaitOne();

            ProgressEventManager.SendProgressEndEvent(new ProgressEndEvent()
            {
                ProgressId = _id.ToString()
            });
        }

        public void SetProgress(int totalSteps, int currentStep, string progressText)
        {
            _lastPercentage = currentStep * 100 / totalSteps; // Scale to [0, 100]

            ProgressEventManager.SendProgressUpdateEvent(new ProgressUpdateEvent()
            {
                ProgressId = _id.ToString(),
                Message = progressText,
                Percentage = _lastPercentage
            });
        }

        public void SetText(string text)
        {
            ProgressEventManager.SendProgressUpdateEvent(new ProgressUpdateEvent()
            {
                ProgressId = _id.ToString(),
                Message = text,
                Percentage = _lastPercentage
            });
        }
    }
}
