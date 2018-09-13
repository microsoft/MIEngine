// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace MICore
{
    public static class UnixUtilities
    {
        internal const string SudoPath = "/usr/bin/sudo";
        // Mono seems to stop responding when the is a large response unless we specify a larger buffer here
        internal const int StreamBufferSize = 1024 * 4;
        private const string PKExecPath = "/usr/bin/pkexec";

        // Linux specific
        private const string PtraceScopePath = "/proc/sys/kernel/yama/ptrace_scope";

        // OS X specific
        private const string CodeSignPath = "/usr/bin/codesign";

        /// <summary>
        /// Launch a new terminal, spin up a new bash shell, cd to the working dir, execute a tty command to get the shell tty and store it.
        /// Start the debugger in mi mode setting the tty to the terminal defined earlier and redirect stdin/stdout
        /// to the correct pipes. After the debugger exits, cleanup the FIFOs. This is done using the trap command to add a
        /// signal handler for SIGHUP on the console (executing the two rm commands)
        /// </summary>
        /// <param name="debuggeeDir">Path to the debuggee directory</param>
        /// <param name="dbgStdInName">File where the stdin for the debugger process is redirected to</param>
        /// <param name="dbgStdOutName">File where the stdout for the debugger process is redirected to</param>
        /// <param name="pidFifo">File where the debugger pid is written to</param>
        /// <param name="dbgCmdScript">Script file to pass the debugger launch command</param>
        /// <param name="debuggerCmd">Command to the debugger</param>
        /// <param name="debuggerArgs">MIDebugger arguments</param>
        /// <returns></returns>
        internal static string LaunchLocalDebuggerCommand(
            string debuggeeDir,
            string dbgStdInName,
            string dbgStdOutName,
            string pidFifo,
            string dbgCmdScript,
            string debuggerCmd,
            string debuggerArgs)
        {
            // On OSX, 'wait' will return once there is a status change from the launched process rather than for it to exit, so
            // we need to use 'fg' there. This works as our bash prompt is launched through apple script rather than 'bash -c'.
            // On Linux, fg will fail with 'no job control' because our commands are being executed through 'bash -c', and
            // bash doesn't support fg in this mode, so we need to use 'wait' there.
            string waitForCompletionCommand = PlatformUtilities.IsOSX() ? "fg > /dev/null; " : "wait $pid; ";

            return
                // echo the shell pid so that we can monitor it
                // Change to the current working directory
                // find the tty
                // enable monitor on the session
                // set the trap command to remove the fifos
                // execute the debugger command in the background
                // Clear the output of executing a process in the background: [job number] pid
                // echo and wait the debugger pid to know whether we need to fake an exit by the debugger
                FormattableString.Invariant(
                    $@"echo $$ > {pidFifo} ; cd {debuggeeDir} ; 
                    DbgTerm=`tty` ; 
                    set -o monitor ; 
                    trap 'rm {dbgStdInName} {dbgStdOutName} {pidFifo} {dbgCmdScript}' EXIT ; 
                    {debuggerCmd} {debuggerArgs} --tty=$DbgTerm < {dbgStdInName} > {dbgStdOutName} & clear ; 
                    pid=$! ; 
                    echo $pid > {pidFifo} ; 
                    {waitForCompletionCommand} "
                    );
        }

        internal static string GetDebuggerCommand(LocalLaunchOptions localOptions)
        {
            if (PlatformUtilities.IsLinux())
            {
                string debuggerPathCorrectElevation = localOptions.MIDebuggerPath;
                string prompt = string.Empty;

                // If running as root, make sure the new console is also root. 
                bool isRoot = UnixNativeMethods.GetEUid() == 0;

                // If the system doesn't allow a non-root process to attach to another process, try to run GDB as root
                if (localOptions.ProcessId.HasValue && !isRoot && UnixUtilities.GetRequiresRootAttach(localOptions.DebuggerMIMode))
                {
                    prompt = String.Format(CultureInfo.CurrentCulture, "read -n 1 -p \\\"{0}\\\" yn; if [[ ! $yn =~ ^[Yy]$ ]] ; then exit 0; fi; ", MICoreResources.Warn_AttachAsRootProcess);

                    // Prefer pkexec for a nice graphical prompt, but fall back to sudo if it's not available
                    if (File.Exists(UnixUtilities.PKExecPath))
                    {
                        debuggerPathCorrectElevation = String.Concat(UnixUtilities.PKExecPath, " ", debuggerPathCorrectElevation);
                    }
                    else if (File.Exists(UnixUtilities.SudoPath))
                    {
                        debuggerPathCorrectElevation = String.Concat(UnixUtilities.SudoPath, " ", debuggerPathCorrectElevation);
                    }
                    else
                    {
                        Debug.Fail("Root required to attach, but no means of elevating available!");
                    }
                }

                return String.Concat(prompt, debuggerPathCorrectElevation);
            }
            else
            {
                return localOptions.MIDebuggerPath;
            }
        }

        internal static string MakeFifo(string identifier = null, Logger logger = null)
        {
            string path = Path.Combine(Path.GetTempPath(), Utilities.GetMIEngineTemporaryFilename(identifier));

            // Mod is normally in octal, but C# has no octal values. This is 384 (rw owner, no rights anyone else)
            const int rw_owner = 384;
            byte[] pathAsBytes = new byte[Encoding.UTF8.GetByteCount(path) + 1];
            Encoding.UTF8.GetBytes(path, 0, path.Length, pathAsBytes, 0);
            int result = UnixNativeMethods.MkFifo(pathAsBytes, rw_owner);

            if (result != 0)
            {
                // Failed to create the fifo. Bail.
                logger?.WriteLine("Failed to create fifo");
                throw new ArgumentException("MakeFifo failed to create fifo at path {0}", path);
            }

            return path;
        }

        internal static bool GetRequiresRootAttach(MIMode mode)
        {
            if (mode != MIMode.Clrdbg)
            {
                // If "ptrace_scope" is a value other than 0, only root can attach to arbitrary processes
                if (GetPtraceScope() != 0)
                {
                    return true; // Attaching to any non-child process requires root
                }
            }

            return false;
        }

        private static int GetPtraceScope()
        {
            // See: https://www.kernel.org/doc/Documentation/security/Yama.txt
            if (!File.Exists(UnixUtilities.PtraceScopePath))
            {
                // If the scope file doesn't exist, security is disabled
                return 0;
            }

            try
            {
                string scope = File.ReadAllText(UnixUtilities.PtraceScopePath);
                return Int32.Parse(scope, CultureInfo.CurrentCulture);
            }
            catch
            {
                // If we were unable to determine the current scope setting, assume we need root
                return -1;
            }
        }

        internal static bool IsProcessRunning(int processId)
        {
            // When getting the process group ID, getpgid will return -1
            // if there is no process with the ID specified.
            return UnixNativeMethods.GetPGid(processId) >= 0;
        }

        public static bool IsBinarySigned(string filePath, Logger logger)
        {
            if (!PlatformUtilities.IsOSX())
            {
                throw new NotImplementedException();
            }

            Process p = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = CodeSignPath,
                    Arguments = "--display " + filePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                 }
            };

            p.OutputDataReceived += (sender, e) =>
            {
                OutputNonEmptyString(e.Data, "codeSign-stdout: ", logger);
            };

            p.ErrorDataReceived += (sender, e) =>
            {
                OutputNonEmptyString(e.Data, "codeSign-stderr: ", logger);
            };

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            return p.ExitCode == 0;
        }

        internal static void OutputNonEmptyString(string str, string prefix, Logger logger)
        {
            if (!String.IsNullOrWhiteSpace(str) && logger != null)
            {
                logger.WriteLine(prefix + str);
            }
        }

        internal static void KillProcessTree(Process p)
        {
            bool isLinux = PlatformUtilities.IsLinux();
            bool isOSX = PlatformUtilities.IsOSX();
            if (isLinux || isOSX)
            {
                // On linux run 'ps -x -o "%p %P"' (similarly on Mac), which generates a list of the process ids (%p) and parent process ids (%P).
                // Using this list, issue a 'kill' command for each child process. Kill the children (recursively) to eliminate
                // the entire process tree rooted at p. 
                Process ps = new Process();
                ps.StartInfo.FileName = "/bin/ps";
                ps.StartInfo.Arguments = isLinux ? "-x -o \"%p %P\"" : "-x -o \"pid ppid\"";
                ps.StartInfo.RedirectStandardOutput = true;
                ps.StartInfo.UseShellExecute = false;
                ps.Start();
                string line;
                List<Tuple<int, int>> processAndParent = new List<Tuple<int, int>>();
                char[] whitespace = new char[] { ' ', '\t' };
                while ((line = ps.StandardOutput.ReadLine()) != null)
                {
                    line = line.Trim();
                    int id, pid;
                    if (Int32.TryParse(line.Substring(0, line.IndexOfAny(whitespace)), NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                        && Int32.TryParse(line.Substring(line.IndexOfAny(whitespace)).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out pid))
                    {
                        processAndParent.Add(new Tuple<int, int>(id, pid));
                    }
                }
                KillChildren(processAndParent, p.Id);
            }
        }

        private static void KillChildren(List<Tuple<int, int>> processes, int pid)
        {
            processes.ForEach((p) =>
            {
                if (p.Item2 == pid)
                {
                    KillChildren(processes, p.Item1);
                    Kill(p.Item1, 9);
                }
            });
        }

        internal static void Interrupt(int pid)
        {
            Kill(pid, 2);
        }

        private static void Kill(int pid, int signal)
        {
            var k = Process.Start("/bin/kill", String.Format(CultureInfo.InvariantCulture, "-{0} {1}", signal, pid));
            k.WaitForExit();
        }
    }
}
