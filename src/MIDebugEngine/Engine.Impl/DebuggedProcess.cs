// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using System.Threading;
using System.Collections.ObjectModel;
using MICore;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.DebugEngineHost;

using Logger = MICore.Logger;

namespace Microsoft.MIDebugEngine
{
    internal class DebuggedProcess : MICore.Debugger
    {
        public AD_PROCESS_ID Id { get; private set; }
        public AD7Engine Engine { get; private set; }
        public List<string> VariablesToDelete { get; private set; }
        public List<IVariableInformation> ActiveVariables { get; private set; }

        public SourceLineCache SourceLineCache { get; private set; }
        public ThreadCache ThreadCache { get; private set; }
        public Disassembly Disassembly { get; private set; }
        public ExceptionManager ExceptionManager { get; private set; }

        private List<DebuggedModule> _moduleList;
        private ISampleEngineCallback _callback;
        private bool _bLastModuleLoadFailed;
        private StringBuilder _pendingMessages;
        private WorkerThread _worker;
        private BreakpointManager _breakpointManager;
        private bool _bEntrypointHit;
        private ResultEventArgs _initialBreakArgs;
        private List<string> _libraryLoaded;   // unprocessed library loaded messages
        private uint _loadOrder;
        private HostWaitDialog _waitDialog;
        public readonly Natvis.Natvis Natvis;
        private ReadOnlyCollection<RegisterDescription> _registers;
        private ReadOnlyCollection<RegisterGroup> _registerGroups;
        private readonly EngineTelemetry _engineTelemetry = new EngineTelemetry();
        private bool _needTerminalReset;

        public DebuggedProcess(bool bLaunched, LaunchOptions launchOptions, ISampleEngineCallback callback, WorkerThread worker, BreakpointManager bpman, AD7Engine engine, HostConfigurationStore configStore) : base(launchOptions, engine.Logger)
        {
            uint processExitCode = 0;
            _pendingMessages = new StringBuilder(400);
            _worker = worker;
            _breakpointManager = bpman;
            Engine = engine;
            _libraryLoaded = new List<string>();
            _loadOrder = 0;
            MICommandFactory = MICommandFactory.GetInstance(launchOptions.DebuggerMIMode, this);
            _waitDialog = (MICommandFactory.SupportsStopOnDynamicLibLoad() && launchOptions.WaitDynamicLibLoad) ? new HostWaitDialog(ResourceStrings.LoadingSymbolMessage, ResourceStrings.LoadingSymbolCaption) : null;
            Natvis = new Natvis.Natvis(this, launchOptions.ShowDisplayString);

            // we do NOT have real Win32 process IDs, so we use a guid
            AD_PROCESS_ID pid = new AD_PROCESS_ID();
            pid.ProcessIdType = (int)enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;
            pid.guidProcessId = Guid.NewGuid();
            this.Id = pid;

            SourceLineCache = new SourceLineCache(this);

            _callback = callback;
            _moduleList = new List<DebuggedModule>();
            ThreadCache = new ThreadCache(callback, this);
            Disassembly = new Disassembly(this);
            ExceptionManager = new ExceptionManager(MICommandFactory, _worker, _callback, configStore);

            VariablesToDelete = new List<string>();
            this.ActiveVariables = new List<IVariableInformation>();

            OutputStringEvent += delegate (object o, string message)
            {
                // We can get messages before we have started the process
                // but we can't send them on until it is
                if (_connected)
                {
                    _callback.OnOutputString(message);
                }
                else
                {
                    _pendingMessages.Append(message);
                }
            };

            LibraryLoadEvent += delegate (object o, EventArgs args)
            {
                ResultEventArgs results = args as MICore.Debugger.ResultEventArgs;
                string file = results.Results.TryFindString("host-name");
                if (!string.IsNullOrEmpty(file) && MICommandFactory.SupportsStopOnDynamicLibLoad())
                {
                    _libraryLoaded.Add(file);
                    if (_waitDialog != null)
                    {
                        _waitDialog.ShowWaitDialog(file);
                    }
                }
                else if (this.MICommandFactory.Mode == MIMode.Clrdbg)
                {
                    string id = results.Results.FindString("id");
                    ulong baseAddr = results.Results.FindAddr("base-address");
                    uint size = results.Results.FindUint("size");
                    bool symbolsLoaded = results.Results.FindInt("symbols-loaded") != 0;
                    var module = new DebuggedModule(id, file, baseAddr, size, symbolsLoaded, string.Empty, _loadOrder++);
                    lock (_moduleList)
                    {
                        _moduleList.Add(module);
                    }
                    _callback.OnModuleLoad(module);
                }
                else if (!string.IsNullOrEmpty(file))
                {
                    string addr = results.Results.TryFindString("loaded_addr");
                    if (string.IsNullOrEmpty(addr) || addr == "-")
                    {
                        return; // identifies the exe, not a real load
                    }
                    // generate module 
                    string id = results.Results.TryFindString("name");
                    bool symsLoaded = true;
                    string symPath = null;
                    if (results.Results.Contains("symbols-path"))
                    {
                        symPath = results.Results.FindString("symbols-path");
                        if (string.IsNullOrEmpty(symPath))
                        {
                            symsLoaded = false;
                        }
                    }
                    else
                    {
                        symPath = file;
                    }
                    ulong loadAddr = results.Results.FindAddr("loaded_addr");

                    uint size = results.Results.FindUint("size");
                    if (String.IsNullOrEmpty(id))
                    {
                        id = file;
                    }
                    var module = FindModule(id);
                    if (module == null)
                    {
                        module = new DebuggedModule(id, file, loadAddr, size, symsLoaded, symPath, _loadOrder++);
                        lock (_moduleList)
                        {
                            _moduleList.Add(module);
                        }
                        _callback.OnModuleLoad(module);
                    }
                }
            };

            if (_launchOptions is LocalLaunchOptions)
            {
                LocalLaunchOptions localLaunchOptions = (LocalLaunchOptions)_launchOptions;

                ITransport localTransport = null;
                // For local linux launch, use the local linux transport which creates a new terminal and uses fifos for gdb communication.
                if (PlatformUtilities.IsLinux() && // TODO: Support OSX also
                    this.MICommandFactory.UseExternalConsoleForLocalLaunch(localLaunchOptions)
                    )
                {
                    localTransport = new LocalLinuxTransport();

                    // Only need to clear terminal for linux local launch
                    _needTerminalReset = (localLaunchOptions.ProcessId == 0 && _launchOptions.DebuggerMIMode == MIMode.Gdb);
                }
                else
                {
                    localTransport = new LocalTransport();
                }

                if (localLaunchOptions.ShouldStartServer())
                {
                    this.Init(
                        new MICore.ClientServerTransport(
                            localTransport,
                            new ServerTransport(killOnClose: true, filterStdout: localLaunchOptions.FilterStdout, filterStderr: localLaunchOptions.FilterStderr)
                        ),
                        _launchOptions);
                }
                else
                {
                    this.Init(localTransport, _launchOptions);
                }
            }
            else if (_launchOptions is PipeLaunchOptions)
            {
                this.Init(new MICore.PipeTransport(), _launchOptions);
            }
            else if (_launchOptions is TcpLaunchOptions)
            {
                this.Init(new MICore.TcpTransport(), _launchOptions);
            }
            else
            {
                throw new ArgumentOutOfRangeException("LaunchInfo.options");
            }

            MIDebugCommandDispatcher.AddProcess(this);

            // When the debuggee exits, we need to exit the debugger
            ProcessExitEvent += delegate (object o, EventArgs args)
            {
                // NOTE: Exceptions leaked from this method may cause VS to crash, be careful

                ResultEventArgs results = args as MICore.Debugger.ResultEventArgs;

                if (results.Results.Contains("exit-code"))
                {
                    // GDB sometimes returns exit codes, which don't fit into uint, like "030000000472".
                    // And we can't throw from here, because it crashes VS.
                    // Full exit code will still usually be reported in the Output window,
                    // but here let's return "uint.MaxValue" just to indicate that something went wrong.
                    if (!uint.TryParse(results.Results.FindString("exit-code"), out processExitCode))
                    {
                        processExitCode = uint.MaxValue;
                    }
                }

                // quit MI Debugger
                _worker.PostOperation(CmdExitAsync);
                if (_waitDialog != null)
                {
                    _waitDialog.EndWaitDialog();
                }
            };

            // When the debugger exits, we tell AD7 we are done
            DebuggerExitEvent += delegate (object o, EventArgs args)
            {
                // NOTE: Exceptions leaked from this method may cause VS to crash, be careful

                // this is the last AD7 Event we can ever send
                // Also the transport is closed when this returns
                _callback.OnProcessExit(processExitCode);

                Dispose();
            };

            DebuggerAbortedEvent += delegate (object o, /*OPTIONAL*/ string debuggerExitCode)
            {
                // NOTE: Exceptions leaked from this method may cause VS to crash, be careful

                // The MI debugger process unexpectedly exited.
                _worker.PostOperation(() =>
                    {
                        _engineTelemetry.SendDebuggerAborted(MICommandFactory, GetLastSentCommandName(), debuggerExitCode);

                        // If the MI Debugger exits before we get a resume call, we have no way of sending program destroy. So just let start debugging fail.
                        if (!_connected)
                        {
                            return;
                        }

                        string message;
                        if (string.IsNullOrEmpty(debuggerExitCode))
                            message = string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_MIDebuggerExited_UnknownCode, MICommandFactory.Name);
                        else
                            message = string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_MIDebuggerExited_WithCode, MICommandFactory.Name, debuggerExitCode);

                        _callback.OnError(message);
                        _callback.OnProcessExit(uint.MaxValue);

                        Dispose();
                    });
            };

