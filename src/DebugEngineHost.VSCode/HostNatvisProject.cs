// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DebugEngineHost
{
    public static class HostNatvisProject
    {
        public delegate void NatvisLoader(string path);

        public static void FindNatvisInSolution(NatvisLoader loader)
        {
            // In-solution natvis is not supported for VS Code now, so do nothing.
        }

        public static void FindNatvisInVSIX(NatvisLoader loader)
        {
            // VSIX natvis is not supported for VS Code now, so do nothing.
        }

        public static string FindSolutionRoot()
        {
            // This was added in MIEngine to support breakpoint sourcefile mapping. 
            // TODO: Return the project root if we want a similar implementation. 
            return String.Empty;
        }
    }
}
