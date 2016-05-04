// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

namespace MICore
{
    public class LocalUnixTerminalTransport : StreamTransport
    {
        private string _dbgStdInName;
        private string _dbgStdOutName;
        private FileSystemWatcher _fifoWatcher;

        public override void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer)
        {
            LocalLaunchOptions localOptions = (LocalLaunchOptions)options;

            // Default working directory is next to the app
            string debuggeeDir;
            if (Path.IsPathRooted(options.ExePath) && File.Exists(options.ExePath))
            {
                debuggeeDir = Path.GetDirectoryName(options.ExePath);
            }
            else
            {
                // If we don't know where the app is, default to HOME, and if we somehow can't get that, go with the root directory.
                debuggeeDir = Environment.GetEnvironmentVariable("HOME");
                if (string.IsNullOrEmpty(debuggeeDir))
                    debuggeeDir = "/";
            }

            _dbgStdInName = UnixUtilities.MakeFifo(Logger);
            _dbgStdOutName = UnixUtilities.MakeFifo(Logger);

            string watcherDirectory = _dbgStdInName.Substring(0, _dbgStdInName.IndexOf(UnixUtilities.FifoPrefix, StringComparison.OrdinalIgnoreCase));
            _fifoWatcher = new FileSystemWatcher(watcherDirectory, UnixUtilities.FifoPrefix + "*");
            _fifoWatcher.Deleted += FifoWatcher_Deleted;
            _fifoWatcher.EnableRaisingEvents = true;

            // Setup the streams on the fifos as soon as possible.
            FileStream dbgStdInStream = new FileStream(_dbgStdInName, FileMode.Open);
            FileStream dbgStdOutStream = new FileStream(_dbgStdOutName, FileMode.Open);

            string debuggerCmd = UnixUtilities.GetDebuggerCommand(localOptions);
            string launchDebuggerCommand = UnixUtilities.LaunchLocalDebuggerCommand(
                debuggeeDir,
                debuggerCmd,
                _dbgStdInName,
                _dbgStdOutName
                );

            TerminalLauncher terminal = TerminalLauncher.MakeTerminal("DebuggerTerminal", launchDebuggerCommand, localOptions.Environment);
            terminal.Launch(debuggeeDir);

            // The in/out names are confusing in this case as they are relative to gdb.
            // What that means is the names are backwards wrt miengine hence the reader
            // being the writer and vice-versa
            // Mono seems to hang when the debugger sends a large response unless we specify a larger buffer here
            writer = new StreamWriter(dbgStdInStream, new UTF8Encoding(false, true), 1024 * 4);
            reader = new StreamReader(dbgStdOutStream, Encoding.UTF8, true, 1024 * 4);
        }

        private void FifoWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (e.FullPath == _dbgStdInName || e.FullPath == _dbgStdOutName)
                {
                    Logger?.WriteLine("Fifo deleted, stop debugging");
                    _fifoWatcher.Deleted -= FifoWatcher_Deleted;
                    this.Callback.OnDebuggerProcessExit(null);
                }
            }
            catch
            {
                // Don't take down OpenDebugAD7 if the file watcher handler failed
            }
        }

        protected override string GetThreadName()
        {
            return "MI.LocalUnixTerminalTransport";
        }

        public override void Close()
        {
            base.Close();

            if (_fifoWatcher != null)
            {
                _fifoWatcher.Deleted -= FifoWatcher_Deleted;
            }
        }
    }
}
