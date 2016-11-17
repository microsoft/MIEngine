// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MICore
{
    /// <summary>
    /// Exception thrown to specify the debugger is disposed.
    /// </summary>
    public class DebuggerDisposedException : ObjectDisposedException
    {
        /// <summary>
        /// Command where the abort happened.
        /// </summary>
        public string AbortedCommand { get; private set; }

        /// <summary>
        /// Constructor for the DebuggerDisposedException which takes message, innerException and aborted command.
        /// </summary>
        public DebuggerDisposedException(string message, Exception innerException, string abortedCommand = null) : base(message, innerException)
        {
            AbortedCommand = abortedCommand;
        }

        /// <summary>
        /// Constructor for the DebuggerDisposedException which takes message and aborted command.
        /// </summary>
        public DebuggerDisposedException(string message, string abortedCommand = null) : this(message, null, abortedCommand)
        {
        }
    }
}
