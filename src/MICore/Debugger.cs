// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using Microsoft.Win32.SafeHandles;

namespace MICore
{
    public enum ProcessState
    {
        NotConnected,
        Running,
        Stopped,
        Exited
    };

    public class Debugger : ITransportCallback
    {
        public event EventHandler BreakModeEvent;
        public event EventHandler RunModeEvent;
        public event EventHandler ProcessExitEvent;
        public event EventHandler DebuggerExitEvent;
        public event EventHandler<string> DebuggerAbortedEvent;
        public event EventHandler<string> OutputStringEvent;
        public event EventHandler EvaluationEvent;
        public event EventHandler ErrorEvent;
        public event EventHandler ModuleLoadEvent;  // occurs when stopped after a libraryLoadEvent
        public event EventHandler LibraryLoadEvent; // a shared library was loaded
        public event EventHandler BreakChangeEvent; // a breakpoint was changed
        public event EventHandler BreakCreatedEvent; // a breakpoint was created
        public event EventHandler ThreadCreatedEvent;
        public event EventHandler ThreadExitedEvent;
        public event EventHandler ThreadGroupExitedEvent;
        public event EventHandler<ResultEventArgs> MessageEvent;
        public event EventHandler<ResultEventArgs> TelemetryEvent;
        private int _exiting;
        public ProcessState ProcessState { get; private set; }
        private MIResults _miResults;

        public bool EntrypointHit { get; protected set; }

        public bool IsCygwin { get; protected set; }

        public virtual void FlushBreakStateData()
        {
        }

        public bool IsClosed
        {
            get
            {
                return _isClosed;
            }
        }

