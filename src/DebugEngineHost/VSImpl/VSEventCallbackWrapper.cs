using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DebugEngineHost.VSImpl
{
    /// <summary>
    /// This class is VS only (_NOT_ VS Code)
    /// </summary>
    class VSEventCallbackWrapper : IDebugEventCallback2
    {
        // If we store the event callback in a normal IDebugEventCallback2 member, COM interop will attempt to call back to the main UI
        // thread the first time that we invoke the call back. To work around this, we will instead store the event call back in the 
        // global interface table like we would if we implemented this code in native.
        private readonly uint _cookie;

        // NOTE: The GIT doesn't aggregate the free threaded marshaler, so we can't store it in an RCW
        // or the CLR will just call right back to the main thread to try and marshal it.
        private readonly IntPtr _pGIT;
        private Guid _IID_IDebugEventCallback2 = typeof(IDebugEventCallback2).GUID;

        private readonly object _cacheLock = new object();
        private int _cachedEventCallbackThread;
        private IDebugEventCallback2 _cacheEventCallback;

        internal VSEventCallbackWrapper(IDebugEventCallback2 ad7Callback)
        {
            // Obtain the GIT from COM, and store the event callback in it
            Guid CLSID_StdGlobalInterfaceTable = new Guid("00000323-0000-0000-C000-000000000046");
            Guid IID_IGlobalInterfaceTable = typeof(IGlobalInterfaceTable).GUID;
            const int CLSCTX_INPROC_SERVER = 0x1;
            _pGIT = NativeMethods.CoCreateInstance(ref CLSID_StdGlobalInterfaceTable, IntPtr.Zero, CLSCTX_INPROC_SERVER, ref IID_IGlobalInterfaceTable);

            var git = GetGlobalInterfaceTable();
            git.RegisterInterfaceInGlobal(ad7Callback, ref _IID_IDebugEventCallback2, out _cookie);
            Marshal.ReleaseComObject(git);
        }

        ~VSEventCallbackWrapper()
        {
            // NOTE: This object does NOT implement the dispose pattern. The reasons are --
            // 1. The underlying thing we are disposing is the SDM's IDebugEventCallback2. We are not going to get
            //    deterministic release of this without both implementing the dispose pattern on this object but also
            //    switching to use Marshal.ReleaseComObject everywhere. Marshal.ReleaseComObject is difficult to get
            //    right and there isn't a large need to deterministically release the SDM's event callback.
            // 2. There is some risk of deadlock if we tried to implement the dispose pattern because of the trickiness
            //    of releasing cross-thread COM interfaces. We could avoid this by doing an async dispose, but then
            //    we losing the primary benefit of dispose which is the deterministic release.

            if (_cookie != 0)
            {
                var git = GetGlobalInterfaceTable();
                git.RevokeInterfaceFromGlobal(_cookie);
                Marshal.ReleaseComObject(git);
            }

            if (_pGIT != IntPtr.Zero)
            {
                Marshal.Release(_pGIT);
            }
        }

        int IDebugEventCallback2.Event(IDebugEngine2 engine, IDebugProcess2 process, IDebugProgram2 program, IDebugThread2 thread, IDebugEvent2 @event, ref Guid riidEvent, uint attribs)
        {
            IDebugEventCallback2 ad7EventCallback = GetAD7EventCallback();
            return ad7EventCallback.Event(engine, process, program, thread, @event, ref riidEvent, attribs);
        }

        private IGlobalInterfaceTable GetGlobalInterfaceTable()
        {
            Debug.Assert(_pGIT != IntPtr.Zero, "GetGlobalInterfaceTable called before the m_pGIT is initialized");
            // NOTE: We want to use GetUniqueObjectForIUnknown since the GIT will exist in both the STA and the MTA, and we don't want
            // them to be the same rcw
            return (IGlobalInterfaceTable)Marshal.GetUniqueObjectForIUnknown(_pGIT);
        }

        private IDebugEventCallback2 GetAD7EventCallback()
        {
            Debug.Assert(_cookie != 0, "GetEventCallback called before m_cookie is initialized");

            // We send esentially all events from the same thread, so lets optimize the common case
            int currentThreadId = Thread.CurrentThread.ManagedThreadId;
            if (_cacheEventCallback != null && _cachedEventCallbackThread == currentThreadId)
            {
                lock (_cacheLock)
                {
                    if (_cacheEventCallback != null && _cachedEventCallbackThread == currentThreadId)
                    {
                        return _cacheEventCallback;
                    }
                }
            }

            var git = GetGlobalInterfaceTable();

            IntPtr pCallback;
            git.GetInterfaceFromGlobal(_cookie, ref _IID_IDebugEventCallback2, out pCallback);

            Marshal.ReleaseComObject(git);

            var eventCallback = (IDebugEventCallback2)Marshal.GetObjectForIUnknown(pCallback);
            Marshal.Release(pCallback);

            lock (_cacheLock)
            {
                _cachedEventCallbackThread = currentThreadId;
                _cacheEventCallback = eventCallback;
            }

            return eventCallback;
        }

        private static class NativeMethods
        {
            [DllImport("ole32.dll", ExactSpelling = true, PreserveSig = false)]
            public static extern IntPtr CoCreateInstance(
                [In] ref Guid clsid,
                IntPtr punkOuter,
                int context,
                [In] ref Guid iid);
        }
    }
}
