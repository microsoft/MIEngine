// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.SSHDebugPS
{
    internal class ConnectionManager
    {
        internal static Connection GetInstance(string name, ConnectionReason reason)
        {
            // TODO: This should -
            // 1. Keep track of instances so we don't always need to reconnect
            // 2. Share credentials with the project system

            return VS.CredentialsDialog.Show(name, reason);
        }
    }
}