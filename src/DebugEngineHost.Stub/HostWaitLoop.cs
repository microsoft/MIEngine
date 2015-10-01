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
