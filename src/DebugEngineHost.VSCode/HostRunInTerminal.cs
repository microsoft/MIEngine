// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.DebugEngineHost
{
    public static class HostRunInTerminal
    {
        private static Action<string, string, bool, List<string>, Dictionary<string, object>, Action<int?>, Action<string>> s_runInTerminalCallback;

        /// <summary>
        /// Checks to see if RunInTerminal is available
        /// </summary>
        /// <returns></returns>
        public static bool IsRunInTerminalAvailable()
        {
            return s_runInTerminalCallback != null;
        }

        /// <summary>
        /// Passes the call to the UI to attempt to RunInTerminal if possible.
        /// </summary>
        public static void RunInTerminal(string title, string cwd, bool useExternalConsole, IReadOnlyList<string> commandArgs, IReadOnlyDictionary<string, string> environmentVars, Action<int?> success, Action<string> failure)
        {
            if (s_runInTerminalCallback != null)
            {
                Dictionary<string, object> env = new Dictionary<string, object>();
                foreach (var item in environmentVars)
                {
                    env.Add(item.Key, item.Value);
                }

                s_runInTerminalCallback(title, cwd, useExternalConsole, commandArgs.ToList<string>(), env, success, failure);
            }
        }

        /// <summary>
        /// Registers callback to call when RunInTerminal is called
        /// </summary>
        /// <param name="runInTerminalCallback">Callback for RunInTerminal</param>
        public static void RegisterRunInTerminalCallback(Action<string, string, bool, List<string>, Dictionary<string, object>, Action<int?>, Action<string>> runInTerminalCallback)
        {
            Debug.Assert(runInTerminalCallback != null, "Callback should not be null.");
            s_runInTerminalCallback = runInTerminalCallback;
        }
    }
}
