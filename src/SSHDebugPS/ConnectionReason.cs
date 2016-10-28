// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SSHDebugPS
{
    /// <summary>
    /// Indicates the reason we are attempting to add a connection
    /// </summary>
    internal enum ConnectionReason
    {
        /// <summary>
        /// A call is being made to 'AddPort' on the port supplier
        /// </summary>
        AddPort,

        /// <summary>
        /// We are connecting because an operation requires a connection
        /// </summary>
        Deferred
    }
}