        public uint MaxInstructionSize { get; private set; }
        public bool Is64BitArch { get; private set; }
        public CommandLock CommandLock { get { return _commandLock; } }
        public MICommandFactory MICommandFactory { get; protected set; }
        public Logger Logger { private set; get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected readonly LaunchOptions _launchOptions;
        public LaunchOptions LaunchOptions { get { return this._launchOptions; } }

        private Queue<Func<Task>> _internalBreakActions = new Queue<Func<Task>>();
        private TaskCompletionSource<object> _internalBreakActionCompletionSource;
        private TaskCompletionSource<object> _consoleDebuggerInitializeCompletionSource = new TaskCompletionSource<object>();
        private LinkedList<string> _initializationLog = new LinkedList<string>();
        private LinkedList<string> _initialErrors = new LinkedList<string>();
        private int _localDebuggerPid = -1;

        protected bool _connected;
        protected bool _terminating;

        public class ResultEventArgs : EventArgs
        {
            public ResultEventArgs(Results results, uint id)
            {
                Results = results;
                Id = id;
            }
            public ResultEventArgs(Results results)
            {
                Results = results;
            }
            public Results Results { get; private set; }
            public ResultClass ResultClass { get { return Results.ResultClass; } }
            public uint Id { get; private set; }
        };

        public class StoppingEventArgs : ResultEventArgs
        {
            public readonly BreakRequest AsyncRequest;
            public StoppingEventArgs(Results results, uint id, BreakRequest asyncRequest = BreakRequest.None) : base(results, id)
            {
                AsyncRequest = asyncRequest;
            }

            public StoppingEventArgs(Results results, BreakRequest asyncRequest = BreakRequest.None) : this(results, 0, asyncRequest)
            { }
        }

        private ITransport _transport;
        private CommandLock _commandLock = new CommandLock();

        /// <summary>
        /// The last command we sent over the transport. This includes both the command name and arguments.
        /// </summary>
        private string _lastCommandText;
        private uint _lastCommandId;
        private bool _isClosed;

        /// <summary>
        /// [Optional] If a console command is being executed, list where we append the output
        /// </summary>
        private StringBuilder _consoleCommandOutput;

        private bool _pendingInternalBreak;
        internal bool IsRequestingInternalAsyncBreak
        {
            get
            {
                return _pendingInternalBreak;
            }
        }

        private bool _waitingToStop;
        private Timer _breakTimer = null;
        private int _retryCount;
        private const int BREAK_DELTA = 3000;   // millisec before trying to break again
        private const int BREAK_RETRY_MAX = 3;  // maximum times to retry

        // The key is the thread group, the value is the pid
        private Dictionary<string, int> _debuggeePids;

        public Debugger(LaunchOptions launchOptions, Logger logger)
        {
            _launchOptions = launchOptions;
            _debuggeePids = new Dictionary<string, int>();
            Logger = logger;
            _miResults = new MIResults(logger);
        }

        protected void SetDebuggerPid(int debuggerPid)
        {
            // Used for testing
            Logger.WriteLine(string.Concat("DebuggerPid=", debuggerPid));
            _localDebuggerPid = debuggerPid;
        }

        /// <summary>
        /// Check if the local debugger process is running.
        /// For Windows, it returns False always to avoid shortcuts taken when it returns True.
        /// </summary>
        /// <returns>True if the local debugger process is running and the platform is Linux or OS X.
        /// False otherwise.</returns>
        private bool IsUnixDebuggerRunning()
        {
            if (_localDebuggerPid > 0)
            {
                if (PlatformUtilities.IsLinux() || PlatformUtilities.IsOSX())
                {
                    return UnixUtilities.IsProcessRunning(_localDebuggerPid);
                }
            }

            return false;
        }

        private void RetryBreak(object o)
        {
            lock (_internalBreakActions)
            {
                if (_waitingToStop && _retryCount < BREAK_RETRY_MAX)
                {
                    Logger.WriteLine("Debugger failed to break. Trying again.");
                    CmdBreak(BreakRequest.Internal);
                    _retryCount++;
                }
                else
                {
                    if (_breakTimer != null)
                    {
                        _breakTimer.Dispose();
                        _breakTimer = null;
                    }
                }
            }
        }

        public void EnqueueInternalBreakAction(Func<Task> func)
        {
            lock (_internalBreakActions)
            {
                if (_internalBreakActionCompletionSource == null)
                {
                    _internalBreakActionCompletionSource = new TaskCompletionSource<object>();
                }
                _internalBreakActions.Enqueue(func);
            }
        }

        public Task AddInternalBreakAction(Func<Task> func)
        {
            if (this.ProcessState == ProcessState.Stopped || !_connected || this.MICommandFactory.AllowCommandsWhileRunning())
            {
                return func();
            }
            else
            {
                lock (_internalBreakActions)
                {
                    if (_internalBreakActionCompletionSource == null)
                    {
                        _internalBreakActionCompletionSource = new TaskCompletionSource<object>();
                    }
                    _internalBreakActions.Enqueue(func);

                    if (!_pendingInternalBreak)
                    {
                        _pendingInternalBreak = true;
                        CmdBreak(BreakRequest.Internal);
                        _retryCount = 0;
                        _waitingToStop = true;

                        // When using signals to stop the proces, do not kick off another break attempt. The debug break injection and
                        // signal based models are reliable so no retries are needed. Cygwin can't currently async-break reliably, so
                        // use retries there.
                        if (!IsLocalGdb() && !this.IsCygwin)
                        {
                            _breakTimer = new Timer(RetryBreak, null, BREAK_DELTA, BREAK_DELTA);
                        }
                    }
                    return _internalBreakActionCompletionSource.Task;
                }
            }
        }

        private async void OnStopped(Results results)
        {
            string reason = results.TryFindString("reason");

            if (reason.StartsWith("exited") || reason.StartsWith("disconnected"))
            {
                if (this.ProcessState != ProcessState.Exited)
                {
                    this.ProcessState = ProcessState.Exited;
                    if (ProcessExitEvent != null)
                    {
                        ProcessExitEvent(this, new ResultEventArgs(results));
                    }
                }
                return;
            }

            //if this is an exception reported from LLDB, it will not currently contain a frame object in the MI
            //if we don't have a frame, check if this is an exception and retrieve the frame
            if (!results.Contains("frame") &&
                (string.Compare(reason, "exception-received", StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(reason, "signal-received", StringComparison.OrdinalIgnoreCase) == 0)
                )
            {
                //get the info for the current frame
                Results frameResult = await MICommandFactory.StackInfoFrame();

                //add the frame to the stopping results
                results = results.Add("frame", frameResult.Find("frame"));
            }

            bool fIsAsyncBreak = MICommandFactory.IsAsyncBreakSignal(results);
            if (await DoInternalBreakActions(fIsAsyncBreak))
            {
                return;
            }

            this.ProcessState = ProcessState.Stopped;
            FlushBreakStateData();

            if (!results.Contains("frame"))
            {
                if (ModuleLoadEvent != null)
                {
                    ModuleLoadEvent(this, new ResultEventArgs(results));
                }
            }
            else if (BreakModeEvent != null)
            {
                BreakRequest request = _requestingRealAsyncBreak;
                _requestingRealAsyncBreak = BreakRequest.None;
                BreakModeEvent(this, new StoppingEventArgs(results, request));
            }
        }

        protected virtual void OnStateChanged(string mode, string strresult)
        {
            this.OnStateChanged(mode, _miResults.ParseResultList(strresult));
        }

        protected void OnStateChanged(string mode, Results results)
        {
            if (mode == "stopped")
            {
                OnStopped(results);
            }
            else if (mode == "running")
            {
                this.ProcessState = ProcessState.Running;
                if (RunModeEvent != null)
                {
                    RunModeEvent(this, new ResultEventArgs(results));
                }
            }
            else if (mode == "exit")
            {
                OnDebuggerProcessExit(null);
            }
            else if (mode.StartsWith("done,bkpt=", StringComparison.Ordinal))
            {
                // TODO handle breakpoint binding
            }
            else if (mode == "done")
            {
            }
            else if (mode == "connected")
            {
                if (this.ProcessState == ProcessState.NotConnected)
                    this.ProcessState = ProcessState.Running;

                if (RunModeEvent != null)
                {
                    RunModeEvent(this, new ResultEventArgs(results));
                }
            }
            else
            {
                Debug.Fail("Unknown mode: " + mode);
            }

            return;
        }

        /// <summary>
        /// Handles executing internal break actions
        /// </summary>
        /// <param name="fIsAsyncBreak">Is the stopping action coming from an async break</param>
        /// <returns>Returns true if the process is continued and we should not enter break state, returns false if the process is stopped and we should enter break state.</returns>
        private async Task<bool> DoInternalBreakActions(bool fIsAsyncBreak)
        {
            TaskCompletionSource<object> source = null;
            Func<Task> item = null;
            Exception firstException = null;
            while (true)
            {
                lock (_internalBreakActions)
                {
                    _waitingToStop = false;
                    if (_internalBreakActions.Count == 0)
                    {
                        _pendingInternalBreak = false;
                        _internalBreakActionCompletionSource = null;
                        break;
                    }
                    source = _internalBreakActionCompletionSource;
                    item = _internalBreakActions.Dequeue();
                }

                try
                {
                    await item();
                }
                catch (Exception e) when (ExceptionHelper.BeforeCatch(e, Logger, reportOnlyCorrupting: true))
                {
                    if (firstException != null)
                    {
                        firstException = e;
                    }
                }
            }

            bool processContinued = false;
            if (source != null)
            {
                if (_isClosed)
                {
                    source.SetException(new ObjectDisposedException("Debugger"));
                }
                else
                {
                    if (_requestingRealAsyncBreak == BreakRequest.Internal && fIsAsyncBreak)
                    {
                        CmdContinueAsync();
                        processContinued = true;
                    }

                    if (firstException != null)
                    {
                        source.SetException(firstException);
                    }
                    else
                    {
                        source.SetResult(null);
                    }
                }
            }

            return processContinued;
        }

        public void Init(ITransport transport, LaunchOptions options)
        {
            _lastCommandId = 1000;
            _transport = transport;
            FlushBreakStateData();

            _transport.Init(this, options, Logger);
        }

        public void SetTargetArch(TargetArchitecture arch)
        {
            switch (arch)
            {
                case TargetArchitecture.ARM:
                    MaxInstructionSize = 4;
                    Is64BitArch = false;
                    break;

                case TargetArchitecture.ARM64:
                    MaxInstructionSize = 8;
                    Is64BitArch = true;
                    break;

                case TargetArchitecture.X86:
                    MaxInstructionSize = 20;
                    Is64BitArch = false;
                    break;

                case TargetArchitecture.X64:
                    MaxInstructionSize = 26;
                    Is64BitArch = true;
                    break;

                case TargetArchitecture.Mips:
                    MaxInstructionSize = 4;
                    Is64BitArch = false;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("options.TargetArchitecture");
            }
        }

        public async Task WaitForConsoleDebuggerInitialize(CancellationToken token)
        {
            if (_consoleDebuggerInitializeCompletionSource == null)
            {
                Debug.Fail("Why is WaitForConsoleDebuggerInitialize called more than once? Not allowed.");
                throw new InvalidOperationException();
            }

            using (token.Register(() => { _consoleDebuggerInitializeCompletionSource.TrySetException(new OperationCanceledException()); }))
            {
                await _consoleDebuggerInitializeCompletionSource.Task;
            }

            lock (_waitingOperations)
            {
                _consoleDebuggerInitializeCompletionSource = null;

                // We no longer care about keeping these, so empty them out
                _initializationLog = null;
                _initialErrors = null;
            }
        }

        protected void CloseQuietly()
        {
            if (Interlocked.CompareExchange(ref _exiting, 1, 0) == 0)
            {
                Close();
            }
        }

        private void Close()
        {
            _isClosed = true;
            _transport.Close();
            lock (_waitingOperations)
            {
                foreach (var value in _waitingOperations.Values)
                {
                    value.Abort();
                }
                _waitingOperations.Clear();
            }
            lock (_internalBreakActions)
            {
                if (_internalBreakActionCompletionSource != null)
                {
                    _internalBreakActionCompletionSource.SetException(new ObjectDisposedException("Debugger"));
                }
                _internalBreakActions.Clear();
            }
        }

        public Task CmdStopAtMain()
        {
            this.VerifyNotDebuggingCoreDump();

            return CmdAsync("-break-insert main", ResultClass.done);
        }

        public Task CmdStart()
        {
            this.VerifyNotDebuggingCoreDump();

            return CmdAsync("-exec-run", ResultClass.running);
        }

        internal bool IsRequestingRealAsyncBreak
        {
            get
            {
                return _requestingRealAsyncBreak != BreakRequest.None;
            }
        }

        public enum BreakRequest    // order is important so a stop request doesn't get overridden by an internal request
        {
            None,
            Internal,
            Async,
            Stop
        }
        protected BreakRequest _requestingRealAsyncBreak = BreakRequest.None;
        public Task CmdBreak(BreakRequest request)
        {
            if (request > _requestingRealAsyncBreak)
            {
                _requestingRealAsyncBreak = request;
            }
            return CmdBreakInternal();
        }


        internal bool IsLocalGdb()
        {
            if (this.MICommandFactory.Mode == MIMode.Gdb &&
               this._launchOptions is LocalLaunchOptions &&
               String.IsNullOrEmpty(((LocalLaunchOptions)this._launchOptions).MIDebuggerServerAddress)
               )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsRemoteGdb()
        {
            return this.MICommandFactory.Mode == MIMode.Gdb &&
               this._launchOptions is PipeLaunchOptions;
        }

        protected bool IsCoreDump
        {
            get
            {
                LocalLaunchOptions localOptions = this._launchOptions as LocalLaunchOptions;
                if (null == localOptions)
                    return false;

                return localOptions.IsCoreDump;
            }
        }

        public async Task<Results> CmdTerminate()
        {
            if (!_terminating)
            {
                _terminating = true;
                if (ProcessState == ProcessState.Running && this.MICommandFactory.Mode != MIMode.Clrdbg)
                {
                    await CmdBreak(BreakRequest.Async);
                }

                await MICommandFactory.Terminate();
            }

            return new Results(ResultClass.done);
        }

        public async Task<Results> CmdDetach()
        {
            if (ProcessState == ProcessState.Running)
            {
                await CmdBreak(BreakRequest.Async);
            }
            await CmdAsync("-target-detach", ResultClass.done);

            return new Results(ResultClass.done);
        }

        public Task CmdBreakInternal()
        {
            this.VerifyNotDebuggingCoreDump();
            Debug.Assert(_requestingRealAsyncBreak != BreakRequest.None);

            // Note that interrupt doesn't work on OS X with gdb:
            // https://sourceware.org/bugzilla/show_bug.cgi?id=20035
            if (IsLocalGdb())
            {
                bool useSignal = false;
                int debuggeePid = 0;
                lock (_debuggeePids)
                {
                    if (_debuggeePids.Count > 0)
                    {
                        debuggeePid = _debuggeePids.First().Value;
                        useSignal = true;
                    }
                }

                if (PlatformUtilities.IsLinux() || PlatformUtilities.IsOSX())
                {
                    // for local linux debugging, send a signal to one of the debuggee processes rather than
                    // using -exec-interrupt. -exec-interrupt does not work with attach and, in some instances, launch. 
                    // End result is either deadlocks or missed bps (since binding in runtime requires break state).
                    // NOTE: this is not required for remote. Remote will not be using LocalLinuxTransport
                    if (useSignal)
                    {
                        return CmdBreakUnix(debuggeePid, ResultClass.done);
                    }
                }
            }
            else if (IsRemoteGdb() && _transport is PipeTransport)
            {
                int pid = PidByInferior("i1");
                if (pid != 0 && ((PipeTransport)_transport).Interrupt(pid))
                {
                    return Task.FromResult<Results>(new Results(ResultClass.done));
                }
            }

            var res = CmdAsync("-exec-interrupt", ResultClass.done);
            return res.ContinueWith((t) =>
            {
                if (t.Result.Contains("reason"))    // interrupt finished synchronously
                {
                    ScheduleResultProcessing(() => OnStopped(t.Result));
                }
            });
        }

        public void CmdContinueAsync()
        {
            this.VerifyNotDebuggingCoreDump();
            PostCommand("-exec-continue");
        }

        public void CmdExitAsync()
        {
            // 'gdb' required for legacy
            PostCommand("-gdb-exit");
        }

        private string Escape(string str)
        {
            StringBuilder outStr = new StringBuilder();
            for (int i = 0; i < str.Length; ++i)
            {
                switch (str[i])
                {
                    case '\"':
                        outStr.Append("\\\"");
                        break;
                    case '\\':
                        outStr.Append("\\\\");
                        break;
                    default:
                        outStr.Append(str[i]);
                        break;
                }
            }
            return outStr.ToString();
        }

        public async Task<string> ConsoleCmdAsync(string cmd, bool ignoreFailures = false)
        {
            if (this.ProcessState != ProcessState.Stopped && this.ProcessState != ProcessState.NotConnected)
            {
                if (this.ProcessState == MICore.ProcessState.Exited)
                {
                    throw new ObjectDisposedException("Debugger");
                }
                else
                {
                    throw new InvalidOperationException(MICoreResources.Error_ProcessMustBeStopped);
                }
            }

            using (ExclusiveLockToken lockToken = await _commandLock.AquireExclusive())
            {
                // check again now that we have the lock
                if (this.ProcessState != MICore.ProcessState.Stopped && this.ProcessState != ProcessState.NotConnected)
                {
                    if (this.ProcessState == MICore.ProcessState.Exited)
                    {
                        throw new ObjectDisposedException("Debugger");
                    }
                    else
                    {
                        throw new InvalidOperationException(MICoreResources.Error_ProcessMustBeStopped);
                    }
                }

                Debug.Assert(_consoleCommandOutput == null, "How is m_consoleCommandOutput already set? Should be impossible.");
                _consoleCommandOutput = new StringBuilder();

                try
                {
                    await ExclusiveCmdAsync("-interpreter-exec console \"" + Escape(cmd) + "\"", ignoreFailures ? ResultClass.None : ResultClass.done, lockToken);

                    return _consoleCommandOutput.ToString();
                }
                finally
                {
                    _consoleCommandOutput = null;
                }
            }
        }

        public async Task<Results> CmdAsync(string command, ResultClass expectedResultClass)
        {
            await _commandLock.AquireShared();

            try
            {
                return await CmdAsyncInternal(command, expectedResultClass);
            }
            finally
            {
                _commandLock.ReleaseShared();
            }
        }

        public Task<Results> ExclusiveCmdAsync(string command, ResultClass expectedResultClass, ExclusiveLockToken exclusiveLockToken)
        {
            if (ExclusiveLockToken.IsNullOrClosed(exclusiveLockToken))
            {
                throw new ArgumentNullException("exclusiveLockToken");
            }

            return CmdAsyncInternal(command, expectedResultClass);
        }

        private Task<Results> CmdAsyncInternal(string command, ResultClass expectedResultClass)
        {
            var waitingOperation = new WaitingOperationDescriptor(command, expectedResultClass);
            uint id;

            lock (_waitingOperations)
            {
                if (_isClosed)
                {
                    throw new ObjectDisposedException("Debugger");
                }

                id = ++_lastCommandId;
                _waitingOperations.Add(id, waitingOperation);
                _lastCommandText = command;
            }

            SendToTransport(id.ToString(CultureInfo.InvariantCulture) + command);

            return waitingOperation.Task;
        }

        private Task<Results> CmdBreakUnix(int debugeePid, ResultClass expectedResultClass)
        {
            // Send sigint to the debuggee process. This is the equivalent of hitting ctrl-c on the console.
            // This will cause gdb to async-break. This is necessary because gdb does not support async break
            // when attached.
            const int sigint = 2;
            UnixNativeMethods.Kill(debugeePid, sigint);

            return Task.FromResult<Results>(new Results(ResultClass.done));
        }

        #region ITransportCallback implementation
        // Note: this can be called from any thread
        void ITransportCallback.OnStdOutLine(string line)
        {
            if (_initializationLog != null)
            {
                lock (_waitingOperations)
                {
                    // check again now that the lock is aquired
                    if (_initializationLog != null)
                    {
                        _initializationLog.AddLast(line);
                    }
                }
            }

            ScheduleStdOutProcessing(line);
        }

        void ITransportCallback.OnStdErrorLine(string line)
        {
            Logger.WriteLine("STDERR: " + line);

            if (_initialErrors != null)
            {
                lock (_waitingOperations)
                {
                    if (_initialErrors != null)
                    {
                        _initialErrors.AddLast(line);
                    }

                    if (_initializationLog != null)
                    {
                        _initializationLog.AddLast(line);
                    }
                }
            }
        }

        public void OnDebuggerProcessExit(/*OPTIONAL*/ string exitCode)
        {
            // GDB has exited. Cleanup. Only let one thread perform the cleanup
            if (Interlocked.CompareExchange(ref _exiting, 1, 0) == 0)
            {
                if (_consoleDebuggerInitializeCompletionSource != null)
                {
                    lock (_waitingOperations)
                    {
                        if (_consoleDebuggerInitializeCompletionSource != null)
                        {
                            MIDebuggerInitializeFailedException exception = new MIDebuggerInitializeFailedException(this.MICommandFactory.Name, _initialErrors.ToList().AsReadOnly(), _initializationLog.ToList().AsReadOnly());
                            _initialErrors = null;
                            _initializationLog = null;

                            _consoleDebuggerInitializeCompletionSource.TrySetException(exception);
                        }
                    }
                }

                Close();
                if (this.ProcessState != ProcessState.Exited)
                {
                    if (DebuggerAbortedEvent != null)
                    {
                        DebuggerAbortedEvent(this, exitCode);
                    }
                }
                else
                {
                    if (DebuggerExitEvent != null)
                    {
                        DebuggerExitEvent(this, null);
                    }
                }
            }
        }

        void ITransportCallback.AppendToInitializationLog(string line)
        {
            Logger.WriteLine(line);

            if (_initializationLog != null)
            {
                lock (_waitingOperations)
                {
                    // check again now that the lock is aquired
                    if (_initializationLog != null)
                    {
                        _initializationLog.AddLast(line);
                    }
                }
            }
        }

        void ITransportCallback.LogText(string line)
        {
            if (!line.EndsWith("\n", StringComparison.Ordinal))
            {
                line += "\n";
            }
            if (OutputStringEvent != null)
            {
                OutputStringEvent(this, line);
            }
        }

        #endregion

        // inherited classes can override this for thread marshalling etc
        protected virtual void ScheduleStdOutProcessing(string line)
        {
            ProcessStdOutLine(line);
        }

        protected virtual void ScheduleResultProcessing(Action func)
        {
            func();
        }

        // a Token is a sequence of decimal digits followed by something else
        // returns null if not a token, or not followed by something else
        private string ParseToken(ref string cmd)
        {
            if (char.IsDigit(cmd, 0))
            {
                int i;
                for (i = 1; i < cmd.Length; i++)
                {
                    if (!char.IsDigit(cmd, i))
                        break;
                }
                if (i < cmd.Length)
                {
                    string token = cmd.Substring(0, i);
                    cmd = cmd.Substring(i);
                    return token;
                }
            }
            return null;
        }

        private class WaitingOperationDescriptor
        {
            /// <summary>
            /// Text of the command that we sent to the debugger (ex: '-target-attach 72')
            /// </summary>
            public readonly string Command;
            private readonly ResultClass _expectedResultClass;
            private readonly TaskCompletionSource<Results> _completionSource = new TaskCompletionSource<Results>();
            public DateTime StartTime { get; private set; }

            /// <summary>
            /// True if the transport has echoed back text which is the same as this command
            /// </summary>
            public bool EchoReceived { get; set; }

            public WaitingOperationDescriptor(string command, ResultClass expectedResultClass)
            {
                this.Command = command;
                _expectedResultClass = expectedResultClass;
                StartTime = DateTime.Now;
            }

            internal void OnComplete(Results results, MICommandFactory commandFactory)
            {
                if (_expectedResultClass != ResultClass.None && _expectedResultClass != results.ResultClass)
                {
                    string miError = null;
                    if (results.ResultClass == ResultClass.error)
                    {
                        miError = results.FindString("msg");
                    }

                    _completionSource.SetException(new UnexpectedMIResultException(commandFactory.Name, this.Command, miError));
                }
                else
                {
                    _completionSource.SetResult(results);
                }
            }

            public Task<Results> Task { get { return _completionSource.Task; } }

            internal void Abort()
            {
                _completionSource.SetException(new ObjectDisposedException("Debugger"));
            }
        }

        private readonly Dictionary<uint, WaitingOperationDescriptor> _waitingOperations = new Dictionary<uint, WaitingOperationDescriptor>();

        public void ProcessStdOutLine(string line)
        {
            if (line.Length == 0)
            {
                return;
            }
            else if (line == "(gdb)")
            {
                if (_consoleDebuggerInitializeCompletionSource != null)
                {
                    lock (_waitingOperations)
                    {
                        if (_consoleDebuggerInitializeCompletionSource != null)
                        {
                            _consoleDebuggerInitializeCompletionSource.TrySetResult(null);
                        }
                    }
                }
            }
            else
            {
                string token = ParseToken(ref line);
                char c = line[0];
                string noprefix = line.Substring(1).Trim();

                if (token != null)
                {
                    // Look for event handlers registered on a specific Result id
                    if (c == '^')
                    {
                        uint id = uint.Parse(token, CultureInfo.InvariantCulture);
                        WaitingOperationDescriptor waitingOperation = null;
                        lock (_waitingOperations)
                        {
                            if (_waitingOperations.TryGetValue(id, out waitingOperation))
                            {
                                _waitingOperations.Remove(id);
                            }
                        }
                        if (waitingOperation != null)
                        {
                            Results results = _miResults.ParseCommandOutput(noprefix);
                            Logger.WriteLine(id + ": elapsed time " + (int)(DateTime.Now - waitingOperation.StartTime).TotalMilliseconds);
                            waitingOperation.OnComplete(results, this.MICommandFactory);
                            return;
                        }
                    }
                    // Check to see if we are just getting the echo of the command we sent
                    else if (c == '-')
                    {
                        uint id = uint.Parse(token, CultureInfo.InvariantCulture);
                        lock (_waitingOperations)
                        {
                            WaitingOperationDescriptor waitingOperation;
                            if (_waitingOperations.TryGetValue(id, out waitingOperation) &&
                                !waitingOperation.EchoReceived &&
                                line == waitingOperation.Command)
                            {
                                // This is just the echo. Ignore.
                                waitingOperation.EchoReceived = true;
                                return;
                            }
                        }
                    }
                }

                switch (c)
                {
                    case '~':
                        OnDebuggeeOutput(noprefix);         // Console stream
                        break;
                    case '^':
                        OnResult(noprefix, token);
                        break;
                    case '*':
                        OnOutOfBand(noprefix);
                        break;
                    case '&':
                        OnLogStreamOutput(noprefix);
                        break;
                    case '=':
                        OnNotificationOutput(noprefix);
                        break;
                    default:
                        OnDebuggeeOutput(line + '\n');
                        break;
                }
            }
        }

        private void OnUnknown(string cmd)
        {
            Debug.WriteLine("DBG:Unknown command: {0}", cmd);
        }

        private void OnResult(string cmd, string token)
        {
            uint id = token != null ? uint.Parse(token, CultureInfo.InvariantCulture) : 0;
            Results results = _miResults.ParseCommandOutput(cmd);

            if (results.ResultClass == ResultClass.done)
            {
                if (EvaluationEvent != null)
                {
                    EvaluationEvent(this, new ResultEventArgs(results, id));
                }
            }
            else if (results.ResultClass == ResultClass.error)
            {
                if (ErrorEvent != null)
                {
                    ErrorEvent(this, new ResultEventArgs(results, id));
                }
            }
            else
            {
                OnStateChanged(cmd, "");
            }
        }

        private void OnDebuggeeOutput(string cmd)
        {
            string decodedOutput = _miResults.ParseCString(cmd);

            if (_consoleCommandOutput == null)
            {
                if (OutputStringEvent != null)
                {
                    OutputStringEvent(this, decodedOutput);
                }
            }
            else
            {
                _consoleCommandOutput.Append(decodedOutput);
            }
        }

        public void WriteOutput(string message)
        {
            if (OutputStringEvent != null)
            {
                OutputStringEvent(this, message + '\n');
            }
        }

        private void OnLogStreamOutput(string cmd)
        {
            if (_consoleCommandOutput == null)
            {
                // We see this in the transport diagnostics, we don't need to see it anywhere else
            }
            else
            {
                string decodedOutput = _miResults.ParseCString(cmd);
                _consoleCommandOutput.Append(decodedOutput);
            }
        }

        private void OnOutOfBand(string cmd)
        {
            if (cmd.StartsWith("stopped,", StringComparison.Ordinal))
            {
                string status = _miResults.ParseCString(cmd.Substring(8));
                OnStateChanged("stopped", status);
            }
            else if (cmd.StartsWith("stopped", StringComparison.Ordinal))
            {
                if (PlatformUtilities.IsWindows() &&
                    this.LaunchOptions is LocalLaunchOptions &&
                    ((LocalLaunchOptions)this.LaunchOptions).ProcessId != 0 &&
                    this.MICommandFactory.Mode == MIMode.Gdb &&
                    !this.IsCygwin
                    )
                {
                    // mingw enters break mode with no status flags on the mi response during attach.
                    // In order to keey the entrypoint state correct, set it to true and continue
                    // the break.
                    this.EntrypointHit = true;
                    CmdContinueAsync();
                }
                else
                {
                    Debug.Fail("Unknown out-of-band msg: " + cmd);
                }
            }
            else if (cmd.StartsWith("running,", StringComparison.Ordinal))
            {
                string status = _miResults.ParseCString(cmd.Substring(8));
                OnStateChanged("running", status);
            }
            else
            {
                Debug.Fail("Unknown out-of-band msg: " + cmd);
            }
        }

        private void OnNotificationOutput(string cmd)
        {
            Results results = null;
            if ((results = MICommandFactory.IsModuleLoad(cmd)) != null)
            {
                if (LibraryLoadEvent != null)
                {
                    LibraryLoadEvent(this, new ResultEventArgs(results));
                }
            }
            else if (cmd.StartsWith("breakpoint-modified,", StringComparison.Ordinal))
            {
                results = _miResults.ParseResultList(cmd.Substring(20));
                if (BreakChangeEvent != null)
                {
                    BreakChangeEvent(this, new ResultEventArgs(results));
                }
            }
            else if (cmd.StartsWith("breakpoint-created,", StringComparison.Ordinal))
            {
                results = _miResults.ParseResultList(cmd.Substring("breakpoint-created,".Length));
                if (BreakCreatedEvent != null)
                {
                    BreakCreatedEvent(this, new ResultEventArgs(results));
                }
            }
            else if (cmd.StartsWith("thread-group-started,", StringComparison.Ordinal))
            {
                results = _miResults.ParseResultList(cmd.Substring("thread-group-started,".Length));
                HandleThreadGroupStarted(results);
            }
            else if (cmd.StartsWith("thread-group-exited,", StringComparison.Ordinal))
            {
                results = _miResults.ParseResultList(cmd.Substring("thread-group-exited,".Length));
                HandleThreadGroupExited(results);
                if (ThreadGroupExitedEvent != null)
                {
                    ThreadGroupExitedEvent(this, new ResultEventArgs(results, 0));
                }
            }
            else if (cmd.StartsWith("thread-created,", StringComparison.Ordinal))
            {
                results = _miResults.ParseResultList(cmd.Substring("thread-created,".Length));
                ThreadCreatedEvent(this, new ResultEventArgs(results, 0));
            }
            else if (cmd.StartsWith("thread-exited,", StringComparison.Ordinal))
            {
                results = _miResults.ParseResultList(cmd.Substring("thread-exited,".Length));
                ThreadExitedEvent(this, new ResultEventArgs(results, 0));
            }
            // NOTE: the message event is an MI Extension from clrdbg, though we could use in it the future for other debuggers
            else if (cmd.StartsWith("message,", StringComparison.Ordinal))
            {
                results = _miResults.ParseResultList(cmd.Substring("message,".Length));
                if (this.MessageEvent != null)
                {
                    this.MessageEvent(this, new ResultEventArgs(results));
                }
            }
            else if (cmd.StartsWith("telemetry,", StringComparison.Ordinal))
            {
                results = _miResults.ParseResultList(cmd.Substring("telemetry,".Length));
                if (this.TelemetryEvent != null)
                {
                    this.TelemetryEvent(this, new ResultEventArgs(results));
                }
            }
            else
            {
                // append a newline if the message didn't come with one
                if (!cmd.EndsWith("\n", StringComparison.Ordinal))
                {
                    cmd += "\n";
                }
                OnDebuggeeOutput("=" + cmd);
            }
        }

        /// <summary>
        /// Obtains the last command (ex: '-exec-break') that we sent to the debugger. This is used in telemetry, and probably shouldn't
        /// be used for any other reason.
        /// </summary>
        /// <returns>The empty string if we haven't sent any commands yet. Otherwise the text of the command</returns>
        public string GetLastSentCommandName()
        {
            string lastCommandText = _lastCommandText;
            if (string.IsNullOrEmpty(lastCommandText))
            {
                // We haven't sent any commands yet
                return string.Empty;
            }

            int spaceIndex = lastCommandText.IndexOf(' ');
            if (spaceIndex >= 0)
            {
                // The last command had arguments. Remove them.
                return lastCommandText.Substring(0, spaceIndex);
            }
            else
            {
                // The last command took no arguments.
                return lastCommandText;
            }
        }

        private void HandleThreadGroupStarted(Results results)
        {
            string threadGroupId = results.FindString("id");
            string pidString = results.FindString("pid");

            int pid = Int32.Parse(pidString, CultureInfo.InvariantCulture);

            // Ignore pid 0 due to spurious thread group created event on iOS (lldb).
            // On android the scheduler runs as pid 0, but that process cannot currently be debugged anyway.
            if (pid != 0)
            {
                lock (_debuggeePids)
                {
                    _debuggeePids.Add(threadGroupId, pid);
                }
            }
        }

        public uint InferiorByPid(int pid)
        {
            foreach (var grp in _debuggeePids)
            {
                if (grp.Value == pid)
                {
                    return InferiorNumber(grp.Key);
                }
            }
            return 0;
        }

        public int PidByInferior(string inf)
        {
            if (_debuggeePids.ContainsKey(inf))
            {
                return _debuggeePids[inf];
            }
            return 0;
        }

        public uint InferiorNumber(string groupId)
        {
            // Inferior names are of the form "iX" where X in the inferior number
            if (groupId.Length >= 2 && groupId[0] == 'i')
            {
                uint id;
                if (UInt32.TryParse(groupId.Substring(1), out id))
                {
                    return id;
                }
            }
            return 1;   // default to the first inferior if group-id not understood
        }

        private void HandleThreadGroupExited(Results results)
        {
            string threadGroupId = results.TryFindString("id");
            bool isThreadGroupEmpty = false;

            if (!String.IsNullOrEmpty(threadGroupId))
            {
                lock (_debuggeePids)
                {
                    if (_debuggeePids.Remove(threadGroupId))
                    {
                        isThreadGroupEmpty = _debuggeePids.Count == 0;
                    }
                }
            }

            if (isThreadGroupEmpty)
            {
                ScheduleStdOutProcessing(@"*stopped,reason=""exited""");

                if (!IsUnixDebuggerRunning())
                {
                    // Processing the fake "stopped" event sent above will normally cause the debugger to close, but if
                    // the debugger process is already gone (e.g. because the terminal window was closed), we won't get
                    // a response, so queue a fake "exit" event for processing as well.
                    ScheduleStdOutProcessing("^exit");
                }
            }
        }

        private async void PostCommand(string cmd)
        {
            try
            {
                await _commandLock.AquireShared();
                try
                {
                    _lastCommandText = cmd;
                    SendToTransport(cmd);
                }
                finally
                {
                    _commandLock.ReleaseShared();
                }
            }
            catch (ObjectDisposedException)
            {
                // This method has 'post' semantics, so if debugging is already stopped, we don't want to throw
            }
        }

        private void SendToTransport(string cmd)
        {
            _transport.Send(cmd);
        }

        public static ulong ParseAddr(string addr, bool throwOnError = false)
        {
            ulong res = 0;
            if (string.IsNullOrEmpty(addr))
            {
                if (throwOnError)
                {
                    throw new ArgumentNullException();
                }
                return 0;
            }
            else if (addr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (throwOnError)
                {
                    res = ulong.Parse(addr.Substring(2), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
                else
                {
                    ulong.TryParse(addr.Substring(2), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out res);
                }
            }
            else
            {
                if (throwOnError)
                {
                    res = ulong.Parse(addr, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture);
                }
                else
                {
                    ulong.TryParse(addr, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out res);
                }
            }
            return res;
        }

        public static uint ParseUint(string str, bool throwOnError = false)
        {
            uint value = 0;
            if (string.IsNullOrEmpty(str))
            {
                if (throwOnError)
                {
                    throw new ArgumentException();
                }
                return value;
            }
            else if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (throwOnError)
                {
                    value = uint.Parse(str.Substring(2), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
                else
                {
                    uint.TryParse(str.Substring(2), System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
                }
            }
            else
            {
                if (throwOnError)
                {
                    value = uint.Parse(str, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture);
                }
                else
                {
                    uint.TryParse(str, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
                }
            }
            return value;
        }

        public void VerifyNotDebuggingCoreDump()
        {
            if (this.IsCoreDump)
                throw new InvalidCoreDumpOperationException();
        }
    }
}

