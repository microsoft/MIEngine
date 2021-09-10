// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugAdapterRunner
{
    /// <summary>Represents a response expected from a <see cref="DebugAdapterCommand"/></summary>
    public class DebugAdapterResponse
    {
        public object Response { get; private set; }
        public dynamic Match { get; internal set; }
        public bool IgnoreOrder { get; private set; }

        public bool IgnoreResponseOrder { get; private set; }

        public DebugAdapterResponse(object response, bool ignoreOrder = false, bool ignoreResponseOrder = false)
        {
            Response = response;
            Match = null;
            IgnoreOrder = ignoreOrder;
            IgnoreResponseOrder = ignoreResponseOrder;
        }
    }
}
