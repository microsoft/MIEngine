// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using DebugAdapterRunner;
using DebuggerTesting.OpenDebug.Commands;
using DebuggerTesting.Settings;
using DebuggerTesting.Utilities;
using Xunit;
using Xunit.Abstractions;
using DarRunner = DebugAdapterRunner.DebugAdapterRunner;

namespace DebuggerTesting.OpenDebug.CrossPlatCpp
{
    /// <summary>
    /// Provides a wrapper to initialize and close a debug adapter.
    /// The constructur will initiate the connection to the debug adapter.
    /// Disposing this wrapper will close the connection.
    /// </summary>
    public class DebuggerRunner : DisposableObject, IDebuggerRunner
    {
#if DEBUG
        private IDebugFailUI savedDebugFailUI;
#endif

        private string engineLogPath;
        private static Regex _engineLogRegEx;

        #region Constructor/Create/Dispose

        private DebuggerRunner(ILoggingComponent logger, ITestSettings testSettings, IEnumerable<Tuple<string, CallbackRequestHandler>> callbackHandlers)
        {
            Parameter.ThrowIfNull(testSettings, nameof(testSettings));
            this.OutputHelper = logger.OutputHelper;
            this.DebuggerSettings = testSettings.DebuggerSettings;
#if DEBUG
            this.savedDebugFailUI = UDebug.DebugFailUI;
            UDebug.DebugFailUI = new XUnitDefaultDebugFailUI(this.OutputHelper);
#endif

            this.WriteLine("Creating Debug Adapter Runner.");
            string adapterId = (this.DebuggerSettings.DebuggerType == SupportedDebugger.VsDbg) ? "cppvsdbg" : "cppdbg";
            string debugAdapterRelativePath = this.DebuggerSettings.DebuggerAdapterPath ?? "OpenDebugAD7";
            string debugAdapterFullPath = Path.Combine(PathSettings.DebugAdaptersPath, debugAdapterRelativePath);

            this.WriteLine("DebugAdapterPath: " + debugAdapterFullPath);

            Assert.True(Directory.Exists(Path.GetDirectoryName(debugAdapterFullPath)), "Debug Adapter Path {0} does not exist.".FormatInvariantWithArgs(debugAdapterFullPath));

            // Output engine log to temp folder if requested.
            this.engineLogPath = Path.Combine(PathSettings.TempPath, "EngineLog-{0}-{1}-{2}.log".FormatInvariantWithArgs(testSettings.Name, testSettings.DebuggerSettings.DebuggeeArchitecture.ToString(), testSettings.DebuggerSettings.DebuggerType.ToString()));
            this.WriteLine("Logging engine to: {0}", this.engineLogPath);

            this.DarRunner = new DarRunner(
                adapterPath: debugAdapterFullPath,
                traceDebugAdapterOutput: DiagnosticsSettings.LogDebugAdapter ? DebugAdapterRunner.TraceDebugAdapterMode.Memory : DebugAdapterRunner.TraceDebugAdapterMode.None,
                engineLogPath: this.engineLogPath,
                pauseForDebugger: false,
                isVsDbg: this.DebuggerSettings.DebuggerType == SupportedDebugger.VsDbg,
                isNative: true,
                responseTimeout: 5000);

            if (callbackHandlers != null)
            {
                foreach (Tuple<string, CallbackRequestHandler> handlerPair in callbackHandlers)
                {
                    this.DarRunner.AddCallbackRequestHandler(handlerPair.Item1, handlerPair.Item2);
                }
            }

            this.WriteLine("Initializing debugger.");
            this.initializeResponse = this.RunCommand(new InitializeCommand(adapterId));
        }

        public static IDebuggerRunner Create(ILoggingComponent logger, ITestSettings testSettings, IEnumerable<Tuple<string, CallbackRequestHandler>> callbackHandlers)
        {
            return new DebuggerRunner(logger, testSettings, callbackHandlers);
        }

