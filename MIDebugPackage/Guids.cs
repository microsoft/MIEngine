// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// MUST match guids.h

using System;

namespace Microsoft.MIDebugPackage
{
    internal static class GuidList
    {
        public const string guidMIDebugPackagePkgString = "7a28ceda-da3e-4172-b19a-bb9c810046a6";
        public const string guidMIDebugPackageCmdSetString = "eb4e4965-e07f-46d0-afcc-aa4dd43a5336";

        public static readonly Guid guidMIDebugPackageCmdSet = new Guid(guidMIDebugPackageCmdSetString);
    };
}
