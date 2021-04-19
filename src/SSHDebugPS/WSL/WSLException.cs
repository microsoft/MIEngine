// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.SSHDebugPS.WSL
{
    [Serializable]
    internal class WSLException : Exception
    {
        public WSLException(string message) : base(message)
        {
        }

        public WSLException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WSLException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}