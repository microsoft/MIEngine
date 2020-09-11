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

    /// <summary>
    /// Interface to get Program information for Debug Adapter Protocol.
    /// </summary>
    [ComImport()]
    [ComVisible(true)]
    [Guid("1615C23F-2BA4-4018-A135-E8B889B01F3E")]
    [InterfaceType(1)]
    public interface IDebugProgramDAP
    {
        /// <summary>
        /// Retrieves the program's pointer size in bits. (e.g. 64 or 32)
        /// </summary>
        /// <param name="pResult">Number of bits in a pointer.</param>
        /// [PreserveSig]
        int GetPointerSize([Out] out int pResult);

        /// <summary>
        /// Tries to complete a given command string.
        /// </summary>
        /// <param name="command">Partial command to complete</param>
        /// <param name="stackFrame">Optional stack frame as context</param>
        /// <param name="result">Completion List or null</param>
        /// <returns></returns>
        int AutoCompleteCommand([In] string command, [In] IDebugStackFrame2 stackFrame, [Out] out string[] result);
    }

    /// <summary>
    /// IDebugMemoryBytesDAP for Debug Adapter Protocol
    /// </summary>
    [ComImport()]
    [ComVisible(true)]
    [Guid("CF4FADE1-3252-4680-9E70-8B44CA92DD3F")]
    [InterfaceType(1)]
    public interface IDebugMemoryBytesDAP
    {
        /// <summary>
        /// This method will create an IDebugMemoryContext from a given address.
        /// </summary>
        [PreserveSig]
        int CreateMemoryContext([In] ulong address, [Out, MarshalAs(UnmanagedType.Interface)] out IDebugMemoryContext2 ppResult);
    }
}
