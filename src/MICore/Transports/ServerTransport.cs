// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Collections.Specialized;
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;

namespace MICore
{
    public class ServerTransport : PipeTransport
    {
        private string _startPattern;
        public string _messagePrefix;
        private bool _started;
        private ManualResetEvent _event;

        public ServerTransport(ManualResetEvent e, bool killOnClose, bool filterStderr = false, bool filterStdout = false) 
            : base(killOnClose, filterStderr, filterStdout)
        {
            _event = e;
        }

        public override void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer)
        {
            LocalLaunchOptions localOptions = (LocalLaunchOptions)options;
            string miDebuggerDir = System.IO.Path.GetDirectoryName(localOptions.MIDebuggerPath);

            Process proc = new Process();
            proc.StartInfo.FileName = localOptions.DebugServer;
            proc.StartInfo.Arguments = localOptions.DebugServerArgs;
            proc.StartInfo.WorkingDirectory = miDebuggerDir;
            _startPattern = localOptions.ServerStarted;
            _messagePrefix = Path.GetFileNameWithoutExtension(localOptions.DebugServer);  

            InitProcess(proc, out reader, out writer);
        }

        protected override string FilterLine(string line)
        {
            if (!_started && Regex.IsMatch(line, _startPattern, RegexOptions.None, new TimeSpan(0, 0, 0, 0, 10) /* 10 ms */))
            {
                _started = true;
                _event.Set();
            }

            this.Callback.LogText(_messagePrefix + ": " + line);   // log to debug output
            return null;
        }

        protected override string GetThreadName()
        {
            return "MI.ServerTransport";
        }
    }
}
