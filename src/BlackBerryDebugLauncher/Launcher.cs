using MICore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace BlackBerryDebugLauncher
{
    [ComVisible(true)]
    [Guid("43BC8C7F-5184-4FE8-9ECF-F33A498375EE")]
    public class Launcher : IPlatformAppLauncher, IBreakHandler
    {
        private EventWaitHandle _eventCtrlC;
        private EventWaitHandle _eventTerminate;

        private IDeviceAppLauncherEventCallback _callback;
        private BlackBerryLaunchOptions _launchOptions;

        private static int HostID = 1;

        public void Initialize(string registryRoot, IDeviceAppLauncherEventCallback eventCallback)
        {
            _callback = eventCallback;
        }

        /// <summary>
        /// Initializes the launcher from the launch settings
        /// </summary>
        /// <param name="exePath">[Required] Path to the executable provided in the VsDebugTargetInfo by the project system. Some launchers may ignore this.</param>
        /// <param name="args">[Optional] Arguments to the executable provided in the VsDebugTargetInfo by the project system. Some launchers may ignore this.</param>
        /// <param name="dir">[Optional] Working directory of the executable provided in the VsDebugTargetInfo by the project system. Some launchers may ignore this.</param>
        /// <param name="launcherXmlOptions">[Required] Deserialized XML options structure</param>
        public void SetLaunchOptions(string exePath, string args, string dir, object launcherXmlOptions, TargetEngine targetEngine)
        {
            if (launcherXmlOptions == null)
                throw new ArgumentNullException("launcherXmlOptions");

            if (_callback == null)
            {
                Debug.Fail("Why is ParseLaunchOptions called before Initialize?");
                throw new InvalidOperationException();
            }

            if (_launchOptions != null)
            {
                Debug.Fail("Why is ParseLaunchOptions being called more than once?");
                throw new InvalidOperationException();
            }

            _launchOptions = new BlackBerryLaunchOptions(exePath, (MICore.Xml.LaunchOptions.BlackBerryLaunchOptions)launcherXmlOptions);
        }

        public void SetupForDebugging(out LaunchOptions debuggerLaunchOptions)
        {
            int currentPID = Process.GetCurrentProcess().Id;
            int currentHostID = Interlocked.Increment(ref HostID);
            string eventCtrlCName = string.Concat("HostCtrlC-", currentHostID, "-", currentPID);
            string eventTerminateName = string.Concat("HostTerminate-", currentHostID, "-", currentPID);

            _eventCtrlC = new EventWaitHandle(false, EventResetMode.AutoReset, eventCtrlCName);
            _eventTerminate = new EventWaitHandle(false, EventResetMode.AutoReset, eventTerminateName);

            // GDB-Host process requires a specific order of arguments:
            // 1. the name of the event, which set will trigger the Ctrl+C signal to the GDB
            // 2. the name of the event, which set will exit the host process and GDB
            // 3. the path to GDB executable itself, that will run
            // 4. optional settings for GDBHost (-s => disable custom console logs, -c => skip checking for GDB-executable existence)
            // 5. all the other arguments that should be passed to GDB (although it's possible to pass arguments to GDB via the executable path,
            //    but in practice they can't be escaped this way; that's why passing them as last arguments of the host are the recommended approach)
            var args = string.Concat(eventCtrlCName, " ", eventTerminateName, " -sc ", "\"", _launchOptions.GdbPath, "\" ", "--interpreter=mi2");

            debuggerLaunchOptions = new PipeLaunchOptions(_launchOptions.GdbHostPath, args);
            debuggerLaunchOptions.AdditionalSOLibSearchPath = _launchOptions.AdditionalSOLibSearchPath;
            debuggerLaunchOptions.DebuggerMIMode = MIMode.Gdb;
            debuggerLaunchOptions.TargetArchitecture = _launchOptions.TargetArchitecture;

            debuggerLaunchOptions.CustomLaunchSetupCommands = GetCustomLaunchSetupCommands();
            debuggerLaunchOptions.LaunchCompleteCommand = GetLaunchCompleteCommand();
        }

        private ReadOnlyCollection<LaunchCommand> GetCustomLaunchSetupCommands()
        {
            var commands = new List<LaunchCommand>();

            string fileCommand = string.Format(CultureInfo.InvariantCulture, "-file-exec-and-symbols \"{0}\"", _launchOptions.ExePath.Replace("\\", "\\\\"));
            string targetCommand = string.Format(CultureInfo.InvariantCulture, "-target-select qnx {0}:{1}", _launchOptions.TargetAddress, _launchOptions.TargetPort);
            string targetAttach = string.Format(CultureInfo.InvariantCulture, "-target-attach {0}", _launchOptions.PID);

            //commands.Add(new LaunchCommand("-gdb-set breakpoint pending on", LauncherResources.DefinePlatform));
            commands.Add(new LaunchCommand(fileCommand, LauncherResources.DefinePlatform));
            commands.Add(new LaunchCommand(targetCommand, LauncherResources.Connecting));
            commands.Add(new LaunchCommand(targetAttach, LauncherResources.Attaching));

            return commands.AsReadOnly();
        }

        private LaunchCompleteCommand GetLaunchCompleteCommand()
        {
            return LaunchCompleteCommand.ExecContinue;
        }

        public void Dispose()
        {
            if (_eventCtrlC != null)
            {
                _eventCtrlC.Dispose();
                _eventCtrlC = null;
            }

            if (_eventTerminate != null)
            {
                _eventTerminate.Dispose();
                _eventTerminate = null;
            }
        }

        public void OnResume()
        {
        }

        public void Terminate()
        {
        }

        void IBreakHandler.Break()
        {
            if (_eventCtrlC != null)
            {
                _eventCtrlC.Set();
            }
        }
    }
}
