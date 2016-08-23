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

        private const string Debugger = "Debugger";

        /// <summary>
        /// Constructor for the DebuggerDisposedException which takes the aborted command.
        /// </summary>
        /// <param name="abortedCommand">MI Command</param>
        public DebuggerDisposedException(string abortedCommand = null) : base(Debugger)
        {
            AbortedCommand = abortedCommand;
        }

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
        public DebuggerDisposedException(string message, string abortedCommand = null) : base(Debugger, message)
        {
            AbortedCommand = abortedCommand;
        }
    }
}
