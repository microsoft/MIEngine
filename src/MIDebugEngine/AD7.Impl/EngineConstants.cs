// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.MIDebugEngine
{
    static public class EngineConstants
    {
        /// <summary>
        /// This is the engine GUID of the engine. It needs to be changed here and in the registration
        /// when creating a new engine.
        /// </summary>
        public static readonly Guid EngineId = new Guid("{ea6637c6-17df-45b5-a183-0951c54243bc}");

        public static readonly Guid GdbEngine = new Guid("{91744D97-430F-42C1-9779-A5813EBD6AB2}");
    }
}
