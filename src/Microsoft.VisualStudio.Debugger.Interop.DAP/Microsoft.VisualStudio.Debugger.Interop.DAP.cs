using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Debugger.Interop.DAP
{
    /// <summary>
    /// An extended set of evaluate flags that are in Debug Adapter Protocol but not listed in AD7
    /// </summary>
    [Flags]
    public enum DAPEvalFlags
    {
        /// <summary>
        /// No additional Eval Flags from DAP
        /// </summary>
        NONE = 0,
        /// <summary>
        /// Evaluation is for a clipboard context
        /// </summary>
        CLIPBOARD_CONTEXT = 1
    }

    /// <summary>
    /// IDebugExpression for Debug Adapter Protocol
    /// </summary>
    [ComImport()]
    [ComVisible(true)]
    [Guid("76C710C4-CE66-4422-AC7C-1E41B3FC0BE3")]
    [InterfaceType(1)]
    public interface IDebugExpressionDAP
    {
        /// <summary>
        /// The IDebugExpression.EvaluateSync interface which includes DAPEvalFlags. 
        /// This method is to be used with calls from clients using the Debug Adapter Protocol.
        /// </summary>
        [PreserveSig]
        int EvaluateSync([In] enum_EVALFLAGS dwFlags, [In] DAPEvalFlags dapFlags, [In] uint dwTimeout, [In][MarshalAs(UnmanagedType.Interface)] IDebugEventCallback2 pExprCallback, [Out, MarshalAs(UnmanagedType.Interface)] out IDebugProperty2 ppResult);
    }
}
