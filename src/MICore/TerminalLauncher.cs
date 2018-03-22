// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace MICore
{
    internal abstract class TerminalLauncher
    {
        private Process _terminalProcess;
        protected string _title;
        protected string _initScript;
        protected bool _isRoot;
        protected ReadOnlyCollection<EnvironmentEntry> _environment;

        public static TerminalLauncher MakeTerminal(string title, string initScript, ReadOnlyCollection<EnvironmentEntry> environment)
        {
            TerminalLauncher terminal = null;
            if (PlatformUtilities.IsLinux())
            {
                terminal = new LinuxTerminalLauncher(title, initScript, environment);
            }
            else if (PlatformUtilities.IsOSX())
            {
                terminal = new MacTerminalLauncher(title, initScript, environment);
            }
            else
            {
                Debug.Fail("Cannot make a terminal for non Linux or OS X.");
                throw new InvalidOperationException();
            }

            return terminal;
        }

        protected abstract string GetProcessExecutable();
        protected abstract string GetProcessArgs();
        protected abstract void SetNewProcessEnvironment(ProcessStartInfo processStartInfo);

        protected TerminalLauncher(string title, string initScript, ReadOnlyCollection<EnvironmentEntry> environment)
        {
            _title = title;
            _initScript = initScript;
            _environment = environment;
            _isRoot = UnixNativeMethods.GetEUid() == 0;
        }

        public void Launch(string workingDirectory, Logger logger)
        {
            _terminalProcess = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = false,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory,
                    FileName = GetProcessExecutable(),
                    Arguments = GetProcessArgs(),
                    // Redirect stdout and stderr to logging
                    // or else it will send to the default stdout and stderr
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            _terminalProcess.OutputDataReceived += (sender, e) =>
            {
                logger?.WriteLine("term-stdout: " + e.Data);
            };

            _terminalProcess.ErrorDataReceived += (sender, e) =>
            {
                logger?.WriteLine("term-stderr: " + e.Data);
            };

            SetNewProcessEnvironment(_terminalProcess.StartInfo);
            _terminalProcess.Start();
            _terminalProcess.BeginOutputReadLine();
            _terminalProcess.BeginErrorReadLine();
        }
    }

    internal class MacTerminalLauncher : TerminalLauncher
    {
        private const string OsascriptPath = "/usr/bin/osascript";
        private const string LaunchTerminalScript = "osxlaunchhelper.scpt";

        public MacTerminalLauncher(string title, string initScript, ReadOnlyCollection<EnvironmentEntry> environment)
            : base(title, initScript, environment)
        { }

        protected override string GetProcessExecutable()
        {
            return OsascriptPath;
        }

        protected override string GetProcessArgs()
        {
            string thisModulePath = typeof(MacTerminalLauncher).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName;
            string launchScript = Path.Combine(Path.GetDirectoryName(thisModulePath), LaunchTerminalScript);
            if (!File.Exists(launchScript))
            {
                string message = string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_InternalFileMissing, launchScript);
                throw new FileNotFoundException(message);
            }

            // NOTE: setEnvironmentScript will either be the empty string or end in a space, so '{2}' and '{3}' can be next to each other.
            string setEnvironmentScript = GetSetEnvironmentScript();
            return string.Format(CultureInfo.InvariantCulture, "{0} \"{1}\" \"{2}{3}\"", launchScript, _title, setEnvironmentScript, _initScript);
        }

        private string GetSetEnvironmentScript()
        {
            if (_environment == null)
            {
                return string.Empty;
            }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (EnvironmentEntry entry in _environment)
            {
                stringBuilder.Append("export ");
                stringBuilder.Append(entry.Name);
                stringBuilder.Append("='");

                // If the value doesn't contain single quote characters ('), we can just drop it in
                if (entry.Value.IndexOf('\'') < 0)
                {
                    stringBuilder.Append(entry.Value);
                }
                else
                {
                    // otherwise we need to escape the single quotes
                    foreach (char c in entry.Value)
                    {
                        if (c != '\'')
                        {
                            stringBuilder.Append(c);
                        }
                        else
                        {
                            // We need to:
                            // 1. End the single quoted string we are in (the one started with "...='"), 
                            // 2. Add a single quote character
                            // 3. Restart the single quoted string
                            stringBuilder.Append(@"'\''");
                        }
                    }
                }

                stringBuilder.Append("'; ");
            }

            return stringBuilder.ToString();
        }

        protected override void SetNewProcessEnvironment(ProcessStartInfo processStartInfo)
        {
            // Nothing to do. Environment is set via the init script
        }
    }

    internal class LinuxTerminalLauncher : TerminalLauncher
    {
        private const string GnomeTerminalPath = "/usr/bin/gnome-terminal";
        private const string XTermPath = "/usr/bin/xterm";
        private string _terminalPath;
        private string _bashCommandPrefix;

        public LinuxTerminalLauncher(string title, string initScript, ReadOnlyCollection<EnvironmentEntry> environment)
            : base(title, initScript, environment)
        {
            if (File.Exists(GnomeTerminalPath))
            {
                _terminalPath = GnomeTerminalPath;
                _bashCommandPrefix = String.Format(CultureInfo.InvariantCulture, "--title {0} -x", _title);
            }
            else if (File.Exists(XTermPath))
            {
                _terminalPath = XTermPath;
                _bashCommandPrefix = String.Format(CultureInfo.InvariantCulture, "-title {0} -e", _title);
            }
            else
            {
                throw new FileNotFoundException(MICoreResources.Error_NoTerminalAvailable_Linux);
            }
        }

        protected override string GetProcessExecutable()
        {
            // NOTE: sudo launch requires sudo or the terminal will fail to launch. The first argument must then be the terminal path
            // TODO: this should be configurable in launch options to allow for other terminals with a default of gnome-terminal so the user can change the terminal
            // command. Note that this is trickier than it sounds since each terminal has its own set of parameters. For now, rely on remote for those scenarios
            return !_isRoot ? _terminalPath : UnixUtilities.SudoPath;
        }

        protected override string GetProcessArgs()
        {
            string terminalArguments = String.Format(CultureInfo.InvariantCulture, "{0} bash -c \"{1}\"", _bashCommandPrefix, _initScript);
            return !_isRoot ? terminalArguments : String.Concat(_terminalPath, " ", terminalArguments);
        }

        protected override void SetNewProcessEnvironment(ProcessStartInfo processStartInfo)
        {
            if (_environment == null)
            {
                return;
            }

            foreach (EnvironmentEntry entry in _environment)
            {
                processStartInfo.SetEnvironmentVariable(entry.Name, entry.Value);
            }
        }
    }
}
