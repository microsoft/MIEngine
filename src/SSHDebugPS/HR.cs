// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.SSHDebugPS
{
    internal static class HR
    {
        public const int S_OK = 0;
        public const int S_FALSE = 1;
        public const int E_NOTIMPL = unchecked((int)0x80004001);
        public const int E_REMOTE_CONNECT_USER_CANCELED = unchecked((int)0x80040758);
        public const int E_FAIL = unchecked((int)0x80004005);

        public static void Check(int hr)
        {
            if (hr < 0)
            {
                Throw(hr);
            }
        }

        public static void Throw(int hr)
        {
            // Always use -1 as the second argument to GetExceptionForHR so that the CLR will ignore
            // any error info which might have been somehow set on this thread.
            IntPtr ignoreErrorInfo = (IntPtr)(-1);

            Exception e = Marshal.GetExceptionForHR(hr, ignoreErrorInfo);
            throw e;
        }
    }
}