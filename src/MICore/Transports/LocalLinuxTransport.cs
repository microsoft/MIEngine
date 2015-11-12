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
using System.Globalization;


namespace MICore
{
    public class LocalLinuxTransport : StreamTransport
    {
        [System.Runtime.InteropServices.DllImport("System.Native", SetLastError = true)]
        private static extern int MkFifo(string name, int mode);

        private void MakeGdbFifo(string path)
        {
            // Mod is normally in octal, but C# has no octal values. This is 384 (rw owner, no rights anyone else)
            const int rw_owner = 384;
            int result = MkFifo(path, rw_owner);

            if (result != 0)
            {
                // Failed to create the fifo. Bail.
                Logger.WriteLine("Failed to create gdb fifo");
                throw new ArgumentException("MakeGdbFifo failed to create fifo");
            }
        }

        public override void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer)
        {
            LocalLaunchOptions localOptions = (LocalLaunchOptions)options;

            // TODO: Need to deal with attach?

            string debuggeeDir = System.IO.Path.GetDirectoryName(options.ExePath);

            string gdbStdInName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string gdbStdOutName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            MakeGdbFifo(gdbStdInName);
            MakeGdbFifo(gdbStdOutName);

            // Setup the streams on the fifos as soon as possible.
            System.IO.FileStream gdbStdInStream = new FileStream(gdbStdInName, FileMode.Open);
            System.IO.FileStream gdbStdOutStream = new FileStream(gdbStdOutName, FileMode.Open);

            // Spin up a new bash shell, cd to the working dir, execute a tty command to get the shell tty and store it
            // start the debugger in mi mode setting the tty to the terminal defined earlier and redirect stdin/stdout
            // to the correct pipes.
            // After gdb exits, cleanup the FIFOs.
            // TODO: this should be configurable in launch options with a default of gnome-terminal
            string terminalPath = "/usr/bin/gnome-terminal";
            Process terminalProcess = new Process();
            terminalProcess.StartInfo.CreateNoWindow = false;
            terminalProcess.StartInfo.UseShellExecute = false;
            terminalProcess.StartInfo.WorkingDirectory = debuggeeDir;
            terminalProcess.StartInfo.FileName = terminalPath;
            terminalProcess.StartInfo.Arguments =
                string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "--title DebuggerTerminal -x bash -c \"cd {0}; DbgTerm=`tty`; {1} --interpreter=mi --tty=$DbgTerm < {2} > {3}; rm {2}; rm {3} ;\"",
                    debuggeeDir,
                    localOptions.MIDebuggerPath,
                    gdbStdInName,
                    gdbStdOutName
                    );

            if (localOptions.Environment != null)
            {
                foreach (EnvironmentEntry entry in localOptions.Environment)
                {
                    terminalProcess.StartInfo.Environment.Add(entry.Name, entry.Value);
                }
            }

            terminalProcess.Start();

            // The in/out names are confusing in this case as they are relative to gdb.
            // What that means is the names are backwards wrt miengine hence the reader
            // being the writer and vice-versa
            writer = new StreamWriter(gdbStdInStream);
            reader = new StreamReader(gdbStdOutStream);
        }

        protected override string GetThreadName()
        {
            return "MI.LocalLinuxTransport";
        }
    }
}