        protected override void Dispose(bool isDisposing)
        {
            // If we disposed is called and the adapter is still running, send
            // a disconnect command.
            if (isDisposing)
            {
#if DEBUG
                UDebug.DebugFailUI = this.savedDebugFailUI;
#endif

                this.DisconnectDebugAdapter(throwOnFailure: false);
            }
            else
            {
                try
                {
                    // If in the finalizer, just try to kill the process
                    if (this.DarRunner?.DebugAdapter?.HasExited == false)
                    {
                        ProcessHelper.KillProcess(this.DarRunner.DebugAdapter, recurse: true);
                    }
                }
                catch (InvalidOperationException e)
                { 
                    // If the process has exited but is already cleaned up, it will throw an InvalidOperationException with the message "Process has not been started"
                    this.WriteLine("InvalidOperationException: {0}. /n StackTrace: {1}", e.Message, e.StackTrace);
                }
            }

            base.Dispose(isDisposing);
        }

        /// <summary>
        /// Call this to close the debug adapter. If child processes still exist
        /// this will throw.
        /// </summary>
        public void DisconnectAndVerify()
        {
            this.DisconnectDebugAdapter(throwOnFailure: true);
        }

        private void DisconnectDebugAdapter(bool throwOnFailure)
        {
            if (this.DarRunner?.DebugAdapter?.HasExited == false)
            {
                this.Comment("Disconnecting Debug Adapter Runner.");
                this.RunCommand(new DisconnectCommand());

                // Wait 60 seconds for the adapter to close
                int attempts = 0;
                int maxAttempts = 10 * 60; // 60 seconds
                while (this.DarRunner.DebugAdapter.HasExited == false &&
                       attempts < maxAttempts)
                {
                    Thread.Sleep(100);
                    attempts++;
                }
            }

            // If the adapter is still running, forcibly stop it
            if (this.DarRunner?.DebugAdapter?.HasExited == false)
            {
                this.Comment("Killing Debug Adapter Runner.");
                int killCount = ProcessHelper.KillProcess(this.DarRunner.DebugAdapter, recurse: true);
                this.WriteLine("Killed {0} process(es).", killCount);

                // At this point the test is likely throwing a failure exception. 
                // Failing the test due to failed cleanup will probably mask the error.
                // Just log the output to the test log.
                this.WriteLine("ERROR: Debug adapter was still running. Found and killed extra processes.");
                Assert.False(throwOnFailure, "ERROR: Debug adapter still running. Processes not cleaned up properly.");
            }

            if (!throwOnFailure && DiagnosticsSettings.LogDebugAdapter)
            {
                this.Comment("Debug Adapter Log");
                this.WriteLine(this.DarRunner.DebugAdapterOutput);
            }

            // Check for any abandoned debugger files or processes
            this.CheckDebuggerArtifacts(throwOnFailure);
        }

        private static Regex EngineLogRegEx
        {
            get
            {
                const string engineLogArtifacts = @".*TempFile=(?<tempFile>.+)|" +
                                                  @".*ShellPid=(?<shellPid>\d+)|" +
                                                  @".*DebuggerPid=(?<debuggerPid>\d+)";

                if (_engineLogRegEx == null)
                    _engineLogRegEx = new Regex(engineLogArtifacts, RegexOptions.Compiled | RegexOptions.Multiline);
                return _engineLogRegEx;
            }
        }

