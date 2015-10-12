// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Microsoft.DebugEngineHost
{
    public sealed class HostWaitLoop
    {
        public HostWaitLoop(string message)
        {
            throw new NotImplementedException();
        }

        public void Wait(WaitHandle launchCompleteHandle, CancellationTokenSource cancellationSource)
        {
            throw new NotImplementedException();
        }

        public void SetProgress(int totalSteps, int currentStep, string progressText)
        {
            throw new NotImplementedException();
        }
    }
}
