// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using libadb;
using JDbg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace AndroidDebugLauncher
{
    [ComVisible(true)]
    [Guid("C9A403DA-D3AA-4632-A572-E81FF6301E9B")]
    sealed public class Launcher : IPlatformAppLauncher
    {
        private readonly CancellationTokenSource _gdbServerExecCancellationSource = new CancellationTokenSource();
        private AndroidLaunchOptions _launchOptions;
        private InstallPaths _installPaths;
        private MICore.WaitLoop _waitLoop;
        private int _jdbPortNumber;
        private AdbShell _shell;
        private int _appProcessId;
        private bool _onResumeSucceeded;
        private bool _isUsingArmEmulator;
        private JDbg.JDbg _jdbg;
        private IDeviceAppLauncherEventCallback _eventCallback;
        private static bool s_sentArmEmulatorWarning;
        private TargetEngine _targetEngine;

        private const string LogcatServiceMessage_SourceId = "1CED0608-638C-4B00-A1D2-CE56B1B672FA";
        private const int LogcatServiceMessage_NewProcess = 0;

        void IPlatformAppLauncher.Initialize(string registryRoot, IDeviceAppLauncherEventCallback eventCallback)
        {
            if (string.IsNullOrEmpty(registryRoot))
                throw new ArgumentNullException("registryRoot");
            if (eventCallback == null)
                throw new ArgumentNullException("eventCallback");

            _eventCallback = eventCallback;
            RegistryRoot.Set(registryRoot);
        }

        void IPlatformAppLauncher.SetLaunchOptions(string exePath, string args, string dir, object launcherXmlOptions, TargetEngine targetEngine)
        {
            if (launcherXmlOptions == null)
                throw new ArgumentNullException("launcherXmlOptions");

            var androidXmlOptions = (MICore.Xml.LaunchOptions.AndroidLaunchOptions)launcherXmlOptions;

            if (_eventCallback == null)
            {
                Debug.Fail("Why is ParseLaunchOptions called before Initialize?");
                throw new InvalidOperationException();
            }

            if (_launchOptions != null)
            {
                Debug.Fail("Why is ParseLaunchOptions being called more than once?");
                throw new InvalidOperationException();
            }

            _launchOptions = new AndroidLaunchOptions(androidXmlOptions, targetEngine);
            _targetEngine = targetEngine;
        }

        void IPlatformAppLauncher.SetupForDebugging(out LaunchOptions result)
        {
            if (_launchOptions == null)
            {
                Debug.Fail("Why is SetupForDebugging being called before ParseLaunchOptions?");
                throw new InvalidOperationException();
            }

            ManualResetEvent doneEvent = new ManualResetEvent(false);
            var cancellationTokenSource = new CancellationTokenSource();
            ExceptionDispatchInfo exceptionDispatchInfo = null;
            LaunchOptions localLaunchOptions = null;

            _waitLoop = new MICore.WaitLoop(LauncherResources.WaitDialogText);

            // Do the work on a worker thread to avoid blocking the UI. Use ThreadPool.QueueUserWorkItem instead
            // of Task.Run to avoid needing to unwrap the AggregateException.
            ThreadPool.QueueUserWorkItem((object o) =>
                {
                    string launchErrorTelemetryResult = null;

                    try
                    {
                        localLaunchOptions = SetupForDebuggingWorker(cancellationTokenSource.Token);
                        launchErrorTelemetryResult = "None";
                    }
                    catch (Exception e)
                    {
                        exceptionDispatchInfo = ExceptionDispatchInfo.Capture(e);
                        if (!(e is OperationCanceledException))
                        {
                            launchErrorTelemetryResult = Telemetry.GetLaunchErrorResultValue(e);
                        }
                    }

                    doneEvent.Set();

                    if (launchErrorTelemetryResult != null)
                    {
                        Telemetry.SendLaunchError(launchErrorTelemetryResult);
                    }
                }
            );

            _waitLoop.Wait(doneEvent, cancellationTokenSource);

            if (exceptionDispatchInfo != null)
            {
                exceptionDispatchInfo.Throw();
            }
            if (localLaunchOptions == null)
            {
                Debug.Fail("No result provided? Should be impossible.");
                throw new InvalidOperationException();
            }

            result = localLaunchOptions;
        }

        private class NamedAction
        {
            public readonly string Name;
            public readonly Action Action;

            public NamedAction(string name, Action action)
            {
                this.Name = name;
                this.Action = action;
            }
        }

        private LaunchOptions SetupForDebuggingWorker(CancellationToken token)
        {
            CancellationTokenRegistration onCancelRegistration = token.Register(() =>
            {
                _gdbServerExecCancellationSource.Cancel();
            });

            using (onCancelRegistration)
            {
                // TODO: Adb exception messages should be improved. Example, if ADB is not started, this is returned:
                //    +		[libadb.AdbException]	{"Could not connect to the adb.exe server. See InnerException for details."}	libadb.AdbException
                // 'See InnerException for details.' should not be there. It should just add the inner exception message:
                //    [System.Net.Sockets.SocketException]	{"No connection could be made because the target machine actively refused it 127.0.0.1:5037"}	System.Net.Sockets.SocketException

                Device device = null;
                string workingDirectory = null;
                string gdbServerRemotePath = null;
                string gdbServerSocketDescription = null;
                string exePath = null;
                Task taskGdbServer = null;
                int gdbPortNumber = 0;
                int progressCurrentIndex = 0;
                int progressStepCount = 0;

                List<NamedAction> actions = new List<NamedAction>();

                actions.Add(new NamedAction(LauncherResources.Step_ResolveInstallPaths, () =>
                {
                    _installPaths = InstallPaths.Resolve(token, _launchOptions);
                }));

                actions.Add(new NamedAction(LauncherResources.Step_ConnectToDevice, () =>
                {
                    Adb adb;
                    try
                    {
                        adb = new Adb(_installPaths.SDKRoot);
                    }
                    catch (ArgumentException)
                    {
                        throw new LauncherException(Telemetry.LaunchFailureCode.InvalidAndroidSDK, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_InvalidAndroidSDK, _installPaths.SDKRoot));
                    }

                    try
                    {
                        adb.Start();
                        device = adb.GetDeviceById(_launchOptions.DeviceId);

                        // There is a rare case, which we have seen it a few times now with the Android emulator where the device will be initially
                        // in the offline state. But after a very short amount of time it comes online. Retry waiting for this.
                        if (device.GetState().HasFlag(DeviceState.Offline))
                        {
                            // Add in an extra progress step and update the dialog
                            progressStepCount++;

                            _waitLoop.SetProgress(progressStepCount, progressCurrentIndex, LauncherResources.Step_WaitingForDeviceToComeOnline);
                            progressCurrentIndex++;

                            const int waitTimePerIteration = 50;
                            const int maxTries = 5000 / waitTimePerIteration; // We will wait for up to 5 seconds

                            // NOTE: libadb has device discovery built in which we could allegedly use instead of a retry loop,
                            // but I couldn't get this to work (though the problem is so rare, I had a lot of trouble testing it), so just using
                            // a retry loop.
                            for (int cTry = 0; true; cTry++)
                            {
                                if (cTry == maxTries)
                                {
                                    throw new LauncherException(Telemetry.LaunchFailureCode.DeviceOffline, LauncherResources.Error_DeviceOffline);
                                }

                                // Sleep for a little while unless this operation is canceled
                                if (token.WaitHandle.WaitOne(waitTimePerIteration))
                                {
                                    throw new OperationCanceledException();
                                }

                                if (!device.GetState().HasFlag(DeviceState.Offline))
                                {
                                    break; // we are no longer offline
                                }
                            }
                        }
                    }
                    catch (AdbException)
                    {
                        throw new LauncherException(Telemetry.LaunchFailureCode.DeviceNotResponding, LauncherResources.Error_DeviceNotResponding);
                    }
                }));

                actions.Add(new NamedAction(LauncherResources.Step_InspectingDevice, () =>
                {
                    try
                    {
                        DeviceAbi[] allowedAbis;
                        switch (_launchOptions.TargetArchitecture)
                        {
                            case TargetArchitecture.ARM:
                                allowedAbis = new DeviceAbi[] { DeviceAbi.armeabi, DeviceAbi.armeabiv7a };
                                break;

                            case TargetArchitecture.ARM64:
                                allowedAbis = new DeviceAbi[] { DeviceAbi.arm64v8a };
                                break;

                            case TargetArchitecture.X86:
                                allowedAbis = new DeviceAbi[] { DeviceAbi.x86 };
                                break;

                            case TargetArchitecture.X64:
                                allowedAbis = new DeviceAbi[] { DeviceAbi.x64 };
                                break;

                            default:
                                Debug.Fail("New target architucture support added without updating this code???");
                                throw new InvalidOperationException();
                        }

                        if (!DoesDeviceSupportAnyAbi(device, allowedAbis))
                        {
                            throw GetBadDeviceAbiException(device.Abi);
                        }

                        if (_launchOptions.TargetArchitecture == TargetArchitecture.ARM && device.IsEmulator)
                        {
                            _isUsingArmEmulator = true;
                        }

                        _shell = device.Shell;
                    }
                    catch (AdbException)
                    {
                        throw new LauncherException(Telemetry.LaunchFailureCode.DeviceNotResponding, LauncherResources.Error_DeviceNotResponding);
                    }

                    VerifySdkVersion();

                    string pwdCommand = string.Concat("run-as ", _launchOptions.Package, " /system/bin/sh -c pwd");
                    ExecCommand(pwdCommand);
                    workingDirectory = PwdOutputParser.ExtractWorkingDirectory(_shell.Out, _launchOptions.Package);

                    if (_targetEngine == TargetEngine.Native)
                    {
                        gdbServerRemotePath = GetGdbServerPath(workingDirectory);
    
                        KillOldInstances(gdbServerRemotePath);
                    }
                }));

                if (!_launchOptions.IsAttach)
                {
                    actions.Add(new NamedAction(LauncherResources.Step_StartingApp, () =>
                    {
                        string activateCommand = string.Concat("am start -D -n ", _launchOptions.Package, "/", _launchOptions.LaunchActivity);
                        ExecCommand(activateCommand);
                        ValidateActivityManagerOutput(activateCommand, _shell.Out);
                    }));
                }

                actions.Add(new NamedAction(LauncherResources.Step_GettingAppProcessId, () =>
                {
                    _appProcessId = GetAppProcessId();
                }));

                if (_targetEngine == TargetEngine.Native)
                {
                    actions.Add(new NamedAction(LauncherResources.Step_StartGDBServer, () =>
                    {
                    // We will default to using a unix socket with gdbserver as this is what the ndk-gdb script uses. Though we have seen
                    // some machines where this doesn't work and we fall back to TCP instead.
                    const bool useUnixSocket = true;

                        taskGdbServer = StartGdbServer(gdbServerRemotePath, workingDirectory, useUnixSocket, out gdbServerSocketDescription);
                    }));
                }

                actions.Add(new NamedAction(LauncherResources.Step_PortForwarding, () =>
                {
                    // TODO: Use a dynamic socket
                    gdbPortNumber = 5039;
                    _jdbPortNumber = 65534;

                    if (_targetEngine == TargetEngine.Native)
                    {
                        device.Forward(string.Format(CultureInfo.InvariantCulture, "tcp:{0}", gdbPortNumber), gdbServerSocketDescription);
                    }

                    if (!_launchOptions.IsAttach)
                    {
                        device.Forward(string.Format(CultureInfo.InvariantCulture, "tcp:{0}", _jdbPortNumber), string.Format(CultureInfo.InvariantCulture, "jdwp:{0}", _appProcessId));
                    }
                }));

                if (_targetEngine == TargetEngine.Native)
                {
                    actions.Add(new NamedAction(LauncherResources.Step_DownloadingFiles, () =>
                    {
                        //pull binaries from the emulator/device
                        var fileSystem = device.FileSystem;
    
                        string app_process_suffix = String.Empty;
                        switch (_launchOptions.TargetArchitecture)
                        {
                            case TargetArchitecture.X86:
                            case TargetArchitecture.ARM:
                                app_process_suffix = "32";
                                break;
                            case TargetArchitecture.X64:
                            case TargetArchitecture.ARM64:
                                app_process_suffix = "64";
                                break;
                            default:
                                Debug.Fail("Unsupported Target Architecture!");
                                break;
                        }
    
                        string app_process = String.Concat("app_process", app_process_suffix);
                        exePath = Path.Combine(_launchOptions.IntermediateDirectory, app_process);
    
                        bool retry = false;
                        try
                        {
                            fileSystem.Download(@"/system/bin/" + app_process, exePath, true);
                        }
                        catch (AdbException) when (String.Compare(app_process_suffix, "32", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            // Older devices don't have an 'app_process32', only an 'app_process', so retry
                            // NOTE: libadb doesn't have an error code property to verify that this is caused
                            // by the file not being found.
                            retry = true;
                        }
    
                        if (retry)
                        {
                            app_process = "app_process";
                            exePath = Path.Combine(_launchOptions.IntermediateDirectory, app_process);
                            fileSystem.Download(@"/system/bin/app_process", exePath, true);
                        }
                        
                        //on 64 bit, 'linker64' is the 64bit version and 'linker' is the 32 bit version 
                        string suffix64bit = String.Empty;
                        if (_launchOptions.TargetArchitecture == TargetArchitecture.X64 || _launchOptions.TargetArchitecture == TargetArchitecture.ARM64)
                        {
                            suffix64bit = "64";
                        }
    
                        string linker = String.Concat("linker", suffix64bit);
                        fileSystem.Download(String.Concat(@"/system/bin/", linker), Path.Combine(_launchOptions.IntermediateDirectory, linker), true);
    
                        //on 64 bit, libc.so lives in /system/lib64/, on 32 bit it lives in simply /system/lib/
                        fileSystem.Download(@"/system/lib" + suffix64bit + "/libc.so", Path.Combine(_launchOptions.IntermediateDirectory, "libc.so"), true);
                    }));
                }
    
                progressStepCount = actions.Count;

                foreach (NamedAction namedAction in actions)
                {
                    token.ThrowIfCancellationRequested();

                    _waitLoop.SetProgress(progressStepCount, progressCurrentIndex, namedAction.Name);
                    progressCurrentIndex++;
                    namedAction.Action();
                }

                _waitLoop.SetProgress(progressStepCount, progressStepCount, string.Empty);

                if (_targetEngine == TargetEngine.Native && taskGdbServer.IsCompleted)
                {
                    token.ThrowIfCancellationRequested();
                    throw new LauncherException(Telemetry.LaunchFailureCode.GDBServerFailed, LauncherResources.Error_GDBServerFailed);
                }

                if (_launchOptions.LogcatServiceId != Guid.Empty)
                {
                    _eventCallback.OnCustomDebugEvent(_launchOptions.LogcatServiceId, new Guid(LogcatServiceMessage_SourceId), LogcatServiceMessage_NewProcess, _appProcessId, null);
                }

                LaunchOptions launchOptions = null;
                if (_targetEngine == TargetEngine.Native)
                {
                    launchOptions = new LocalLaunchOptions(_installPaths.GDBPath, string.Format(CultureInfo.InvariantCulture, ":{0}", gdbPortNumber));
                    launchOptions.ExePath = exePath;
                }
                else
                {
                    launchOptions = new JavaLaunchOptions(_launchOptions.JVMHost, _launchOptions.JVMPort, _launchOptions.SourceRoots);
                }

                launchOptions.AdditionalSOLibSearchPath = _launchOptions.AdditionalSOLibSearchPath;
                launchOptions.TargetArchitecture = _launchOptions.TargetArchitecture;
                launchOptions.WorkingDirectory = _launchOptions.IntermediateDirectory;

                launchOptions.DebuggerMIMode = MIMode.Gdb;
                
                launchOptions.VisualizerFile = "Microsoft.Android.natvis";

                return launchOptions;
            }
        }

        private void ValidateActivityManagerOutput(string activateCommand, string commandOutput)
        {
            // Example error output
            //   Starting: Intent { cmp=com.example.hellojni/.bogus }
            //   Error type 3
            //   Error: Activity class {com.example.hellojni/com.example.hellojni.bogus} does not exist.
            using (var reader = new StringReader(commandOutput))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        break;

                    if (line.StartsWith("Error:", StringComparison.Ordinal))
                    {
                        string errorMessage = line.Substring("Error:".Length).Trim();
                        throw new LauncherException(Telemetry.LaunchFailureCode.ActivityManagerFailed, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_ShellCommandFailed, activateCommand, errorMessage));
                    }
                }
            }
        }

        private int GetAppProcessId()
        {
            Debug.Assert(_shell != null, "GetAppProcessId called before m_shell is set");

            const int waitTime = 10000; // maximum time to wait for the app process to show up
            const int sleepPerIteration = 100;
            for (int iteration = 0; iteration < waitTime / sleepPerIteration; iteration++)
            {
                // Give the device/emulator time to run before asking for a process list again
                if (iteration != 0)
                {
                    // If we are attaching, we should be able to get the PID immediately.
                    // Do not wait to see if it shows up;
                    if (_launchOptions.IsAttach)
                    {
                        break;
                    }
                    Thread.Sleep(sleepPerIteration);
                }

                List<int> appProcessList = GetProcessIds(_launchOptions.Package);
                if (appProcessList.Count == 0)
                {
                    continue;
                }
                else if (appProcessList.Count != 1)
                {
                    throw new LauncherException(Telemetry.LaunchFailureCode.MultipleApplicationProcesses, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_MulltipleApplicationProcesses, _launchOptions.Package));
                }

                return appProcessList[0];
            }

            if (_launchOptions.IsAttach)
            {
                throw new LauncherException(Telemetry.LaunchFailureCode.NoReport, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_PackageIsNotRunning, _launchOptions.Package));
            }
            else
            {
                throw new LauncherException(Telemetry.LaunchFailureCode.PackageDidNotStart, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_PackageDidNotStart, _launchOptions.Package));
            }
        }

        /// <summary>
        /// Returns the process id's for any running instances of a process
        /// </summary>
        /// <param name="processName">the process name to look for pids</param>
        /// <returns>A list of process id's</returns>
        private List<int> GetProcessIds(string processName)
        {
            Debug.Assert(_shell != null, "GetProcessIds called before m_shell is set");

            ExecCommandNoLog("ps");
            var processList = new ProcessListParser(_shell.Out);

            return processList.FindProcesses(processName);
        }

        private void VerifySdkVersion()
        {
            Debug.Assert(_shell != null, "VerifySdkVersion called before m_shell is set");

            string cmd = "getprop ro.build.version.sdk";
            ExecCommand(cmd);

            int sdkVersion = 0;
            if (!int.TryParse(_shell.Out, out sdkVersion))
            {
                throw new LauncherException(Telemetry.LaunchFailureCode.BadAndroidVersionFormat, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_ShellCommandBadResults, cmd));
            }

            if (sdkVersion < 17)
            {
                throw new LauncherException(Telemetry.LaunchFailureCode.UnsupportedAndroidVersion, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_UnsupportedAPILevel, sdkVersion));
            }
        }

        private string GetGdbServerPath(string workingDirectory)
        {
            // On the android device, the gdbserver and native shared objects of an ndk app can be out /data/data/<appname>/lib/*
            // The lib directory symlinks to somewhere else on the file system, for example /data/data/<appname>/lib/ -> /data/app-lib/<appname>-1/lib
            // On 64 bit, this symlink is broken (I think this is a bug in 64 bit android).
            // We must attempt to find the gdbserver path manually then. 

            // Android issue: https://code.google.com/p/android/issues/detail?id=186010&thanks=186010&ts=1441922626

            //check the correct location for x86/arm/arm64
            string gdbServerPath = workingDirectory + "/lib/gdbserver";

            string lsCommand = string.Format(CultureInfo.InvariantCulture, "ls {0}", gdbServerPath);
            string output = ExecCommand(lsCommand);
            if (string.Compare(output, gdbServerPath, StringComparison.OrdinalIgnoreCase) == 0)
            {
                //gdbserver is symlinked correctly
                return gdbServerPath;
            }
            else if (_launchOptions.TargetArchitecture == TargetArchitecture.X64)
            {
                //start looking other places, only do this on 64 bit
                //TODO: This needs some additional testsing
                lsCommand = string.Format(CultureInfo.InvariantCulture, "ls /data/app/{0}*/lib/x86_64/gdbserver", _launchOptions.Package);
                output = ExecCommand(lsCommand);
                return output;
            }
            else
            {
                throw new LauncherException(Telemetry.LaunchFailureCode.NoGdbServer, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_NoGdbServer, gdbServerPath));
            }
        }

        private static bool DoesDeviceSupportAnyAbi(Device device, DeviceAbi[] allowedAbis)
        {
            if (allowedAbis.Contains(device.Abi))
                return true;

            if (allowedAbis.Contains(device.Abi2))
                return true;

            string abiListValue;
            if (device.Properties.TryGetPropertyByName("ro.product.cpu.abilist", out abiListValue))
            {
                string[] deviceAbis = abiListValue.Split(',');
                foreach (DeviceAbi allowedAbi in allowedAbis)
                {
                    string allowedAbiString = allowedAbi.ToString();

                    foreach (string deviceAbi in deviceAbis)
                    {
                        if (deviceAbi.Equals(allowedAbiString, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }

            return false;
        }

        private void KillOldInstances(string gdbServerProcessName)
        {
            Debug.Assert(_shell != null, "KillOldInstances called before m_shell is set");

            ExecCommand("ps");
            var processList = new ProcessListParser(_shell.Out);

            if (!_launchOptions.IsAttach)
            {
                foreach (int pid in processList.FindProcesses(_launchOptions.Package))
                {
                    KillRemoteProcess(pid);
                }
            }

            foreach (int pid in processList.FindProcesses(gdbServerProcessName))
            {
                KillRemoteProcess(pid);
            }
        }

        private void KillRemoteProcess(int pid)
        {
            Debug.Assert(_shell != null, "KillRemoteProcess called before m_shell is set");

            string killCommand = string.Format(CultureInfo.InvariantCulture, "kill -9 {0}", pid);
            ExecCommand(killCommand);
        }

        private Task StartGdbServer(string gdbServerRemotePath, string workingDirectory, bool useUnixSocket, out string gdbServerSocketDescription)
        {
            Debug.Assert(_shell != null, "StartGdbServer called before m_shell is set");

            if (_appProcessId == 0)
            {
                Debug.Fail("StartGDBServer called before m_appProcessId set.");
                throw new InvalidOperationException();
            }

            string gdbServerSocketArgument;
            if (useUnixSocket)
            {
                gdbServerSocketArgument = "+debug-socket";
                gdbServerSocketDescription = string.Concat("localfilesystem:", workingDirectory, "/debug-socket");
            }
            else
            {
                // NOTE: 61518 is a random number in the dynamic/private range. A quick internet search revealed no usages.
                gdbServerSocketArgument = "tcp:61518";
                gdbServerSocketDescription = gdbServerSocketArgument;
            }

            string optionalLoggingArguments = string.Empty;
            string optionalSuffix = string.Empty;
            if (Logger.IsEnabled)
            {
                ExecCommand(string.Concat(gdbServerRemotePath, " --version"));

                optionalLoggingArguments = GetGdbServerLoggingArguments();
                optionalSuffix = "; echo gdbserver exited with code $?";
            }

            // --debug option tells gdbserver to display extra status information about the debugging process. The --remote-debug

            string gdbServerCommand = string.Format(CultureInfo.InvariantCulture, "run-as {0} {1} {2}{3} --attach {4}{5}", _launchOptions.Package, gdbServerRemotePath, optionalLoggingArguments, gdbServerSocketArgument, _appProcessId, optionalSuffix);

            StringBuilder errorOutput = new StringBuilder();
            TaskCompletionSource<object> serverReady = new TaskCompletionSource<object>();
            Action<string> outputHandler = (string output) =>
            {
                if (Logger.IsEnabled)
                {
                    StringBuilder debugMessage = new StringBuilder("GDB SERVER: ");
                    debugMessage.Append(output);
                    debugMessage.Replace("\r", "\\r");
                    debugMessage.Replace("\n", "\\n");
                    debugMessage.Replace("\t", "\\t");
                    Logger.WriteLine(debugMessage.ToString());
                }

                // Here is the expected output from GDB Server --
                //   Attached; pid = 1313
                //   \r\n
                //   Listening on Unix socket debug-socket\r\n
                if (output.ToLowerInvariant().Contains("listening"))
                {
                    // We have now gotten to the listening stage, so we aren't in an error scenario
                    errorOutput = null;
                    serverReady.TrySetResult(null);
                }
                else if (errorOutput != null)
                {
                    // We are before 'listening' so we could be in an error scenario, save the output so that we can try and scrape it.
                    errorOutput.Append(output);
                }
            };

            _gdbServerExecCancellationSource.Token.ThrowIfCancellationRequested();

            Logger.WriteLine("ADB<-{0}", gdbServerCommand);
            Task serverExitedOrCanceled = _shell.ExecAsync(gdbServerCommand, _gdbServerExecCancellationSource.Token, outputHandler);
            int completedTask = Task.WaitAny(serverReady.Task, serverExitedOrCanceled);

            _gdbServerExecCancellationSource.Token.ThrowIfCancellationRequested();

            if (completedTask == 1)
            {
                // For some reason, there are some Android devices where unix sockets don't work. To work around, if we are using unix sockets, and
                // they fail, try again with TCP.
                if (useUnixSocket && HasGdbServerInvalidSocketError(errorOutput))
                {
                    Logger.WriteLine("Retrying GDB Server launch using TCP socket.");
                    return StartGdbServer(gdbServerRemotePath, workingDirectory, /*useUnixSocket:*/ false, out gdbServerSocketDescription);
                }

                throw new LauncherException(Telemetry.LaunchFailureCode.GDBServerFailed, LauncherResources.Error_GDBServerFailed);
            }

            return serverExitedOrCanceled;
        }

        /// <summary>
        /// Tests if the GDBServer output has a line indicating it was unable to open a unix socket
        /// </summary>
        /// <param name="errorOutput">[Optional] StringBuilder containing the output</param>
        /// <returns>true if the special error was found</returns>
        private bool HasGdbServerInvalidSocketError(/*OPTIONAL*/ StringBuilder errorOutput)
        {
            if (errorOutput == null)
                return false;

            using (var reader = new StringReader(errorOutput.ToString()))
            {
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                        return false;

                    // This error message is what GDB returns when it fails to open the unix socket
                    if (line.StartsWith("Could not open remote device", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
        }

        private string ExecCommand(string command)
        {
            Debug.Assert(_shell != null, "ExecCommand called before m_shell is set");

            Logger.WriteLine("ADB<-{0}", command);

            string response = ExecCommandNoLog(command);

            Logger.WriteTextBlock("ADB->", response);

            return response;
        }

        private string ExecCommandNoLog(string command)
        {
            Debug.Assert(_shell != null, "ExecCommand called before m_shell is set");

            if (!_shell.Exec(command))
            {
                throw new LauncherException(Telemetry.LaunchFailureCode.AdbShellFailed, string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_AdbShellFailure, command));
            }

            return _shell.Out.Trim();
        }

        private LauncherException GetBadDeviceAbiException(DeviceAbi deviceAbi)
        {
            string message = string.Format(CultureInfo.CurrentCulture, LauncherResources.Error_BadDeviceAbi, _launchOptions.TargetArchitecture, _launchOptions.DeviceId, deviceAbi);
            throw new LauncherException(Telemetry.LaunchFailureCode.BadDeviceAbi, message);
        }

        void IPlatformAppLauncher.OnResume()
        {
            if (_jdbPortNumber == 0)
            {
                throw new InvalidOperationException();
            }

            // We do not need to attach JDbg in an attach scneario
            if (!_launchOptions.IsAttach)
            {
                ThreadPool.QueueUserWorkItem(async (state) =>
                {
                    try
                    {
                        _jdbg = JDbg.JDbg.Attach(_jdbPortNumber);
                        await _jdbg.Inititalize();

                        //we will let the Dispose method on the launcher detach and close m_jdbg

                        _onResumeSucceeded = true;
                    }
                    catch (JDbg.JdwpException e)
                    {
                        MICore.Logger.WriteLine("JdwpException: {0}", e.Message);

                        string message = LauncherResources.Warning_JDbgResumeFailure;

                        if (e.ErrorCode == ErrorCode.VMUnavailable)
                        {
                            // NOTE: I have seen the following behaviors from Eclipse in this case
                            // 1. The user could already be in the debug perspective and if the have breakpoints set, they might
                            //    have hit. In these cases they might not know to go back to Eclipse to resume.
                            // 2. The user could be in the Java perspective but suddenly start debugging. However, Eclipse will
                            //    stay in the edit perspective so there may be no indication that they are debugging. Sometimes
                            //    the app just seems to run, so everything is good there. But sometimes the app doesn't resume
                            //    and so the user needs to either close Eclipse or every time switch to the debug perspective and
                            //    disconnect.
                            message = string.Concat(message, " ", LauncherResources.Warning_JdbgVMUnavailable);
                        }
                        else if (e.InnerException != null)
                        {
                            message = string.Concat(message, " ", e.InnerException.Message);
                        }

                        _eventCallback.OnWarning(message);
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore failures if Close is called
                    }
                });
            }
            else
            {
                // For attach, just set succeeded
                _onResumeSucceeded = true;
            }

            if (_isUsingArmEmulator && !s_sentArmEmulatorWarning)
            {
                _eventCallback.OnWarning(LauncherResources.Warning_ArmEmulator);

                // Set this to true so that we don't spam the user, we will not show the warning again until we restart debugging
                s_sentArmEmulatorWarning = true;
            }
        }

        public void Dispose()
        {
            // Disconnect from GDB server if we are connected
            _gdbServerExecCancellationSource.Cancel();
            _gdbServerExecCancellationSource.Dispose();

            // close the connection to JDbg
            var jdbg = Interlocked.Exchange(ref _jdbg, null);
            if (jdbg != null)
            {
                jdbg.Close();
            }

            // Do not kill the app on detach
            if (_launchOptions != null && !_launchOptions.IsAttach)
            {
                // Kill the app if it has been launched
                int appProcessId = Interlocked.Exchange(ref _appProcessId, 0);
                if (appProcessId != 0 && !_onResumeSucceeded)
                {
                    // Queue this to a worker thread as dispose may be called on the VS UI thread
                    ThreadPool.QueueUserWorkItem((object o) =>
                    {
                        ForceStopApplication(_launchOptions.Package);
                    });
                }
            }
        }

        private void ForceStopApplication(string packageName)
        {
            try
            {
                ExecCommand(string.Format(CultureInfo.InvariantCulture, @"am force-stop {0}", packageName));
            }
            catch
            {
                // If anything fails here, no reason to tell the user
            }
        }

        public void Terminate()
        {
            if (!_launchOptions.IsAttach)
            {
                ForceStopApplication(_launchOptions.Package);
            }
        }

        private string GetGdbServerLoggingArguments()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegistryRoot.Value + @"\Debugger"))
            {
                if (key == null)
                {
                    Debug.Fail("Why is Debugger key missing?");
                    return string.Empty;
                }

                var arguments = key.GetValue("GDBServerLoggingArguments") as string;
                if (!string.IsNullOrWhiteSpace(arguments))
                {
                    return arguments + " ";
                }

                return string.Empty;
            }
        }
    }
}
