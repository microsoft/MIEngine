// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.DebugEngineHost.VSCode
{
    internal static class AssemblyResolver
    {
        static readonly HashSet<string> s_unresolvedNames = new HashSet<string>();

        public static void Initialize()
        {
            AssemblyLoadContext.Default.Resolving += OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(AssemblyLoadContext loadContext, AssemblyName assemblyName)
        {
            Assembly asm = InnerResolveHandler(assemblyName);

            return asm;
        }

        private static Assembly InnerResolveHandler(AssemblyName assemblyName)
        {
            string assemblyFileName = string.Concat(assemblyName.Name, ".dll");

            if (assemblyName.CultureInfo != null && !assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture))
            {
                //Prepend the culture directory (e.g. ja\Microsoft.VisualStudio.Test.resources.dll)
                assemblyFileName = Path.Combine(assemblyName.CultureInfo.Name, assemblyFileName);
            }

            // This name was unresolved last time. No need to check for it again
            lock (s_unresolvedNames)
            {
                if (s_unresolvedNames.Contains(assemblyFileName))
                {
                    return null;
                }
            }

            Assembly asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assemblyFileName));

            if (asm == null)
            {
                lock (s_unresolvedNames)
                {
                    s_unresolvedNames.Add(assemblyFileName);
                }
            }

            return asm;
        }
    }
}
