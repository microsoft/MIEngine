// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;


namespace MICore
{
    public class LocalLinuxTransport : StreamTransport
    {
        [DllImport("System.Native", SetLastError = true)]
        private static extern int MkFifo(string name, int mode);

        [DllImport("System.Native", SetLastError = true)]
        private static extern uint GetEUid();

        private void MakeGdbFifo(string path)
        {
            // Mod is normally in octal, but C# has no octal values. This is 384 (rw owner, no rights anyone else)
            const int rw_owner = 384;
            int result = MkFifo(path, rw_owner);

            if (result != 0)
            {
                // Failed to create the fifo. Bail.
                Logger.WriteLine("Failed to create gdb fifo");
                throw new ArgumentException("MakeGdbFifo failed to create fifo at path {0}", path);
            }
        }

        public override void InitStreams(LaunchOptions options, out StreamReader reader, out StreamWriter writer)
        {
            LocalLaunchOptions localOptions = (LocalLaunchOptions)options;

            string debuggeeDir = System.IO.Path.GetDirectoryName(options.ExePath);

            string gdbStdInName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string gdbStdOutName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            MakeGdbFifo(gdbStdInName);
            MakeGdbFifo(gdbStdOutName);

            // Setup the streams on the fifos as soon as possible.
            System.IO.FileStream gdbStdInStream = new FileStream(gdbStdInName, FileMode.Open);
            System.IO.FileStream gdbStdOutStream = new FileStream(gdbStdOutName, FileMode.Open);

            // If running as root, make sure the new console is also root. 
            bool isRoot = GetEUid() == 0;

            // Spin up a new bash shell, cd to the working dir, execute a tty command to get the shell tty and store it
            // start the debugger in mi mode setting the tty to the terminal defined earlier and redirect stdin/stdout
            // to the correct pipes. After gdb exits, cleanup the FIFOs.
            //
            // NOTE: sudo launch requires sudo or the terminal will fail to launch. The first argument must then be the terminal path
            // TODO: this should be configurable in launch options to allow for other terminals with a default of gnome-terminal so the user can change the terminal
            // command. Note that this is trickier than it sounds since each terminal has its own set of parameters. For now, rely on remote for those scenarios
            string terminalPath = "/usr/bin/gnome-terminal";
            string sudoPath = "/usr/bin/sudo";
            Process terminalProcess = new Process();
            terminalProcess.StartInfo.CreateNoWindow = false;
            terminalProcess.StartInfo.UseShellExecute = false;
            terminalProcess.StartInfo.WorkingDirectory = debuggeeDir;
            terminalProcess.StartInfo.FileName = !isRoot ? terminalPath : sudoPath;

            string argumentString = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "--title DebuggerTerminal -x bash -c \"cd {0}; DbgTerm=`tty`; {1} --interpreter=mi --tty=$DbgTerm < {2} > {3}; rm {2}; rm {3} ;\"",
                    debuggeeDir,
                    localOptions.MIDebuggerPath,
                    gdbStdInName,
                    gdbStdOutName
                    );

            terminalProcess.StartInfo.Arguments = !isRoot ? argumentString : String.Concat(terminalPath, " ", argumentString);
            Logger.WriteLine(terminalProcess.StartInfo.Arguments);

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
