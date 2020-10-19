// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDebugAD7
{
    internal static class HRConstants
    {
        public const int S_OK = 0;
        public const int S_FALSE = 1;
        public const int E_FAIL = unchecked((int)0x80004005);
        public const int E_INVALIDARG = unchecked((int)0x80070057);
        public const int E_NOTIMPL = unchecked((int)0x80004001);
        public const int E_ABORT = unchecked((int)(0x80004004));
        public const int COMQC_E_BAD_MESSAGE = unchecked((int)0x80110604);
        public const int RPC_E_SERVERFAULT = unchecked((int)0x80010105);
        public const int RPC_E_DISCONNECTED = unchecked((int)0x80010108);
        public const int E_ACCESSDENIED = unchecked((int)0x80070005);
        public const int E_CRASHDUMP_UNSUPPORTED = unchecked((int)0x80040211);
    }

    internal static class Constants
    {
        // POST_PREVIEW_TODO: no-func-eval support, radix, timeout
        public const uint EvaluationRadix = 10;
        public const uint ParseRadix = 10;
        public const uint EvaluationTimeout = 5000;
        public const int DisconnectTimeout = 2000;
        public const int DefaultTracepointCallstackDepth = 10;
        public const int InvalidProcessId = -1;
    }
}