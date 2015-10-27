namespace MICore
{
    /// <summary>
    /// Interface used by the Debugger class to stop execution of currently debugged process to be able to send commands.
    /// Provided by all kinds of launchers, that use non asynchronous native debuggers.
    /// </summary>
    public interface IBreakHandler
    {
        /// <summary>
        /// Stops currently executed program, by for example issuing CTRL+C signal to underlying native debugger.
        /// </summary>
        void Break();

        /// <summary>
        /// Gets an indication, if instead of debugged process should be instantly terminated (killed).
        /// </summary>
        bool UseOldStyleTermination
        {
            get;
        }
    }
}
