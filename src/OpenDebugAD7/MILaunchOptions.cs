// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.DebugEngineHost;
using Microsoft.DebugEngineHost.VSCode;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OpenDebug;

namespace OpenDebugAD7
{
    ///<summary>Generates launch options for launching the mi engine.</summary>
    internal static class MILaunchOptions
    {
        /// <summary>
        /// Returns the path to lldb-mi which is installed when the extension is installed
        /// </summary>
        /// <returns>Path to lldb-mi or null if it doesn't exist</returns>
        internal static string GetLLDBMIPath()
        {
            string exePath = null;
            string directory = EngineConfiguration.GetAdapterDirectory();
            DirectoryInfo dir = new DirectoryInfo(directory);

            // Remove /bin from the path to get to the debugAdapter folder
            string debugAdapterPath = dir.Parent?.FullName;

            if (!String.IsNullOrEmpty(debugAdapterPath))
            {
                // Path for lldb-mi 10.x and if it exists use it.
                exePath = Path.Combine(debugAdapterPath, "lldb-mi", "bin", "lldb-mi");
                if (!File.Exists(exePath))
                {
                    // Fall back to using path for lldb-mi 3.8
                    exePath = Path.Combine(debugAdapterPath, "lldb", "bin", "lldb-mi");
                    if (!File.Exists(exePath))
                    {
                        // Neither exist
                        return null;
                    }
                }
            }

            return exePath;
        }
    }
}