            ModuleLoadEvent += async delegate (object o, EventArgs args)
            {
                // NOTE: This is an async void method, so make sure exceptions are caught and somehow reported
                
                if (_needTerminalReset)
                {
                    _needTerminalReset = false;

                    // This is to work around a GDB bug of warning "Failed to set controlling terminal: Operation not permitted"
                    // Reset debuggee terminal after the first module load.
                    // The clear is done by sending reset string (ESC, c) to terminal STDERR
                    await ConsoleCmdAsync(@"shell echo -e \\033c 1>&2");                    
                }

                if (this.MICommandFactory.SupportsStopOnDynamicLibLoad() && !_launchOptions.WaitDynamicLibLoad)
                {
                    await CmdAsync("-gdb-set stop-on-solib-events 0", ResultClass.None);
                }

                if (_libraryLoaded.Count != 0)
                {
                    string moduleNames = string.Join(", ", _libraryLoaded);

                    try
                    {
                        _libraryLoaded.Clear();
                        SourceLineCache.OnLibraryLoad();

                        await _breakpointManager.BindAsync();
                        await CheckModules();

                        _bLastModuleLoadFailed = false;
                    }
                    catch (Exception e) when (ExceptionHelper.BeforeCatch(e, Logger, reportOnlyCorrupting: true))
                    {
                        if (this.ProcessState == MICore.ProcessState.Exited)
                        {
                            return; // ignore exceptions after the process has exited
                        }

                        string exceptionDescription = EngineUtils.GetExceptionDescription(e);
                        string message = string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_ExceptionProcessingModules, moduleNames, exceptionDescription);

                        // to avoid spamming the user, if the last module failed, we send the next failure to the output windiw instead of a message box
                        if (!_bLastModuleLoadFailed)
                        {
                            _callback.OnError(message);
                            _bLastModuleLoadFailed = true;
                        }
                        else
                        {
                            _callback.OnOutputMessage(new OutputMessage(message, enum_MESSAGETYPE.MT_OUTPUTSTRING, OutputMessage.Severity.Warning));
                        }
                    }
                }
                if (_waitDialog != null)
                {
                    _waitDialog.EndWaitDialog();
                }
                if (MICommandFactory.SupportsStopOnDynamicLibLoad())
                {
                    // Do not continue if debugging core dump
                    if (!this.IsCoreDump)
                    {
                        CmdContinueAsync();
                    }
                }
            };

            // When we break we need to gather information
            BreakModeEvent += async delegate (object o, EventArgs args)
            {
                // NOTE: This is an async void method, so make sure exceptions are caught and somehow reported

                ResultEventArgs results = args as MICore.Debugger.ResultEventArgs;
                if (_waitDialog != null)
                {
                    _waitDialog.EndWaitDialog();
                }

                if (!this._connected)
                {
                    _initialBreakArgs = results;
                    return;
                }

                try
                {
                    await HandleBreakModeEvent(results);
                }
                catch (Exception e) when (ExceptionHelper.BeforeCatch(e, Logger, reportOnlyCorrupting: true))
                {
                    if (this.ProcessState == MICore.ProcessState.Exited)
                    {
                        return; // ignore exceptions after the process has exited
                    }

                    string exceptionDescription = EngineUtils.GetExceptionDescription(e);
                    string message = string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_FailedToEnterBreakState, exceptionDescription);
                    _callback.OnError(message);

                    Terminate();
                }
            };

            ErrorEvent += delegate (object o, EventArgs args)
            {
                // NOTE: Exceptions leaked from this method may cause VS to crash, be careful

                ResultEventArgs result = (ResultEventArgs)args;
                _callback.OnError(result.Results.FindString("msg"));
            };

            ThreadCreatedEvent += delegate (object o, EventArgs args)
            {
                ResultEventArgs result = (ResultEventArgs)args;
                ThreadCache.ThreadEvent(result.Results.FindInt("id"), /*deleted */false);
            };

            ThreadExitedEvent += delegate (object o, EventArgs args)
            {
                ResultEventArgs result = (ResultEventArgs)args;
                ThreadCache.ThreadEvent(result.Results.FindInt("id"), /*deleted*/true);
            };

            MessageEvent += (object o, ResultEventArgs args) =>
            {
                OutputMessage outputMessage = DecodeOutputEvent(args.Results);
                if (outputMessage != null)
                {
                    _callback.OnOutputMessage(outputMessage);
                }
            };

            TelemetryEvent += (object o, ResultEventArgs args) =>
            {
                string eventName;
                KeyValuePair<string, object>[] properties;
                if (_engineTelemetry.DecodeTelemetryEvent(args.Results, out eventName, out properties))
                {
                    HostTelemetry.SendEvent(eventName, properties);
                }
            };

            BreakChangeEvent += _breakpointManager.BreakpointModified;
        }

        public async Task Initialize(HostWaitLoop waitLoop, CancellationToken token)
        {
            bool success = false;
            Natvis.Initialize(_launchOptions.VisualizerFile);
            int total = 1;

            await this.WaitForConsoleDebuggerInitialize(token);

            try
            {
                await this.MICommandFactory.EnableTargetAsyncOption();
                List<LaunchCommand> commands = GetInitializeCommands();

                total = commands.Count();
                var i = 0;
                foreach (var command in commands)
                {
                    token.ThrowIfCancellationRequested();
                    waitLoop.SetProgress(total, i++, command.Description);
                    if (command.IsMICommand)
                    {
                        Results results = await CmdAsync(command.CommandText, ResultClass.None);
                        if (results.ResultClass == ResultClass.error)
                        {
                            if (command.FailureHandler != null)
                            {
                                command.FailureHandler(results.FindString("msg"));
                            }
                            else if (!command.IgnoreFailures)
                            {
                                string miError = results.FindString("msg");
                                throw new UnexpectedMIResultException(MICommandFactory.Name, command.CommandText, miError);
                            }
                        }
                    }
                    else
                    {
                        await ConsoleCmdAsync(command.CommandText);
                    }
                }


                success = true;
            }
            finally
            {
                if (!success)
                {
                    Terminate();
                }
            }
            waitLoop.SetProgress(total, total, String.Empty);
            token.ThrowIfCancellationRequested();
        }

        private List<LaunchCommand> GetInitializeCommands()
        {
            List<LaunchCommand> commands = new List<LaunchCommand>();

            commands.AddRange(_launchOptions.SetupCommands);

            // On Windows ';' appears to correctly works as a path seperator and from the documentation, it is ':' on unix
            string pathEntrySeperator = _launchOptions.UseUnixSymbolPaths ? ":" : ";";
            string escappedSearchPath = string.Join(pathEntrySeperator, _launchOptions.GetSOLibSearchPath().Select(path => EscapePath(path, ignoreSpaces: true)));
            if (!string.IsNullOrWhiteSpace(escappedSearchPath))
            {
                if (_launchOptions.DebuggerMIMode == MIMode.Gdb)
                {
                    // Do not place quotes around so paths for gdb
                    commands.Add(new LaunchCommand("-gdb-set solib-search-path " + escappedSearchPath + pathEntrySeperator, ResourceStrings.SettingSymbolSearchPath));
                    
                }
                else
                {
                    // surround so lib path with quotes in other cases
                    commands.Add(new LaunchCommand("-gdb-set solib-search-path \"" + escappedSearchPath + pathEntrySeperator + "\"", ResourceStrings.SettingSymbolSearchPath));
                }
            }

            if (this.MICommandFactory.SupportsStopOnDynamicLibLoad())
            {
                // Do not stop on shared library load/unload events while debugging core dump.
                // Also check _needTerminalReset because we need to work around a GDB bug and clear the terminal error message. 
                // This clear operation can't be done too early (because GDB only generate this message after start debugging) 
                // or too late (otherwise we might clear debuggee's output). 
                // The stop cause by first module load seems to be the perfect timing to clear the terminal, 
                // that's why we still need to initially turn stop-on-solib-events on then turn it off after the first stop.
                if ((_needTerminalReset || _launchOptions.WaitDynamicLibLoad) && !this.IsCoreDump)
                {
                    commands.Add(new LaunchCommand("-gdb-set stop-on-solib-events 1"));
                }
            }

            // Custom launch options replace the built in launch steps. This is used on iOS
            // and Linux attach scenarios.
            if (_launchOptions.CustomLaunchSetupCommands != null)
            {
                commands.AddRange(_launchOptions.CustomLaunchSetupCommands);
            }
            else
            {
                LocalLaunchOptions localLaunchOptions = _launchOptions as LocalLaunchOptions;
                if (this.IsCoreDump)
                {
                    // Add executable information
                    this.AddExecutablePathCommand(commands);

                    // Add core dump information (linux/mac does not support quotes around this path but spaces in the path do work)
                    string coreDump = _launchOptions.UseUnixSymbolPaths ? localLaunchOptions.CoreDumpPath : EscapePath(localLaunchOptions.CoreDumpPath);
                    string coreDumpCommand = String.Concat("-target-select core ", coreDump);
                    string coreDumpDescription = String.Format(CultureInfo.CurrentCulture, ResourceStrings.LoadingCoreDumpMessage, localLaunchOptions.CoreDumpPath);
                    commands.Add(new LaunchCommand(coreDumpCommand, coreDumpDescription, ignoreFailures: false));
                }
                else if (null != localLaunchOptions && localLaunchOptions.ProcessId != 0)
                {
                    // This is an attach

                    // check for remote
                    string destination = localLaunchOptions.MIDebuggerServerAddress;
                    if (!string.IsNullOrEmpty(destination))
                    {
                        commands.Add(new LaunchCommand("-target-select remote " + destination, string.Format(CultureInfo.CurrentUICulture, ResourceStrings.ConnectingMessage, destination)));
                    }

                    int pid = localLaunchOptions.ProcessId;
                    commands.Add(new LaunchCommand(String.Format(CultureInfo.CurrentUICulture, "-target-attach {0}", pid), ignoreFailures: false));
                    return commands;
                }
                else
                {
                    // The default launch is to start a new process
                    if (!string.IsNullOrWhiteSpace(_launchOptions.ExeArguments))
                    {
                        commands.Add(new LaunchCommand("-exec-arguments " + _launchOptions.ExeArguments));
                    }

                    if (!string.IsNullOrWhiteSpace(_launchOptions.WorkingDirectory))
                    {
                        string escappedDir = EscapePath(_launchOptions.WorkingDirectory);
                        commands.Add(new LaunchCommand("-environment-cd " + escappedDir));
                    }

                    // On Windows, with CLRDBG, if we should launch a new console, set the TTY
                    if (localLaunchOptions != null &&
                        this.MICommandFactory.Mode == MIMode.Clrdbg &&
                        PlatformUtilities.IsWindows() &&
                        this.MICommandFactory.UseExternalConsoleForLocalLaunch(localLaunchOptions))
                    {
                        commands.Add(new LaunchCommand("-inferior-tty-set <new-console>"));
                    }

                    this.AddExecutablePathCommand(commands);
                    commands.Add(new LaunchCommand("-break-insert main", ignoreFailures: true));

                    if (null != localLaunchOptions)
                    {
                        string destination = localLaunchOptions.MIDebuggerServerAddress;
                        if (!string.IsNullOrEmpty(destination))
                        {
                            commands.Add(new LaunchCommand("-target-select remote " + destination, string.Format(CultureInfo.CurrentUICulture, ResourceStrings.ConnectingMessage, destination)));
                        }
                    }
                }
            }

            return commands;
        }

        private void AddExecutablePathCommand(IList<LaunchCommand> commands)
        {
            string exe = EscapePath(_launchOptions.ExePath);
            string description = string.Format(CultureInfo.CurrentUICulture, ResourceStrings.LoadingSymbolMessage, _launchOptions.ExePath);
            Action<string> failureHandler = (string miError) =>
            {
                string message = string.Format(CultureInfo.CurrentUICulture, ResourceStrings.Error_ExePathInvalid, _launchOptions.ExePath, MICommandFactory.Name, miError);
                throw new LaunchErrorException(message);
            };

            commands.Add(new LaunchCommand("-file-exec-and-symbols " + exe, description, ignoreFailures:false, failureHandler:failureHandler));
        }

        public override void FlushBreakStateData()
        {
            base.FlushBreakStateData();
            Natvis.Cache.Flush();
        }

        private void Dispose()
        {
            if (_launchOptions.DeviceAppLauncher != null)
            {
                _launchOptions.DeviceAppLauncher.Dispose();
            }
            if (_waitDialog != null)
            {
                _waitDialog.Dispose();
            }

            Logger.Flush();
        }

        private async Task HandleBreakModeEvent(ResultEventArgs results)
        {
            string reason = results.Results.TryFindString("reason");
            int tid;
            if (!results.Results.Contains("thread-id"))
            {
                Results res = await MICommandFactory.ThreadInfo();
                tid = res.FindInt("id");
            }
            else
            {
                tid = results.Results.FindInt("thread-id");
            }

            // Any existing variable objects at this point are from the last time we were in break mode, and are
            //  therefore invalid.  Dispose them so they're marked for cleanup.
            lock(this.ActiveVariables)
            {
                foreach (IVariableInformation varInfo in this.ActiveVariables)
                {
                    varInfo.Dispose();
                }
                this.ActiveVariables.Clear();
            }

            ThreadCache.MarkDirty();
            MICommandFactory.DefineCurrentThread(tid);

            DebuggedThread thread = await ThreadCache.GetThread(tid);
            await ThreadCache.StackFrames(thread);  // prepopulate the break thread in the thread cache
            ThreadContext cxt = await ThreadCache.GetThreadContext(thread);

            if (cxt == null)
            {
                // Something went seriously wrong. For instance, this can happen when the primary thread
                // of an app exits on linux while background threads continue to run with pthread_exit on the main thread
                // See https://devdiv.visualstudio.com/DefaultCollection/DevDiv/VS%20Diag%20IntelliTrace/_workItems?_a=edit&id=197616&triage=true
                // for a repro
                Debug.Fail("Failed to find thread on break event.");
                throw new Exception(String.Format(CultureInfo.CurrentUICulture, ResourceStrings.MissingThreadBreakEvent, tid));
            }

            ThreadCache.SendThreadEvents(this, null);   // make sure that new threads have been pushed to the UI

            //always delete breakpoints pending deletion on break mode
            //the flag tells us if we hit an existing breakpoint pending deletion that we need to continue

            await _breakpointManager.DeleteBreakpointsPendingDeletion();

            // Delete GDB variable objects that have been marked for cleanup
            List<string> variablesToDelete = null;
            lock (VariablesToDelete)
            {
                variablesToDelete = new List<string>(this.VariablesToDelete);
                VariablesToDelete.Clear();
            }

            foreach (var variable in variablesToDelete)
            {
                try
                {
                    await MICommandFactory.VarDelete(variable);
                }
                catch (MIException)
                {
                    //not much to do really, we're leaking MI debugger variables.
                    Debug.Fail("Failed to delete variable: " + variable + ". This is leaking memory in the MI Debugger.");
                }
            }

            if (String.IsNullOrWhiteSpace(reason) && !_bEntrypointHit)
            {
                _bEntrypointHit = true;
                CmdContinueAsync();
                FireDeviceAppLauncherResume();
            }
            else if (reason == "entry-point-hit")
            {
                _bEntrypointHit = true;
                _callback.OnEntryPoint(thread);
            }
            else if (reason == "breakpoint-hit")
            {
                string bkptno = results.Results.FindString("bkptno");
                ulong addr = cxt.pc ?? 0;

                bool fContinue;
                TupleValue frame = results.Results.TryFind<TupleValue>("frame");
                AD7BoundBreakpoint[] bkpt = _breakpointManager.FindHitBreakpoints(bkptno, addr, frame, out fContinue);
                if (bkpt != null)
                {
                    List<object> bplist = new List<object>();
                    bplist.AddRange(bkpt);
                    _callback.OnBreakpoint(thread, bplist.AsReadOnly());
                }
                else if (!_bEntrypointHit)
                {
                    _bEntrypointHit = true;
                    _callback.OnEntryPoint(thread);
                }
                else if (bkptno == "<EMBEDDED>")
                {
                    _callback.OnBreakpoint(thread, new ReadOnlyCollection<object>(new AD7BoundBreakpoint[] { }));
                }
                else
                {
                    if (fContinue)
                    {
                        //we hit a bp pending deletion
                        //post the CmdContinueAsync operation so it does not happen until we have deleted all the pending deletes
                        CmdContinueAsync();
                    }
                    else
                    {
                        // not one of our breakpoints, so stop with a message
                        _callback.OnException(thread, "Unknown breakpoint", "", 0);
                    }
                }
            }
            else if (reason == "end-stepping-range" || reason == "function-finished")
            {
                _callback.OnStepComplete(thread);
            }
            else if (reason == "signal-received")
            {
                string name = results.Results.TryFindString("signal-name");
                if ((name == "SIG32") || (name == "SIG33"))
                {
                    // we are going to ignore these (Sigma) signals for now
                    CmdContinueAsync();
                }
                else if (MICommandFactory.IsAsyncBreakSignal(results.Results))
                {
                    _callback.OnAsyncBreakComplete(thread);
                }
                else
                {
                    uint code = 0;
                    string sigName = results.Results.TryFindString("signal-name");
                    code = results.Results.Contains("signal") ? results.Results.FindUint("signal") : 0;
                    if (String.IsNullOrEmpty(sigName) && code != 0 && EngineUtils.SignalMap.Instance.ContainsValue(code))
                    {
                        sigName = EngineUtils.SignalMap.Instance.First((p) => p.Value == code).Key;
                    }
                    else if (!String.IsNullOrEmpty(sigName) && code == 0 && EngineUtils.SignalMap.Instance.ContainsKey(sigName))
                    {
                        code = EngineUtils.SignalMap.Instance[sigName];
                    }
                    _callback.OnException(thread, sigName, results.Results.TryFindString("signal-meaning"), code);
                }
            }
            else if (reason == "exception-received")
            {
                string exceptionName = results.Results.TryFindString("exception-name");
                if (string.IsNullOrEmpty(exceptionName))
                    exceptionName = "Exception";

                string description = results.Results.FindString("exception");
                Guid? exceptionCategory;
                ExceptionBreakpointState state;
                MICommandFactory.DecodeExceptionReceivedProperties(results.Results, out exceptionCategory, out state);

                _callback.OnException(thread, exceptionName, description, 0, exceptionCategory, state);
            }
            else
            {
                Debug.Fail("Unknown stopping reason");
                _callback.OnException(thread, "Unknown", "Unknown stopping event", 0);
            }
        }

        internal WorkerThread WorkerThread
        {
            get { return _worker; }
        }
        internal string EscapePath(string path, bool ignoreSpaces = false)
        {
            if (_launchOptions.UseUnixSymbolPaths)
            {
                path = path.Replace('\\', '/');
            }
            else
            {
                path = path.Trim();
                path = path.Replace(@"\", @"\\");
            }

            if (!ignoreSpaces && path.IndexOf(' ') != -1)
            {
                path = '"' + path + '"';
            }
            return path;
        }

        internal static string UnixPathToWindowsPath(string unixPath)
        {
            return unixPath.Replace('/', '\\');
        }
        private async Task CheckModules()
        {
            // NOTE: The version of GDB that comes in the Android SDK doesn't support -file-list-shared-library
            // so we need to use the console command
            //string results = await MICommandFactory.GetSharedLibrary();
            string results = await ConsoleCmdAsync("info sharedlibrary");

            using (StringReader stringReader = new StringReader(results))
            {
                while (true)
                {
                    string line = stringReader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    ulong startAddr = 0;
                    ulong endAddr = 0;
                    if (line.StartsWith("From")) // header line, ignore
                    {
                        continue;
                    }
                    else if (line.StartsWith("0x"))  // module with load address
                    {
                        // line format: 0x<hex start addr>  0x<hex end addr>  [ Yes | No ]  <filename>
                        line = MICommandFactory.SpanNextAddr(line, out startAddr);
                        if (line == null)
                        {
                            continue;
                        }
                        line = MICommandFactory.SpanNextAddr(line, out endAddr);
                        if (line == null || endAddr < startAddr)
                        {
                            continue;
                        }
                    }
                    line = line.Trim();
                    bool symbolsLoaded;
                    if (line.StartsWith("Yes"))
                    {
                        symbolsLoaded = true;
                        line = line.Substring(3);
                    }
                    else if (line.StartsWith("No"))
                    {
                        symbolsLoaded = false;
                        line = line.Substring(2);
                    }
                    else
                    {
                        continue;
                    }
                    line = line.Trim();
                    line = line.TrimEnd(new char[] { '"', '\n' });
                    var module = FindModule(line);
                    if (module == null)
                    {
                        module = new DebuggedModule(line, line, startAddr, endAddr - startAddr, symbolsLoaded, line, _loadOrder++);
                        lock (_moduleList)
                        {
                            _moduleList.Add(module);
                        }

                        _callback.OnModuleLoad(module);
                    }
                }
            }
        }

        // this is called on any thread, so we need to dispatch the command via
        // the Worker thread, to end up in DispatchCommand
        protected override void ScheduleStdOutProcessing(string line)
        {
            _worker.PostOperation(() => { ProcessStdOutLine(line); });
        }

        protected override void ScheduleResultProcessing(Action func)
        {
            _worker.PostOperation(() => { func(); });
        }

        public async Task Execute(DebuggedThread thread)
        {
            await ExceptionManager.EnsureSettingsUpdated();

            // Should clear stepping state
            if (_worker.IsPollThread())
            {
                CmdContinueAsync();
            }
            else
            {
                _worker.PostOperation(CmdContinueAsync);
            }
        }

        public Task Continue(DebuggedThread thread)
        {
            // Called after Stopping event
            return Execute(thread);
        }

        public async Task Step(int threadId, enum_STEPKIND kind, enum_STEPUNIT unit)
        {
            this.VerifyNotDebuggingCoreDump();

            await ExceptionManager.EnsureSettingsUpdated();

            if ((unit == enum_STEPUNIT.STEP_LINE) || (unit == enum_STEPUNIT.STEP_STATEMENT))
            {
                switch (kind)
                {
                    case enum_STEPKIND.STEP_INTO:
                        await MICommandFactory.ExecStep(threadId);
                        break;
                    case enum_STEPKIND.STEP_OVER:
                        await MICommandFactory.ExecNext(threadId);
                        break;
                    case enum_STEPKIND.STEP_OUT:
                        await MICommandFactory.ExecFinish(threadId);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (unit == enum_STEPUNIT.STEP_INSTRUCTION)
            {
                await MICommandFactory.ExecStepInstruction(threadId);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public DebuggedModule ResolveAddress(ulong addr)
        {
            lock (_moduleList)
            {
                return _moduleList.Find((m) => m.AddressInModule(addr));
            }
        }

        public void Close()
        {
            if (_launchOptions.DeviceAppLauncher != null)
            {
                _launchOptions.DeviceAppLauncher.Terminate();
            }
            CloseQuietly();
        }

        public async Task ResumeFromLaunch()
        {
            _connected = true;

            // Send any strings we got before the process came up
            if (_pendingMessages?.Length != 0)
            {
                _callback.OnOutputString(_pendingMessages.ToString());
                _pendingMessages = null;
            }

            await this.ExceptionManager.EnsureSettingsUpdated();

            if (_initialBreakArgs != null)
            {
                await CheckModules();
                _libraryLoaded.Clear();
                await HandleBreakModeEvent(_initialBreakArgs);
                _initialBreakArgs = null;
            }
            else if (this.IsCoreDump)
            {
                // Set initial state of debug engine to stopped with emulated results
                this.OnStateChanged("stopped", await this.GenerateStoppedRecordResults());
            }
            else
            {
                bool attach = false;
                int attachPid = 0;
                if (_launchOptions is LocalLaunchOptions)
                {
                    attachPid = ((LocalLaunchOptions)_launchOptions).ProcessId;
                    if (attachPid != 0)
                    {
                        attach = true;
                    }
                }

                if (!attach)
                {
                    switch (_launchOptions.LaunchCompleteCommand)
                    {
                        case LaunchCompleteCommand.ExecRun:
                            await MICommandFactory.ExecRun();
                            break;
                        case LaunchCompleteCommand.ExecContinue:
                            await MICommandFactory.ExecContinue();
                            break;
                        case LaunchCompleteCommand.None:
                            break;
                        default:
                            Debug.Fail("Not implemented enum code for LaunchCompleteCommand??");
                            throw new NotImplementedException();
                    }
                }

                FireDeviceAppLauncherResume();
            }
        }

        private void FireDeviceAppLauncherResume()
        {
            if (_launchOptions.DeviceAppLauncher != null)
            {
                _launchOptions.DeviceAppLauncher.OnResume();
            }
        }

        /// <summary>
        /// Generates results that represent an emulated MI stopped record.
        /// </summary>
        private async Task<Results> GenerateStoppedRecordResults()
        {
            Results threadInfo = await this.MICommandFactory.ThreadInfo();

            // Get the current thread identifier
            string currentThreadId = threadInfo.FindString("current-thread-id");

            // Get list of all threads in the process
            ValueListValue threads = threadInfo.Find<ValueListValue>("threads");

            // Find the thread that is the current thread, which should exist since there is a current thread id value
            TupleValue currentThread = threads.AsArray<TupleValue>().FirstOrDefault(tv => currentThreadId.Equals(tv.FindString("id"), StringComparison.Ordinal));
            Debug.Assert(null != currentThread, String.Concat("Unable to find thread with ID ", currentThreadId, "."));
            if (null == currentThread)
                throw new UnexpectedMIResultException(this.MICommandFactory.Name, "-thread-info", null);

            // Get the frame of the current thread
            TupleValue currentFrame = currentThread.Find<TupleValue>("frame");

            // Collect the addr, func, and args fields from the current frame as they are required.
            // Collect the file, fullname, and line fileds if they are available. They may be missing if the frame is for
            // a binary that does not have symbols.
            TupleValue newFrame = currentFrame.Subset(
                new string[] { "addr", "func", "args" },
                new string[] { "file", "fullname", "line" });

            // Create result that emulates a signal received from the debuggee with the frame and thread information
            List<NamedResultValue> values = new List<NamedResultValue>();
            values.Add(new NamedResultValue("reason", new ConstValue("signal-received")));
            values.Add(new NamedResultValue("frame", newFrame));
            values.Add(new NamedResultValue("thread-id", new ConstValue(currentThreadId)));
            return new Results(ResultClass.done, values);
        }

        public void Terminate()
        {
            // Pretend to kill the process, which will tear down the MI Debugger
            //TODO: Something better than this.
            if (_launchOptions.DeviceAppLauncher != null)
            {
                _launchOptions.DeviceAppLauncher.Terminate();
            }
            ScheduleStdOutProcessing(@"*stopped,reason=""exited"",exit-code=""42""");
        }

        public void Detach() { }
        public DebuggedModule[] GetModules()
        {
            lock (_moduleList)
            {
                return _moduleList.ToArray();
            }
        }

        public DebuggedModule FindModule(string id)
        {
            lock (_moduleList)
            {
                return _moduleList.Find((m) => m.Id == id);
            }
        }

        public bool GetSourceInformation(uint addr, ref string m_documentName, ref string m_functionName, ref uint m_lineNum, ref uint m_numParameters, ref uint m_numLocals)
        {
            return false;
        }

        public uint[] GetAddressesForSourceLocation(string moduleName, string documentName, uint dwStartLine, uint dwStartCol)
        {
            uint[] addrs = new uint[1];
            addrs[0] = 0xDEADF00D;
            return addrs;
        }

        public void SetBreakpoint(uint address, Object client)
        {
            throw new NotImplementedException();
        }

        internal void OnPostedOperationError(object sender, Exception e)
        {
            if (this.ProcessState == MICore.ProcessState.Exited)
            {
                return; // ignore exceptions after the process has exited
            }

            string exceptionMessage = e.Message.TrimEnd(' ', '\t', '.', '\r', '\n');
            string userMessage = string.Format(CultureInfo.CurrentCulture, MICoreResources.Error_ExceptionInOperation, exceptionMessage);
            _callback.OnError(userMessage);
        }

        //This method gets the locals and parameters and creates an MI debugger variable for each one so that we can manipulate them (and expand children, etc.)
        //NOTE: Eval is called
        internal async Task<List<VariableInformation>> GetLocalsAndParameters(AD7Thread thread, ThreadContext ctx)
        {
            List<VariableInformation> variables = new List<VariableInformation>();

            ValueListValue localsAndParameters = await MICommandFactory.StackListVariables(PrintValues.NoValues, thread.Id, ctx.Level);

            foreach (var localOrParamResult in localsAndParameters.Content)
            {
                string name = localOrParamResult.FindString("name");
                bool isParam = localOrParamResult.TryFindString("arg") == "1";
                SimpleVariableInformation simpleInfo = new SimpleVariableInformation(name, isParam);
                VariableInformation vi = await simpleInfo.CreateMIDebuggerVariable(ctx, Engine, thread);
                variables.Add(vi);
            }

            return variables;
        }

        //This method gets the value/type info for the method parameters without creating an MI debugger varialbe for them. For use in the callstack window
        //NOTE: eval is not called
        public async Task<List<SimpleVariableInformation>> GetParameterInfoOnly(AD7Thread thread, ThreadContext ctx)
        {
            List<SimpleVariableInformation> parameters = new List<SimpleVariableInformation>();

            ValueListValue localAndParameters = await MICommandFactory.StackListVariables(PrintValues.SimpleValues, thread.Id, ctx.Level);

            foreach (var results in localAndParameters.Content.Where(r => r.TryFindString("arg") == "1"))
            {
                parameters.Add(new SimpleVariableInformation(results.FindString("name"), /*isParam*/ true, results.FindString("value"), results.FindString("type")));
            }

            return parameters;
        }

        //This method gets the value/type info for the method parameters of all frames without creating an mi debugger varialbe for them. For use in the callstack window
        //NOTE: eval is not called
        public async Task<List<ArgumentList>> GetParameterInfoOnly(AD7Thread thread, bool values, bool types, uint low, uint high)
        {
            var frames = await MICommandFactory.StackListArguments(values || types ? PrintValues.SimpleValues : PrintValues.NoValues, thread.Id, low, high);
            List<ArgumentList> parameters = new List<ArgumentList>();

            foreach (var f in frames)
            {
                int level = f.FindInt("level");
                ListValue argList = null;
                f.TryFind<ListValue>("args", out argList);
                List<SimpleVariableInformation> args = new List<SimpleVariableInformation>();
                if (argList != null)
                {
                    if (argList is ValueListValue) // a tuple for each arg
                    {
                        foreach (var arg in ((ValueListValue)argList).Content)
                        {
                            args.Add(new SimpleVariableInformation(arg.FindString("name"), /*isParam*/ true, arg.TryFindString("value"), arg.TryFindString("type")));
                        }
                    }
                    else
                    {
                        // simple arg name list
                        string[] names = ((ResultListValue)argList).FindAllStrings("name");
                        foreach (var n in names)
                        {
                            args.Add(new SimpleVariableInformation(n, /*isParam*/ true, null, null));
                        }
                    }
                }
                parameters.Add(new ArgumentList(level, args));
            }

            return parameters;
        }

        internal async Task<uint> ReadProcessMemory(ulong address, uint count, byte[] bytes)
        {
            string cmd = "-data-read-memory-bytes " + EngineUtils.AsAddr(address, Is64BitArch) + " " + count.ToString();
            Results results = await CmdAsync(cmd, ResultClass.None);
            if (results.ResultClass == ResultClass.error)
            {
                return uint.MaxValue;
            }
            ValueListValue mem = results.Find<ValueListValue>("memory");
            if (mem.IsEmpty())
            {
                return 0;
            }
            TupleValue res = mem.Content[0] as TupleValue;
            if (res == null)
            {
                return 0;
            }
            ulong start = res.FindAddr("begin");
            ulong end = res.FindAddr("end");
            ulong offset = res.FindAddr("offset");   // for some reason this is formatted as hex
            string content = res.FindString("contents");
            uint toRead = (uint)content.Length / 2;
            if (toRead > count)
            {
                toRead = count;
            }
            // ensure the buffer contains the desired bytes.
            if (start + offset != address)
            {
                throw new MIException(Constants.E_FAIL);
            }

            for (int pos = 0; pos < toRead; ++pos)
            {
                // Decode one byte
                string strByte = content.Substring(pos * 2, 2);
                bytes[pos] = Convert.ToByte(strByte, 16);
            }
            return toRead;
        }

        private OutputMessage DecodeOutputEvent(Results results)
        {
            // NOTE: the message event is an MI Extension from clrdbg, though we could use in it the future for other debuggers
            string text = results.TryFindString("text");
            if (string.IsNullOrEmpty(text))
            {
                Debug.Fail("Bogus message event. Missing 'text' property.");
                return null;
            }

            string sendTo = results.TryFindString("send-to");
            if (string.IsNullOrEmpty(sendTo))
            {
                Debug.Fail("Bogus message event, missing 'send-to' property");
                return null;
            }

            enum_MESSAGETYPE messageType;
            switch (sendTo)
            {
                case "message-box":
                    messageType = enum_MESSAGETYPE.MT_MESSAGEBOX;
                    break;

                case "output-window":
                    messageType = enum_MESSAGETYPE.MT_OUTPUTSTRING;
                    break;

                default:
                    Debug.Fail("Bogus message event. Unexpected 'send-to' property. Ignoring.");
                    return null;
            }

            OutputMessage.Severity severity = OutputMessage.Severity.Warning;
            switch (results.TryFindString("severity"))
            {
                case "error":
                    severity = OutputMessage.Severity.Error;
                    break;

                case "warning":
                    severity = OutputMessage.Severity.Warning;
                    break;
            }

            switch (results.TryFindString("source"))
            {
                case "target-exception":
                    messageType |= enum_MESSAGETYPE.MT_REASON_EXCEPTION;
                    break;
                case "jmc-prompt":
                    messageType |= (enum_MESSAGETYPE)enum_MESSAGETYPE90.MT_REASON_JMC_PROMPT;
                    break;
                case "step-filter":
                    messageType |= (enum_MESSAGETYPE)enum_MESSAGETYPE90.MT_REASON_STEP_FILTER;
                    break;
                case "fatal-error":
                    messageType |= (enum_MESSAGETYPE)enum_MESSAGETYPE120.MT_FATAL_ERROR;
                    break;
            }

            uint errorCode = results.TryFindUint("error-code") ?? 0;
            return new OutputMessage(text, messageType, severity, errorCode);
        }

        private static RegisterGroup GetGroupForRegister(List<RegisterGroup> registerGroups, string name, EngineUtils.RegisterNameMap nameMap)
        {
            string grpName = nameMap.GetGroupName(name);
            RegisterGroup grp = registerGroups.FirstOrDefault((g) => { return g.Name == grpName; });
            if (grp == null)
            {
                grp = new RegisterGroup(grpName);
                registerGroups.Add(grp);
            }
            return grp;
        }

        private void InitializeRegisters()
        {
            WorkerThread.RunOperation(async () =>
            {
                if (_registers != null)
                    return; // already initialized

                string[] names = await MICommandFactory.DataListRegisterNames();

                if (_registers != null)
                    return; // already initialized

                EngineUtils.RegisterNameMap nameMap = EngineUtils.RegisterNameMap.Create(names);
                List<RegisterDescription> desc = new List<RegisterDescription>();
                var registerGroups = new List<RegisterGroup>();
                for (int i = 0; i < names.Length; ++i)
                {
                    if (String.IsNullOrEmpty(names[i]))
                    {
                        continue;  // ignore the empty names
                    }
                    RegisterGroup grp = GetGroupForRegister(registerGroups, names[i], nameMap);
                    desc.Add(new RegisterDescription(names[i], grp, i));
                }
                _registerGroups = registerGroups.AsReadOnly();
                _registers = desc.AsReadOnly();
            });
        }

        public ReadOnlyCollection<RegisterDescription> GetRegisterDescriptions()
        {
            // If this is called on the Worker thread it may deadlock
            Debug.Assert(!_worker.IsPollThread());

            if (_registers == null)
            {
                InitializeRegisters();
            }

            return _registers;
        }

        public ReadOnlyCollection<RegisterGroup> GetRegisterGroups()
        {
            // If this is called on the Worker thread it may deadlock
            Debug.Assert(!_worker.IsPollThread());

            if (_registerGroups == null)
            {
                InitializeRegisters();
            }

            return _registerGroups;
        }

        public async Task<Tuple<int, string>[]> GetRegisters(int threadId, uint level)
        {
            TupleValue[] values = await MICommandFactory.DataListRegisterValues(threadId);
            Tuple<int, string>[] regValues = new Tuple<int, string>[values.Length];
            for (int i = 0; i < values.Length; ++i)
            {
                int index = values[i].FindInt("number");
                string regContent = values[i].FindString("value");
                regValues[i] = new Tuple<int, string>(index, regContent);
            }
            return regValues;
        }

        public async Task DisableBreakpointsForFuncEvalAsync()
        {
            await _breakpointManager.DisableBreakpointsForFuncEvalAsync();
        }

        public async Task EnableBreakpointsAfterFuncEvalAsync()
        {
            await _breakpointManager.EnableAfterFuncEvalAsync();
        }

        public async Task<List<ulong>> StartAddressesForLine(string file, uint line)
        {
            List<ulong> addresses = new List<ulong>();
            var srcLines = await SourceLineCache.GetLinesForFile(file);
            if (srcLines == null || srcLines.Length == 0)
            {
                srcLines = await SourceLineCache.GetLinesForFile(System.IO.Path.GetFileName(file));
            }
            if (srcLines != null && srcLines.Length > 0)
            {
                bool gotoNextFunc = false;
                foreach (var l in srcLines)
                {
                    if (gotoNextFunc)
                    {
                        if (l.Line == 0)
                        {
                            gotoNextFunc = false;
                        }
                    }
                    else if (line == l.Line)
                    {
                        addresses.Add(l.AddrStart);
                        gotoNextFunc = true;
                    }
                }
            }
            if (addresses.Count == 0)
            {
                // ask the underlying debugger for the line info
                addresses = await MICommandFactory.StartAddressesForLine(EscapePath(file), line);
            }
            return addresses;
        }
    }
}
