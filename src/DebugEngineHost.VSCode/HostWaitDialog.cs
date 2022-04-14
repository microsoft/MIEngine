// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DebugEngineHost.VSCode;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using System;
using System.Globalization;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// This is a an implementation of HostWaitDialog for VS Code
    /// using the ProgressEvent
    /// </summary>
    public sealed class HostWaitDialog : IDisposable
    {
        private Guid _id = Guid.NewGuid();
        private readonly string _message;

        public HostWaitDialog(string format, string caption)
        {
            _message = string.Format(CultureInfo.CurrentCulture, format, caption);
        }

        public void ShowWaitDialog(string item)
        {
            ProgressEventManager.SendProgressStartEvent(new ProgressStartEvent()
            {
                ProgressId = _id.ToString(),
                Title = _message,
                Message = item
            });
        }

        public void EndWaitDialog()
        {
            ProgressEventManager.SendProgressEndEvent(new ProgressEndEvent()
            {
                ProgressId = _id.ToString()
            });
        }

        public void Dispose()
        {
            EndWaitDialog();
        }
    }
}
