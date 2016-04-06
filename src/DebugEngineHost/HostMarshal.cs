// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost
{
    /// <summary>
    /// This class provides marshalling helper methods to a debug engine. 
    /// 
    /// When run in Visual Studio, these methods deal with COM marshalling.
    /// 
    /// When run in Visual Studio code, these methods are stubs to allow the AD7 API to function without COM.
    /// </summary>
    public static class HostMarshal
    {
        /// <summary>
        /// Registers the specified code context if it isn't already registered and returns an IntPtr that can be
        /// used by the host to get back to the object.
        /// </summary>
        /// <param name="codeContext">Object to register</param>
        /// <returns>In VS, the IntPtr to a native COM object which can be returned to VS. In VS Code, an identifier
        /// that allows VS Code to get back to the object.</returns>
        public static IntPtr RegisterCodeContext(IDebugCodeContext2 codeContext)
        {
            return Marshal.GetComInterfaceForObject(codeContext, typeof(IDebugCodeContext2));
        }

        /// <summary>
        /// Obtains a document position interface given the specified IntPtr of the document position.
        /// </summary>
        /// <param name="documentPositionId">In VS, the IUnknown pointer to QI for a document position. In VS Code,
        /// the identifier for the document position</param>
        /// <returns>Document position object</returns>
        public static IDebugDocumentPosition2 GetDocumentPositionForIntPtr(IntPtr documentPositionId)
        {
            // TODO: It looks like the MIEngine currently leaks the native document position. Fix that.

            return (IDebugDocumentPosition2)Marshal.GetObjectForIUnknown(documentPositionId);
        }

        /// <summary>
        /// Obtains a function position interface given the specified IntPtr of the location.
        /// </summary>
        /// <param name="locationId">In VS, the IUnknown pointer to QI for a function position. In VS Code,
        /// the identifier for the function position</param>
        /// <returns>Function position object</returns>
        public static IDebugFunctionPosition2 GetDebugFunctionPositionForIntPtr(IntPtr locationId)
        {
            // TODO: It looks like the MIEngine currently leaks the native document position. Fix that.

            return (IDebugFunctionPosition2)Marshal.GetObjectForIUnknown(locationId);
        }

        /// <summary>
        /// Obtain the string expression from the bpLocation union for a BPLT_DATA_STRING breakpoint.
        /// </summary>
        /// <param name="stringId"></param>
        /// <returns></returns>
        public static string GetDataBreakpointStringForIntPtr(IntPtr stringId)
        {
            return (string)Marshal.PtrToStringBSTR(stringId);
        }

        /// <summary>
        /// Return the string form of the address of a bound data breakpoint 
        /// </summary>
        /// <param name="address">address string</param>
        /// <returns>IntPtr to a BSTR which can be returned to VS.</returns>
        public static IntPtr GetIntPtrForDataBreakpointAddress(string address)
        {
            return Marshal.StringToBSTR(address);
        }

        /// <summary>
        /// Obtains a code context interface given the specified IntPtr of the location.
        /// </summary>
        /// <param name="contextId">In VS, the IUnknown pointer to QI for a code context. In VS Code,
        /// the identifier for the code context</param>
        /// <returns>code context object</returns>
        public static IDebugCodeContext2 GetDebugCodeContextForIntPtr(IntPtr contextId)
        {
            // TODO: It looks like the MIEngine currently leaks the code context. Fix that.

            return (IDebugCodeContext2)Marshal.GetObjectForIUnknown(contextId);
        }

        /// <summary>
        /// Obtains an event callback interface that can be used to send events on any threads
        /// </summary>
        /// <param name="ad7Callback">The underlying event call back which was obtained from the port</param>
        /// <returns>In VS, a thread-safe wrapper on top of the underlying SDM event callback which allows
        /// sending events on any thread. In VS Code, this just returns the provided ad7Callback. </returns>
        public static IDebugEventCallback2 GetThreadSafeEventCallback(IDebugEventCallback2 ad7Callback)
        {
            return new VSImpl.VSEventCallbackWrapper(ad7Callback);
        }

        /// <summary>
        /// Allocate storage and copy the guid to the allocated bytes
        /// </summary>
        /// <param name="guid">guid to copy</param>
        /// <returns>pointer to the allocated bytes</returns>
        public static IntPtr AllocateGuid(Guid guid)
        {
            byte[] guidBytes = guid.ToByteArray();
            IntPtr result = Marshal.AllocCoTaskMem(guidBytes.Length);
            Marshal.Copy(guidBytes, 0, result, guidBytes.Length);
            return result;
        }
    }
}
