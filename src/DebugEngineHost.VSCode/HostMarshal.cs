// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DebugEngineHost.VSCode;
using Microsoft.VisualStudio.Debugger.Interop;
using System;

namespace Microsoft.DebugEngineHost
{
    public static class HostMarshal
    {
        private static readonly HandleCollection<IDebugDocumentPosition2> s_documentPositions = new HandleCollection<IDebugDocumentPosition2>();
        private static readonly HandleCollection<IDebugFunctionPosition2> s_functionPositions = new HandleCollection<IDebugFunctionPosition2>();
        private static readonly HandleCollection<IDebugCodeContext2> s_codeContexts = new HandleCollection<IDebugCodeContext2>();

        public static IntPtr RegisterCodeContext(IDebugCodeContext2 codeContext)
        {
            if (codeContext == null)
            {
                throw new ArgumentNullException(nameof(codeContext));
            }

            lock (s_codeContexts)
            {
                return new IntPtr(s_codeContexts.Create(codeContext));
            }
        }

        public static IntPtr RegisterDocumentPosition(IDebugDocumentPosition2 documentPosition)
        {
            if (documentPosition == null)
            {
                throw new ArgumentNullException(nameof(documentPosition));
            }

            lock (s_documentPositions)
            {
                return (IntPtr)s_documentPositions.Create(documentPosition);
            }
        }

        public static IntPtr RegisterFunctionPosition(IDebugFunctionPosition2 functionPosition)
        {
            if (functionPosition == null)
            {
                throw new ArgumentNullException(nameof(functionPosition));
            }

            lock (s_functionPositions)
            {
                return (IntPtr)s_functionPositions.Create(functionPosition);
            }
        }

        //TODO: The MIEngine doesn't call this, but it should - currently this leaks
        //public static void ReleaseDocumentPositionId(IntPtr positionId)
        //{
        //    lock (_documentPositions)
        //    {
        //        if (!_documentPositions.Remove((int)positionId))
        //        {
        //            throw new ArgumentOutOfRangeException("positionId");
        //        }
        //    }
        //}

        public static void ReleaseCodeContextId(IntPtr codeContextId)
        {
            lock (s_codeContexts)
            {
                if (!s_codeContexts.Remove(codeContextId.ToInt32()))
                {
                    throw new ArgumentOutOfRangeException(nameof(codeContextId));
                }
            }
        }

        public static IDebugDocumentPosition2 GetDocumentPositionForIntPtr(IntPtr documentPositionId)
        {
            lock (s_documentPositions)
            {
                IDebugDocumentPosition2 documentPosition;
                if (!s_documentPositions.TryGet((int)documentPositionId, out documentPosition))
                {
                    throw new ArgumentOutOfRangeException(nameof(documentPositionId));
                }

                return documentPosition;
            }
        }

        public static IDebugFunctionPosition2 GetDebugFunctionPositionForIntPtr(IntPtr functionPositionId)
        {
            lock (s_functionPositions)
            {
                IDebugFunctionPosition2 functionPosition;
                if (!s_functionPositions.TryGet((int)functionPositionId, out functionPosition))
                {
                    throw new ArgumentOutOfRangeException(nameof(functionPositionId));
                }

                return functionPosition;
            }
        }

        /// <summary>
        /// Obtain the string expression from the bpLocation union for a BPLT_DATA_STRING breakpoint.
        /// </summary>
        /// <param name="stringId"></param>
        /// <returns></returns>
        public static string GetDataBreakpointStringForIntPtr(IntPtr stringId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return the string form of the address of a bound data breakpoint 
        /// </summary>
        /// <param name="address">address string</param>
        /// <returns>IntPtr to a BSTR which can be returned to VS.</returns>
        public static IntPtr GetIntPtrForDataBreakpointAddress(string address)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obtains a code context interface given the specified IntPtr of the location.
        /// </summary>
        /// <param name="contextId">In VS, the IUnknown pointer to QI for a code context. In VS Code,
        /// the identifier for the code context</param>
        /// <returns>code context object</returns>
        public static IDebugCodeContext2 GetDebugCodeContextForIntPtr(IntPtr contextId)
        {
            lock (s_codeContexts)
            {
                IDebugCodeContext2 codeContext;
                if (!s_codeContexts.TryGet(contextId.ToInt32(), out codeContext))
                {
                    throw new ArgumentOutOfRangeException(nameof(contextId));
                }

                return codeContext;
            }
        }

        public static IDebugEventCallback2 GetThreadSafeEventCallback(IDebugEventCallback2 ad7Callback)
        {
            return ad7Callback;
        }

        public static int Release(IntPtr unknownId)
        {
            // Not used in XPlat scenario.
            // TODO: Find a way to use ReleaseDocumentPositionId pattern (commented out above) to release handles from handlelist
            return 0;
        }
    }
}
