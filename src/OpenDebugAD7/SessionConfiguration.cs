// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDebugAD7
{
    /// <summary>
    /// Class to hold configuration for a single debug session, such as Just My Code or Require Exact Source settings
    /// </summary>
    internal class SessionConfiguration
    {
        public bool RequireExactSource { get; set; } = true;
        public bool JustMyCode { get; set; } = true;
        public bool StopAtEntrypoint { get; set; } = false;
        public bool EnableStepFiltering { get; set; } = true;
    }
}
