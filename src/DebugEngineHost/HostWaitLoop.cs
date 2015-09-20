// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{
    public sealed class HostWaitLoop
    {
        private readonly object _progressLock = new object();
        private VSImpl.VsWaitLoop _vsWaitLoop;

        public HostWaitLoop(string message)
        {
            try
            {
                _vsWaitLoop = VSImpl.VsWaitLoop.TryCreate(message);
            }
            catch (FileNotFoundException)
            {
                // Visual Studio is not installed on this box
            }
        }

        /// <summary>
        /// Waits on the specified handle. This method should be called only once.
        /// </summary>
        /// <param name="launchCompleteHandle">[Required] handle to wait on</param>
        /// <param name="cancellationSource">[Required] Object to signal cancellation if cancellation is requested</param>
        /// <returns>true if we were able to successfully wait, false if we failed to wait and should fall back to the CLR provided wait function</returns>
        /// <exception cref="FileNotFoundException">Thrown by the JIT if Visual Studio is not installed</exception>
        public void Wait(WaitHandle launchCompleteHandle, CancellationTokenSource cancellationSource)
        {
            if (_vsWaitLoop != null)
            {
                _vsWaitLoop.Wait(launchCompleteHandle, cancellationSource);

                lock (_progressLock)
                {
                    _vsWaitLoop = null;
                }
            }
            else
            {
                launchCompleteHandle.WaitOne(); // For glass, fall back to waiting using the .NET Framework APIs
            }
        }

        public void SetProgress(int totalSteps, int currentStep, string progressText)
        {
            lock (_progressLock)
            {
                if (_vsWaitLoop != null)
                {
                    _vsWaitLoop.SetProgress(totalSteps, currentStep, progressText);
                }
            }
        }
    }
}
