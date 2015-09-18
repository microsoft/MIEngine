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
    public static class VSMarshal
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
        /// <param name="documentPositionId">In VS, the IUknown pointer to QI for a document position. In VS Code,
        /// the identifier for the document position</param>
        /// <returns>Document position object</returns>
        public static IDebugDocumentPosition2 GetDocumentPositionForIntPtr(IntPtr documentPositionId)
        {
            // TODO: It looks like the MIEngine currently leaks the native document position. Fix that.

            return (IDebugDocumentPosition2)Marshal.GetObjectForIUnknown(documentPositionId);
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
    }
}
