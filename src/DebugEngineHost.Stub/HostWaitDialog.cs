// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DebugEngineHost
{
    public sealed class HostWaitDialog : IDisposable
    {
        public HostWaitDialog(string format, string caption)
        {
            throw new NotImplementedException();
        }
        public void ShowWaitDialog(string item)
        {
            throw new NotImplementedException();
        }

        public void EndWaitDialog()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            EndWaitDialog();
        }
    }
}
