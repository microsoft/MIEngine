// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.SSHDebugPS
{
    /// <summary>
    /// Exception thrown when the user cancels trying to connect
    /// </summary>
    internal class AD7ConnectCanceledException : OperationCanceledException
    {
        public AD7ConnectCanceledException(ConnectionReason reason)
        {
            // The debugger has a good HRESULT for when the user cancels trying to connect to the target computer.
            // Unfortunately, the core debugger doesn't always understand this HR, and in code paths where it doesn't
            // the message is lousy. So only use the debugger's HRESULT when we are in AddPort or any other code path
            // where we are sure cancel messages are respected. 
            if (reason == ConnectionReason.AddPort)
            {
                this.HResult = HR.E_REMOTE_CONNECT_USER_CANCELED;
            }
        }
    }
}