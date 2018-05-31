// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Globalization;

namespace OpenDebug
{
    public class Terminal
    {
        private const string TERMINAL_TITLE = "VS Code Console";
        private const string PRESS_KEY_TO_CONTINUE = "Press any key to continue...";

        private const string OSASCRIPT = "/usr/bin/osascript";  // osascript is the AppleScript interpreter on OS X
        private const string LINUX_TERM = "/usr/bin/gnome-terminal";    //private const string LINUX_TERM = "/usr/bin/x-terminal-emulator";
        private const string WHICH = "/usr/bin/which";
        private const string WHERE = "where";
        private const string CMD = "cmd.exe";

        private static char[] s_ARGUMENT_SEPARATORS = new char[] { ' ', '\t' };

        /*
         * Enclose the given string in quotes if it contains space or tab characters.
         */
        public static string Quote(string arg)
        {
            if (arg.IndexOfAny(s_ARGUMENT_SEPARATORS) >= 0)
            {
                return '"' + arg + '"';
            }
            return arg;
        }

        public class LaunchResult
        {
            public bool Success { get; private set; }
            public string Message { get; private set; }
            public Process Process { get; private set; }
            public int ProcessId { get; private set; }
            public Process ConsoleProcess { get; private set; }

            public LaunchResult()
            {
                Success = true;
            }

            public void SetProcess(Process p, int pid)
            {
                Process = p;
                ProcessId = pid;
            }

            public void SetConsoleProcess(Process p)
            {
                ConsoleProcess = p;
            }

            public void SetError(string message)
            {
                Success = false;
                Message = message;
            }
        }

        public static LaunchResult LaunchInTerminal(string directory, string runtimePath, string[] runtimeArgs, string program, string[] programArgs, Dictionary<string, string> environmentVariables)
        {
            if (Utilities.IsOSX())
            {
                return LaunchInTerminalOSX(directory, runtimePath, runtimeArgs, program, programArgs, environmentVariables);
            }
            if (Utilities.IsLinux())
            {
                return LaunchInTerminalLinux(directory, runtimePath, runtimeArgs, program, programArgs, environmentVariables);
            }
            if (Utilities.IsWindows())
            {
                return LaunchInTerminalWindows(directory, runtimePath, runtimeArgs, program, programArgs, environmentVariables);
            }
            return LaunchInTerminalGeneric(directory, runtimePath, runtimeArgs, program, programArgs, environmentVariables);
        }

        // --- private ---------------------------------------------------------------------------------------------------------

        /*
         * Generic launch: lauch node directly
         */
        private static LaunchResult LaunchInTerminalGeneric(string directory, string runtimePath, string[] runtimeArgs, string program, string[] programArgs, Dictionary<string, string> environmentVariables)
        {
            Process process = new Process();
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.WorkingDirectory = directory;
            process.StartInfo.FileName = runtimePath;
            process.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", ConcatArgs(runtimeArgs), Terminal.Quote(program), ConcatArgs(programArgs));

            if (environmentVariables != null)
            {
                // we cannot set the env vars on the process StartInfo because we need to set StartInfo.UseShellExecute to true at the same time.
                // instead we set the env vars on OpenDebug itself because we know that OpenDebug lives as long as a debug session.
                foreach (var entry in environmentVariables)
                {
                    System.Environment.SetEnvironmentVariable(entry.Key, entry.Value);
                }
            }

            var result = new LaunchResult();
            try
            {
                process.Start();
                result.SetProcess(process, process.Id);
            }
            catch (Exception e)
            {
                result.SetError(e.Message);
            }
            return result;
        }

        /*
         * On Linux we try to launch.
         */
        private static LaunchResult LaunchInTerminalLinux(string directory, string runtimePath, string[] runtimeArgs, string program, string[] programArgs, Dictionary<string, string> environmentVariables)
        {
            Process terminalProcess = new Process();
            terminalProcess.StartInfo.CreateNoWindow = true;
            terminalProcess.StartInfo.UseShellExecute = false;

            terminalProcess.StartInfo.WorkingDirectory = directory;
            terminalProcess.StartInfo.FileName = LINUX_TERM;
            terminalProcess.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, "--title {0} -x bash -c 'cd {1}; {2} {3} {4} {5} ; echo; read -p {6} -n1;'",
                Quote(TERMINAL_TITLE), Quote(directory), Quote(runtimePath), ConcatArgs(runtimeArgs), Quote(program), ConcatArgs(programArgs), Quote(PRESS_KEY_TO_CONTINUE));

            if (environmentVariables != null)
            {
                ProcessStartInfo processStartInfo = terminalProcess.StartInfo;
                foreach (var entry in environmentVariables)
                {
                    processStartInfo.SetEnvironmentVariable(entry.Key, entry.Value);
                }
            }

            var result = new LaunchResult();
            try
            {
                terminalProcess.Start();
                result.SetProcess(terminalProcess, terminalProcess.Id);
            }
            catch (Exception e)
            {
                result.SetError(e.Message);
            }