        /// <summary>
        /// Check for abandoned files or processes
        /// </summary>
        /// <param name="throwOnFailure">Fail the test if abandoned artifacts found</param>
        private void CheckDebuggerArtifacts(bool throwOnFailure)
        {
            try
            {
                if (string.IsNullOrEmpty(this.engineLogPath) || !File.Exists(this.engineLogPath))
                    return;

                string engineLogContent = File.ReadAllText(this.engineLogPath);

                // Add the MIEngine log to the test log (if it is enabled)
                if (!throwOnFailure && DiagnosticsSettings.LogMIEngine)
                {
                    this.Comment("Engine Log");
                    this.WriteLine(engineLogContent);
                }

                // Parse the mi engine's log to find associated temp files and debugger pids
                // This only checks for any process ids or files logged by the mi engine
                // and may not be a complete list.
                List<string> tempFiles = new List<string>(3);
                List<int> associatedPids = new List<int>(2);
                foreach (Match match in EngineLogRegEx.Matches(engineLogContent))
                {
                    string tempFile = match.Groups?["tempFile"]?.Value;
                    string shellPid = match.Groups?["shellPid"]?.Value;
                    string debuggerPid = match.Groups?["debuggerPid"]?.Value;

                    if (!string.IsNullOrEmpty(tempFile))
                        tempFiles.Add(tempFile);
                    else if (!string.IsNullOrEmpty(shellPid))
                        associatedPids.Add(int.Parse(shellPid, CultureInfo.InvariantCulture));
                    else if (!string.IsNullOrEmpty(debuggerPid))
                        associatedPids.Add(int.Parse(debuggerPid, CultureInfo.InvariantCulture));
                }

                // Check for abandoned temp files
                foreach (string tempFile in tempFiles)
                {
                    if (File.Exists(tempFile))
                    {
                        this.WriteLine("ERROR: Debug Adapter abandoned temp file: " + tempFile);
                        TryDeleteFile(tempFile);
                        Assert.False(throwOnFailure, "ERROR: Debug Adapter abandoned temp file: " + tempFile);
                    }
                }

                // Check for abandoned processes
                foreach (int associatedPid in associatedPids)
                {
                    if (ProcessHelper.IsProcessRunning(associatedPid))
                    {
                        this.WriteLine("ERROR: Debug Adapter abandoned process: " + associatedPid.ToString(CultureInfo.InvariantCulture));
                        ProcessHelper.KillProcess(associatedPid, recurse: true);
                        Assert.False(throwOnFailure, "ERROR: Debug Adapter abandoned process: " + associatedPid.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }
            catch (Exception ex)
            {
                this.WriteLine("Error checking engine log. {0}", UDebug.ExceptionToString(ex));
            }
        }

        private static void TryDeleteFile(string tempFile)
        {
            // This is already in cleanup code, ignore exceptions
            try
            {
                File.Delete(tempFile);
            }
            catch (IOException)
            { }
        }

        #endregion

        #region ILoggingComponent Members

        public ITestOutputHelper OutputHelper { get; private set; }

        #endregion

        #region IDebuggerRunner Members

        public IDebuggerSettings DebuggerSettings { get; private set; }

        private InitializeResponseValue initializeResponse { get; set; }
        InitializeResponseValue IDebuggerRunner.InitializeResponse
        {
            get
            {
                return this.initializeResponse;
            }
        }

        public bool ErrorEncountered { get; set; }

        public DarRunner DarRunner { get; private set; }

        public R RunCommand<R>(ICommandWithResponse<R> command, params IEvent[] expectedEvents)
        {
            return command.Run(this, expectedEvents);
        }

        public void RunCommand(ICommand command, params IEvent[] expectedEvents)
        {
            command.Run(this, expectedEvents);
        }

        /// <summary>
        /// This gets populated when a stopped event occurs. It has the thread
        /// id of the stopped thread.
        /// </summary>
        public int StoppedThreadId
        {
            get
            {
                return this.DarRunner.CurrentThreadId;
            }
        }

        public IRunBuilder Expects
        {
            get { return new RunBuilder(this); }
        }

        #endregion

        #region XUnitDefaultDebugFailUI

#if DEBUG
        /// <summary>
        /// Implements the Debug Fail UI for the UDebug class
        /// </summary>
        internal class XUnitDefaultDebugFailUI : IDebugFailUI
        {
            private ITestOutputHelper outputHelper;

            public XUnitDefaultDebugFailUI(ITestOutputHelper outputHelper)
            {
                this.outputHelper = outputHelper;
            }

            DebugUIResult IDebugFailUI.ShowFailure(string message, string callLocation, string exception, string callStack)
            {
                string header = exception + ": " + message;
                this.outputHelper.WriteLine(header + Environment.NewLine + callStack);
                Debug.Fail(header, callStack);
                return DebugUIResult.Ignore;
            }
        }
#endif //DEBUG

        #endregion
    }
}
