// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

using Microsoft.DebugEngineHost;

namespace MICore
{
    internal class RunInTerminalLauncher
    {
        private string _title;

        private Dictionary<string, string> _environment;

        public RunInTerminalLauncher(string title, ReadOnlyCollection<EnvironmentEntry> envEntries)
        {
            _title = title;
            _environment = new Dictionary<string, string>();

            if (envEntries != null && envEntries.Any())
            {
                foreach (var envEntry in envEntries)
                {
                    Debug.Assert(!_environment.ContainsKey(envEntry.Name), FormattableString.Invariant($"Duplicate key ${envEntry.Name} detected!"));
                    _environment[envEntry.Name] = envEntry.Value;
                }
            }
        }

        public void Launch(List<string> cmdArgs, bool useExternalConsole, Action<int?> launchCompleteAction, Action<string> launchFailureAction, Logger logger)
        {
            if (HostRunInTerminal.IsRunInTerminalAvailable())
            {
                HostRunInTerminal.RunInTerminal(_title, string.Empty, useExternalConsole, cmdArgs, new ReadOnlyDictionary<string, string>(_environment), launchCompleteAction, launchFailureAction);
            }
        }
    }
}
