// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.DebugEngineHost
{
    public static class HostOutputWindow
    {
        private static Action<string> s_launchErrorCallback;
        private static Action<string, string, bool, List<string>, Dictionary<string, object>, Action<int?>, Action<string>> s_runInTerminalCallback;

        public static void InitializeLaunchErrorCallback(Action<string> launchErrorCallback)
        {
            Debug.Assert(launchErrorCallback != null, "Bogus arguments to InitializeLaunchErrorCallback");
            s_launchErrorCallback = launchErrorCallback;
        }

        public static void WriteLaunchError(string outputMessage)
        {
            if (s_launchErrorCallback != null)
            {
                s_launchErrorCallback(outputMessage);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if it is able to attempt RunInTerminal</returns>
        public static bool TryRunInTerminal(string title, string cwd, bool useExternalConsole, IReadOnlyList<string> commandArgs, IReadOnlyDictionary<string, string> environmentVars, Action<int?> success, Action<string> failure)
        {
            if (s_runInTerminalCallback != null)
            {
                Dictionary<string, object> env = new Dictionary<string, object>();
                foreach (var item in environmentVars)
                {
                    env.Add(item.Key, item.Value);
                }

                s_runInTerminalCallback(title, cwd, useExternalConsole, commandArgs.ToList<string>(), env, success, failure);
                return true;
            }
            return false;
        }

        public static void RegisterRunInTerminalCallback(Action<string, string, bool, List<string>, Dictionary<string, object>, Action<int?>, Action<string>> runInTerminalCallback)
        {
            Debug.Assert(runInTerminalCallback != null, "Callback should not be null.");
            s_runInTerminalCallback = runInTerminalCallback;
        }
    }
}
