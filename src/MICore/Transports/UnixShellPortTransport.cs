// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DebugEngineHost;
using Microsoft.VisualStudio.Debugger.Interop.UnixPortSupplier;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MICore
{
    /// <summary>
    /// Transport used when the debugger is started through IDebugUnixShellPort  (SSH, and possible other things in the future).
    /// </summary>
    public class UnixShellPortTransport : ITransport, IDebugUnixShellCommandCallback
    {
        private readonly object _closeLock = new object();
        private ITransportCallback _callback;
        private Logger _logger;
        private string _startRemoteDebuggerCommand;
        private IDebugUnixShellAsyncCommand _asyncCommand;
        private bool _bQuit;
        private bool _debuggerLaunched = false;
        private UnixShellPortLaunchOptions _launchOptions;

        private const string ErrorPrefix = "Error:";
        private const string ShellScriptName = "GetClrDbg.sh";

        public UnixShellPortTransport()
        {
        }

        public void Init(ITransportCallback transportCallback, LaunchOptions options, Logger logger, HostWaitLoop waitLoop = null)
        {
            _launchOptions = (UnixShellPortLaunchOptions)options;
            _callback = transportCallback;
            _logger = logger;
            _startRemoteDebuggerCommand = _launchOptions.StartRemoteDebuggerCommand;

            if (_launchOptions.DebuggerMIMode == MIMode.Clrdbg)
            {
                if (!UnixShellPortLaunchOptions.HasSuccessfulPreviousLaunch(_launchOptions))
                {
                    waitLoop?.SetText(MICoreResources.Info_InstallingDebuggerOnRemote);
                    string remoteDebuggerLocation = null;
                    try
                    {
                        remoteDebuggerLocation = DownloadAndCopyFileToRemote(_launchOptions.DebuggerInstallationDirectory, _launchOptions.GetClrDbgUrl).Result;
                    }
                    catch (Exception e)
                    {
                        // Even if downloading & copying to remote fails, we will still try to invoke the script as it might already exist.
                        string message = String.Format(CultureInfo.CurrentUICulture, MICoreResources.Warning_DownloadingClrDbgToRemote, e.Message);
                        _callback.AppendToInitializationLog(message);
                    }
                }
            }

            _callback.AppendToInitializationLog(string.Format(CultureInfo.CurrentUICulture, MICoreResources.Info_StartingUnixCommand, _startRemoteDebuggerCommand));
            _launchOptions.UnixPort.BeginExecuteAsyncCommand(_startRemoteDebuggerCommand, this, out _asyncCommand);
        }

        /// <summary>
        /// Downloads and copies the GetClrDbg.sh shell script to the remote machine.
        /// </summary>
        /// <param name="remoteDirectory">Location on the remote machine.</param>
        /// <param name="getclrdbgUri">URI of the GetClrDbg.sh script.</param>
        /// <returns>Full path of the location of GetClrDbg.sh on the remote machine.</returns>
        private async Task<string> DownloadAndCopyFileToRemote(string remoteDirectory, string getclrdbgUri)
        {
            string localFile = Path.GetTempFileName();
            string remoteFilePath = null;
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetStreamAsync(getclrdbgUri))
                    {
                        using (TextReader textReader = new StreamReader(response))
                        {
                            using (Stream destinationStream = File.Create(localFile))
                            {
                                await response.CopyToAsync(destinationStream);
                            }
                        }
                    }
                }

                _launchOptions.UnixPort.MakeDirectory(remoteDirectory);
                remoteFilePath = remoteDirectory + Path.AltDirectorySeparatorChar + ShellScriptName;
                _launchOptions.UnixPort.CopyFile(localFile, remoteFilePath);
            }
            finally
            {
                if (File.Exists(localFile))
                {
                    File.Delete(localFile);
                }
            }

            return remoteFilePath;
        }

        public void Close()
        {
            lock (_closeLock)
            {
                if (_bQuit)
                    return;
                _bQuit = true;

                _asyncCommand.Abort();
            }
        }

        public void Send(string cmd)
        {
            _logger?.WriteLine("<-" + cmd);
            _logger?.Flush();
            _asyncCommand.WriteLine(cmd);
        }

        int ITransport.DebuggerPid
        {
            get
            {
                return 0; // this isn't known, and shouldn't be needed
            }
        }

        bool ITransport.IsClosed
        {
            get { return _bQuit; }
        }

        void IDebugUnixShellCommandCallback.OnOutputLine(string line)
        {
            if (!_debuggerLaunched)
            {
                if (_launchOptions.DebuggerMIMode != MIMode.Clrdbg)
                {
                    _debuggerLaunched = true;
                }
                else
                {
                    if (line != null && line.StartsWith(ErrorPrefix, System.StringComparison.OrdinalIgnoreCase))
                    {
                        _callback.OnStdErrorLine(line.Substring(ErrorPrefix.Length).Trim());
                    }
                    else
                    {
                        _callback.OnStdOutLine(line);
                    }

                    if (line.Equals("Info: Launching clrdbg"))
                    {
                        _debuggerLaunched = true;
                        UnixShellPortLaunchOptions.SetSuccessfulLaunch(_launchOptions);
                        return;
                    }
                }
            }

            _logger?.WriteLine("->" + line);
            _logger?.Flush();

            if (!string.IsNullOrEmpty(line))
            {
                _callback.OnStdOutLine(line);
            }
        }

        void IDebugUnixShellCommandCallback.OnExit(string exitCode)
        {
            if (!_bQuit)
            {
                _callback.AppendToInitializationLog(string.Format(CultureInfo.InvariantCulture, "{0} exited with code {1}.", _startRemoteDebuggerCommand, exitCode ?? "???"));

                _bQuit = true;
                try
                {
                    _callback.OnDebuggerProcessExit(exitCode);
                }
                catch
                {
                    // eat exceptions on this thread so we don't bring down VS
                }
            }
        }
    }
}