            return result;
        }

        /*
         * On OS X we do not launch command directly but we launch an AppleScript that creates (or reuses) a Terminal window
         * and then launches the command inside that window. The script returns the process of the command or an error.
         */
        private static LaunchResult LaunchInTerminalOSX(string directory, string runtimePath, string[] runtimeArgs, string program, string[] programArgs, Dictionary<string, string> environmentVariables)
        {
            // first fix the PATH so that 'runtimePath' can be found if installed with 'brew'
            Utilities.FixPathOnOSX();

            var activate_terminal = false;  // see bug 17519

            string thisModulePath = typeof(Terminal).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName;
            var scriptPath = Path.Combine(Path.GetDirectoryName(thisModulePath), "TerminalHelper.scpt");
            var runtimeName = Path.GetFileName(runtimePath);

            var arguments = string.Format(CultureInfo.InvariantCulture, "{0} -t {1} -w {2} -r {3} -rn {4} -p {5}",
                Quote(scriptPath), Quote(TERMINAL_TITLE), Quote(directory), Quote(runtimePath), Quote(runtimeName), Quote(program));
            if (runtimeArgs != null)
            {
                foreach (var r in runtimeArgs)
                {
                    arguments += string.Format(CultureInfo.InvariantCulture, " -ra {0}", Quote(r));
                }
            }
            if (programArgs != null)
            {
                foreach (var a in programArgs)
                {
                    arguments += string.Format(CultureInfo.InvariantCulture, " -pa {0}", Quote(a));
                }
            }
            if (environmentVariables != null)
            {
                foreach (var entry in environmentVariables)
                {
                    arguments += string.Format(CultureInfo.InvariantCulture, " -e \"{0}={1}\"", entry.Key, entry.Value);
                }
            }
            if (activate_terminal)
            {
                arguments += " -a";
            }

            var scriptProcess = new Process();

            scriptProcess.StartInfo.CreateNoWindow = true;
            scriptProcess.StartInfo.UseShellExecute = false;
            scriptProcess.StartInfo.FileName = OSASCRIPT;
            scriptProcess.StartInfo.Arguments = arguments;
            scriptProcess.StartInfo.RedirectStandardOutput = true;
            scriptProcess.StartInfo.RedirectStandardError = true;

            var result = new LaunchResult();
            try
            {
                scriptProcess.Start();

                var stdout = scriptProcess.StandardOutput.ReadToEnd();
                var stderr = scriptProcess.StandardError.ReadToEnd();

                if (stdout.Length > 0)
                {
                    int pid;
                    var lines = Regex.Split(stdout, "\r\n|\r|\n");
                    if (lines.Length > 0)
                    {
                        if (Int32.TryParse(lines[0], out pid))
                        {
                            if (pid > 0)
                            {    // we got a real process ID
                                result.SetProcess(null, pid);
                            }
                        }
                        else
                        {
                            // could not parse, assume the reason is in stdout
                            result.SetError(stdout);
                        }
                    }
                }
                else
                {
                    // we got nothing on stdout; assume that stderr contains an error message
                    result.SetError(stderr);
                }
            }
            catch (Exception e)
            {
                result.SetError(e.Message);
            }

            return result;
        }

        /*
         * On Windows....
         */
        private static LaunchResult LaunchInTerminalWindows(string workingDirectory, string runtimePath, string[] runtimeArguments, string program, string[] program_args, Dictionary<string, string> environmentVariables)
        {
            var title = workingDirectory + " - VS Code";

            var consoleProc = new Process();
            consoleProc.StartInfo.CreateNoWindow = true;
            consoleProc.StartInfo.UseShellExecute = true;
            consoleProc.StartInfo.WorkingDirectory = workingDirectory;
            consoleProc.StartInfo.FileName = CMD;
            consoleProc.StartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, "/C title {0} && {1} {2} {3} {4} || pause",
                title, Terminal.Quote(runtimePath), ConcatArgs(runtimeArguments), Terminal.Quote(program), ConcatArgs(program_args));

            if (environmentVariables != null)
            {
                // we cannot set the env vars on the process StartInfo because we need to set StartInfo.UseShellExecute to true at the same time.
                // instead we set the env vars on OpenDebug itself because we know that OpenDebug lives as long as a debug session.
                foreach (var entry in environmentVariables)
                {
                    System.Environment.SetEnvironmentVariable(entry.Key, entry.Value);
                }
            }

            var result = new LaunchResult();
            try
            {
                consoleProc.Start();
                result.SetConsoleProcess(consoleProc);
            }
            catch (Exception e)
            {
                result.SetError(e.Message);
            }

            return result;
        }

        private static string ConcatArgs(string[] args)
        {
            var arg = "";
            if (args != null)
            {
                foreach (var r in args)
                {
                    arg += " " + Terminal.Quote(r);
                }
            }
            return arg;
        }
    }
}
