// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace DebuggerTesting.Utilities
{
    public static class AssemblyExtensions
    {
        #region Methods

#if !CORECLR

        /// <summary>
        /// Gets the path of the <see cref="Assembly"/>.
        /// </summary>
        public static string GetPath(this Assembly assembly)
        {
            Parameter.ThrowIfNull(assembly, nameof(assembly));

            return assembly.Location;
        }

#endif

        #endregion
    }
}