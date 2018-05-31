// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// This is a nop implementation of HostWaitDialog. For now at least,
    /// there is no way to put up the dialog in VS Code, so do nothing.
    /// </summary>
    public sealed class HostWaitDialog : IDisposable
    {
        public HostWaitDialog(string format, string caption)
        {
        }

        public void ShowWaitDialog(string item)
        {
        }

        public void EndWaitDialog()
        {
        }

        public void Dispose()
        {
            EndWaitDialog();
        }
    }
}
