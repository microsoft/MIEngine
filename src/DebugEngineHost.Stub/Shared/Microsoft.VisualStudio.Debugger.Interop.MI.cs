// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Debugger.Interop.MI
{
    /// <summary>
    /// IDebugProperty for MIEngine
    /// </summary>
    [ComImport()]
    [ComVisible(true)]
    [Guid("27F5EFAF-9DBA-4AC0-A456-1F97E50F3CDA")]
    [InterfaceType(1)]
    public interface IDebugMIEngineProperty
    {
        /// <summary> 
        /// Get the expression context for the property
        /// </summary>
        [PreserveSig]
        int GetExpressionContext([Out, MarshalAs(UnmanagedType.Interface)] out IDebugExpressionContext2 ppExpressionContext);
    }
}
