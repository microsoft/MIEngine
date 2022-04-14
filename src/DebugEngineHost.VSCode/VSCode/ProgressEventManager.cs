// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DebugEngineHost.VSCode
{
    public static class ProgressEventManager
    {
        private static Action<DebugEvent> s_sendEvent;

        public static void SetEventHandler(Action<DebugEvent> sendEvent)
        {
            s_sendEvent = sendEvent;
        }

        public static void SendProgressStartEvent(ProgressStartEvent start)
        {
            SendEvent(start);
        }

        public static void SendProgressEndEvent(ProgressEndEvent end)
        {
            SendEvent(end);
        }

        public static void SendProgressUpdateEvent(ProgressUpdateEvent update)
        {
            SendEvent(update);
        }

        private static void SendEvent(DebugEvent debugEvent)
        {
            if (s_sendEvent != null && debugEvent != null)
            {
                s_sendEvent(debugEvent);
            }
        }
    }
}
