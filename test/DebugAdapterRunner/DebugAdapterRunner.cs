// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using OpenDebug;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace DebugAdapterRunner
{
    /// <summary>
    /// Delegate for a callback request handler. For example, handler for `runInTerminal`
    /// </summary>
    /// <param name="runner">The runner instance</param>
    /// <param name="dispatcherRequest">The dispatcher request</param>
    public delegate void CallbackRequestHandler(DebugAdapterRunner runner, DispatcherRequest dispatcherRequest);

    public enum TraceDebugAdapterMode
    {
        None = 0,
        Console = 1,
        Memory = 2,
    }

    public class DebugAdapterRunner
    {
        // Whether the tool will dump requests and responses between the debug adapter and itself
        private TraceDebugAdapterMode _traceDebugAdapterOutput = TraceDebugAdapterMode.None;

        private volatile int _seq;

        private bool _hasAsserted;

        private string _assertionFileName;

        private readonly Action<string> _errorLogger;

        // The timeout for getting a response from the debug adapter
        public int ResponseTimeout;

        // Reference to debug adapter process
        public Process DebugAdapter;

        // The current thread id which is automatically updated when the tool receives a stopped event
        public int CurrentThreadId;

        public DateTime StartTime { get; } = DateTime.Now;

        // Keep a trace of the debug adapter output if requested
        private StringBuilder _debugAdapterOutput = new StringBuilder();

        private IDictionary<string, CallbackRequestHandler> _callbackHandlers = new Dictionary<string, CallbackRequestHandler>();

        private static readonly Encoding s_utf8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        // Current list of responses received from the debug adapter
        public List<string> Responses { get; private set; }

        public string DebugAdapterOutput
        {
            get { return _debugAdapterOutput.ToString(); }
        }

        public void AppendLineToDebugAdapterOutput(string line)
        {
            if (_traceDebugAdapterOutput == TraceDebugAdapterMode.Memory)
            {
                _debugAdapterOutput.AppendLine(line);
            }
        }


        public DebugAdapterRunner(
            string adapterPath,
            TraceDebugAdapterMode traceDebugAdapterOutput = TraceDebugAdapterMode.None,
            bool engineLogging = false,
            string engineLogPath = null,
            bool pauseForDebugger = false,
            bool isVsDbg = false,
            bool isNative = true,
            int responseTimeout = 5000,
            Action<string> errorLogger = null,
            bool redirectVSAssert = false,
            IEnumerable<KeyValuePair<string, string>> additionalEnvironmentVariables = null)
        {
            this._errorLogger = errorLogger;

            List<string> adapterArgs = new List<string>();
            if (isVsDbg)
            {
                adapterArgs.Add("--interpreter=vscode");
            }
            else
            {
                if (traceDebugAdapterOutput != TraceDebugAdapterMode.None)
                {
                    adapterArgs.Add("--trace=response");
                }
            }

            if (pauseForDebugger)
            {
                adapterArgs.Add("--pauseForDebugger");
            }

            if (!string.IsNullOrEmpty(engineLogPath))
            {
                adapterArgs.Add("--engineLogging=" + engineLogPath);
            }
            else if (engineLogging)
            {
                adapterArgs.Add("--engineLogging");
            }

            StartDebugAdapter(adapterPath, string.Join(" ", adapterArgs), traceDebugAdapterOutput, pauseForDebugger, redirectVSAssert, responseTimeout, additionalEnvironmentVariables);
        }

        public DebugAdapterRunner(
            string adapterPath,
            string adapterArgs,
            TraceDebugAdapterMode traceDebugAdapterOutput,
            bool pauseForDebugger,
            bool redirectVSAssert,
            int responseTimeout,
            Action<string> errorLogger,
            IEnumerable<KeyValuePair<string, string>> additionalEnvironmentVariables)
        {
            _errorLogger = errorLogger;

            StartDebugAdapter(adapterPath, adapterArgs, traceDebugAdapterOutput, pauseForDebugger, redirectVSAssert, responseTimeout, additionalEnvironmentVariables);
        }

        private void StartDebugAdapter(
            string adapterPath,
            string adapterArgs,
            TraceDebugAdapterMode traceDebugAdapterOutput,
            bool pauseForDebugger,
            bool redirectVSAssert,
            int responseTimeout,
            IEnumerable<KeyValuePair<string, string>> additionalEnvironmentVariables)
        {
            // If pauseForDebugger is enabled, we might be debugging the adapter for a while.
            ResponseTimeout = pauseForDebugger ? 1000 * 60 * 60 * 24 : responseTimeout;

            _traceDebugAdapterOutput = traceDebugAdapterOutput;

            EventWaitHandle debugAdapterStarted = new EventWaitHandle(false, EventResetMode.AutoReset);
            Responses = new List<string>();

            ProcessStartInfo startInfo = new ProcessStartInfo(adapterPath, adapterArgs);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardOutputEncoding = s_utf8NoBOM;
            startInfo.StandardInputEncoding = s_utf8NoBOM;

            if (redirectVSAssert)
            {
                _assertionFileName = Path.Combine(Path.GetTempPath(), string.Format(CultureInfo.InvariantCulture, "vsassert.{0}.txt", Guid.NewGuid()));
                startInfo.Environment["VSASSERT"] = _assertionFileName;
            }

            if (additionalEnvironmentVariables != null)
            {
                foreach (KeyValuePair<string, string> pair in additionalEnvironmentVariables)
                {
                    startInfo.Environment[pair.Key] = pair.Value;
                }
            }

            DebugAdapter = Process.Start(startInfo);

            DebugAdapter.ErrorDataReceived += (o, a) =>
            {
                if (pauseForDebugger)
                {
                    debugAdapterStarted.Set();
                }

                string message = a.Data ?? string.Empty;
                if (message.StartsWith("ASSERT FAILED", StringComparison.Ordinal))
                {
                    _hasAsserted = true;
                }

                LogErrorLine(message);
            };
            DebugAdapter.BeginErrorReadLine();

            if (pauseForDebugger)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "Attach a debugger to PID {0}", DebugAdapter.Id));
                debugAdapterStarted.WaitOne();
            }
        }

        private void LogErrorLine(string message)
        {
            try
            {
                if (Debugger.IsAttached)
                {
                    Debug.WriteLine(message);
                }

                if (_errorLogger != null)
                {
                    _errorLogger(message);
                }
                else if (_traceDebugAdapterOutput == TraceDebugAdapterMode.Console)
                {
                    Console.WriteLine(message);
                }

                if (_traceDebugAdapterOutput != TraceDebugAdapterMode.None)
                {
                    AppendLineToDebugAdapterOutput(message);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Returns the sequence number for the next message
        /// </summary>
        /// <returns>Sequence number</returns>
        public int GetNextSequenceNumber()
        {
            return Interlocked.Increment(ref _seq);
        }

        public string SerializeMessage(DispatcherMessage message)
        {
            string serialized = JsonConvert.SerializeObject(message);
            byte[] bytes = Encoding.UTF8.GetBytes(serialized);
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}{2}{3}", DAPConstants.ContentLength, bytes.Length, DAPConstants.TwoCrLf, serialized);
        }

        public void Run(Command command)
        {
            try
            {
                command.Run(this);
            }
            catch (DARException darException)
            {
                throw new DARException(string.Format(CultureInfo.InvariantCulture, "Test command execution failed...\nFailure = {0}\n\nCommand = {1}\n\nFull output = {2}",
                    darException.Message, JsonConvert.SerializeObject(command), DebugAdapterOutput));
            }

            if (this.HasAsserted())
            {
                throw new DARException(string.Format(CultureInfo.InvariantCulture, "Debug adapter has asserted wher running command. See test log for details. Command: {0}", JsonConvert.SerializeObject(command)));
            }
        }

        public bool HasAsserted()
        {
            if (_hasAsserted)
                return true;

            if (_assertionFileName != null && File.Exists(_assertionFileName))
            {
                _hasAsserted = true;

                try
                {
                    foreach (var line in File.ReadAllLines(_assertionFileName))
                    {
                        LogErrorLine(line);
                    }
                }
                catch
                {
                }

                return true;
            }

            return false;
        }

        public void AddCallbackRequestHandler(string commandName, CallbackRequestHandler handler)
        {
            this._callbackHandlers.Add(commandName, handler);
        }

        internal void HandleCallbackRequest(string receivedMessage)
        {
            DispatcherRequest dispatcherRequest = JsonConvert.DeserializeObject<DispatcherRequest>(receivedMessage);

            if (_callbackHandlers.TryGetValue(dispatcherRequest.command, out CallbackRequestHandler handler))
            {
                handler(this, dispatcherRequest);
            }
            else
            {
                string errorMessage = String.Format(CultureInfo.CurrentCulture, "Received unhandled callback command '{0}'", dispatcherRequest.command);
                throw new DARException(errorMessage);
            }
        }
    }
}
