// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JDbg
{
    public enum ErrorCode
    {
        VMUnavailable,
        InvalidResponse,
        SocketError,
        ConnectFailure,
        SendFailure,
        CommandFailure,
        FailedToInitialize
    }

    public class JdwpException : Exception
    {
        public readonly ErrorCode ErrorCode;

        internal JdwpException(ErrorCode errorCode, string message) : base(message)
        {
            this.ErrorCode = errorCode;
        }

        internal JdwpException(ErrorCode errorCode, string message, Exception innerException) : base(message, innerException)
        {
            this.ErrorCode = errorCode;
        }
    }
}
