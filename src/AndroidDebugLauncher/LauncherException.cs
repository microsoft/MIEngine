// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidDebugLauncher
{
    internal class LauncherException : Exception
    {
        public readonly Telemetry.LaunchFailureCode TelemetryCode;

        public LauncherException(Telemetry.LaunchFailureCode telemetryCode, string message)
            : base(message)
        {
            TelemetryCode = telemetryCode;
        }
    }
}
