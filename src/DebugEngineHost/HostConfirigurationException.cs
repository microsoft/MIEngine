// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.DebugEngineHost
{
    internal sealed class HostConfirigurationException : Exception
    {
        private const int E_DEBUG_ENGINE_NOT_REGISTERED = unchecked((int)0x80040019);

        public HostConfirigurationException(string missingLocation) : base(string.Format(CultureInfo.InvariantCulture, "Missing configuration section '{0}'", missingLocation))
        {
            this.HResult = E_DEBUG_ENGINE_NOT_REGISTERED;
        }
    }
}