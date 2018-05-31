// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.DebugEngineHost
{
    public static class HostOutputWindow
    {
        private static Action<string> s_launchErrorCallback;

        public static void InitializeLaunchErrorCallback(Action<string> launchErrorCallback)
        {
            Debug.Assert(launchErrorCallback != null, "Bogus arguments to InitializeLaunchErrorCallback");
            s_launchErrorCallback = launchErrorCallback;
        }

        public static void WriteLaunchError(string outputMessage)
        {
            if (s_launchErrorCallback != null)
            {
                s_launchErrorCallback(outputMessage);
            }
        }
    }
}
