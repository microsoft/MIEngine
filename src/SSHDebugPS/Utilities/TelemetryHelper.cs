// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.SSHDebugPS.Utilities
{
    internal static class TelemetryHelper
    {
        public const string Event_DockerPSParseFailure = @"VS/Diagnostics/Debugger/SSHDebugPS/DockerPSParseFailure";
        public const string Event_ProcFSError = @"VS/Diagnostics/Debugger/SSHDebugPS/ProcFSError";
        public static readonly string Property_ExceptionName = "vs.diagnostics.debugger.ExceptionName";
    }
}
