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

namespace Microsoft.MIDebugEngine
{
    internal class DebuggedProcess : MICore.Debugger
    {
        public static DebuggedProcess g_Process; // TODO: Remove
        public AD_PROCESS_ID Id { get; private set; }
        public AD7Engine Engine { get; private set; }
        public List<string> VariablesToDelete { get; private set; }

        public SourceLineCache SourceLineCache { get; private set; }
        public ThreadCache ThreadCache { get; private set; }
        public Disassembly Disassembly { get; private set; }

        private List<DebuggedModule> _moduleList;
        private ISampleEngineCallback _callback;
        private bool _bStarted;
        private bool _bLastModuleLoadFailed;
        private StringBuilder _pendingMessages;
        private WorkerThread _worker;
        private readonly LaunchOptions _launchOptions;
        private BreakpointManager _breakpointManager;
        private bool _bEntrypointHit;
        private ResultEventArgs _initialBreakArgs;
        private List<string> _libraryLoaded;   // unprocessed library loaded messages
        private uint _loadOrder;
        private WaitDialog _waitDialog;
        public readonly Natvis.Natvis Natvis;
        private ReadOnlyCollection<RegisterDescription> _registers;
        private ReadOnlyCollection<RegisterGroup> _registerGroups;

        public DebuggedProcess(bool bLaunched, LaunchOptions launchOptions, ISampleEngineCallback callback, WorkerThread worker, BreakpointManager bpman, AD7Engine engine)
        {
            uint processExitCode = 0;
            g_Process = this;
            _bStarted = false;
            _pendingMessages = new StringBuilder(400);
            _worker = worker;
            _launchOptions = launchOptions;
            _breakpointManager = bpman;
            Engine = engine;
            _libraryLoaded = new List<string>();
            _loadOrder = 0;
            MICommandFactory = MICommandFactory.GetInstance(launchOptions.DebuggerMIMode, this);
            _waitDialog = MICommandFactory.SupportsStopOnDynamicLibLoad() ? new WaitDialog(ResourceStrings.LoadingSymbolMessage, ResourceStrings.LoadingSymbolCaption) : null;
            Natvis = new Natvis.Natvis(this);

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

            VariablesToDelete = new List<string>();

            MessageEvent += delegate (object o, string message)
            {
                // We can get messages before we have started the process
                // but we can't send them on until it is
                if (_bStarted)
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
                this.Init(new MICore.LocalTransport(), _launchOptions);
            }
            else if (_launchOptions is PipeLaunchOptions)
            {
                this.Init(new MICore.PipeTransport(), _launchOptions);
            }
            else if (_launchOptions is TcpLaunchOptions)
            {
                this.Init(new MICore.TcpTransport(), _launchOptions);
            }
            else if (_launchOptions is SerialLaunchOptions)
            {
                string port = ((SerialLaunchOptions)_launchOptions).Port;
                this.Init(new MICore.SerialTransport(port), _launchOptions);
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
                    processExitCode = results.Results.FindUint("exit-code");
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

            DebuggerAbortedEvent += delegate (object o, EventArgs args)
            {
                // NOTE: Exceptions leaked from this method may cause VS to crash, be careful

                // The MI debugger process unexpectedly exited.
                _worker.PostOperation(() =>
                    {
                        // If the MI Debugger exits before we get a resume call, we have no way of sending program destroy. So just let start debugging fail.
                        if (!_connected)
                        {
                            return;
                        }

                        _callback.OnError(MICoreResources.Error_MIDebuggerExited);
                        _callback.OnProcessExit(uint.MaxValue);

                        Dispose();
                    });
            };

            ModuleLoadEvent += async delegate (object o, EventArgs args)
            {
                // NOTE: This is an async void method, so make sure exceptions are caught and somehow reported

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
                    catch (Exception e)
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
                            _callback.OnOutputMessage(message, enum_MESSAGETYPE.MT_OUTPUTSTRING);
                        }
                    }
                }
                if (_waitDialog != null)
                {
                    _waitDialog.EndWaitDialog();
                }
                if (MICommandFactory.SupportsStopOnDynamicLibLoad())
                {
                    CmdContinueAsync();
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
                catch (Exception e)
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

            RunModeEvent += delegate (object o, EventArgs args)
            {
                // NOTE: Exceptions leaked from this method may cause VS to crash, be careful

                if (!_bStarted)
                {
                    _bStarted = true;

                    // Send any strings we got before the process came up
                    if (_pendingMessages.Length != 0)
                    {
                        try
                        {
                            _callback.OnOutputString(_pendingMessages.ToString());
                        }
                        catch
                        {
                            // If something goes wrong sending the output, lets not crash VS
                        }
                    }
                    _pendingMessages = null;
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

            BreakChangeEvent += _breakpointManager.BreakpointModified;
        }

        public async Task Initialize(MICore.WaitLoop waitLoop, CancellationToken token)
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
                        if (results.ResultClass == ResultClass.error && !command.IgnoreFailures)
                        {
                            string miError = results.FindString("msg");
                            throw new UnexpectedMIResultException(command.CommandText, miError);
                        }
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
                commands.Add(new LaunchCommand("-gdb-set solib-search-path \"" + escappedSearchPath + pathEntrySeperator + "\"", ResourceStrings.SettingSymbolSearchPath));
            }

            if (this.MICommandFactory.SupportsStopOnDynamicLibLoad())
            {
                commands.Add(new LaunchCommand("-gdb-set stop-on-solib-events 1"));
            }

            // Custom launch options replace the built in launch steps. This is used on iOS
            // and Linux attach scenarios.
            if (_launchOptions.CustomLaunchSetupCommands != null)
            {
                commands.AddRange(_launchOptions.CustomLaunchSetupCommands);
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

                string exe = EscapePath(_launchOptions.ExePath);
                commands.Add(new LaunchCommand("-file-exec-and-symbols " + exe, string.Format(CultureInfo.CurrentUICulture, ResourceStrings.LoadingSymbolMessage, _launchOptions.ExePath)));

                commands.Add(new LaunchCommand("-break-insert main", ignoreFailures:true));

                if (_launchOptions is LocalLaunchOptions)
                {
                    string destination = ((LocalLaunchOptions)_launchOptions).MIDebuggerServerAddress;
                    if (!string.IsNullOrEmpty(destination))
                    {
                        commands.Add(new LaunchCommand("-target-select remote " + destination, string.Format(CultureInfo.CurrentUICulture, ResourceStrings.ConnectingMessage, destination)));
                    }
                }
            }

            return commands;
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

            ThreadCache.MarkDirty();
            MICommandFactory.DefineCurrentThread(tid);

            DebuggedThread thread = await ThreadCache.GetThread(tid);
            await ThreadCache.StackFrames(thread);  // prepopulate the break thread in the thread cache
            ThreadContext cxt = await ThreadCache.GetThreadContext(thread);
            ThreadCache.SendThreadEvents(this, null);   // make sure that new threads have been pushed to the UI

            //always delete breakpoints pending deletion on break mode
            //the flag tells us if we hit an existing breakpoint pending deletion that we need to continue

            await _breakpointManager.DeleteBreakpointsPendingDeletion();

            //delete varialbes that have been GC'd
            List<string> variablesToDelete = new List<string>();
            lock (VariablesToDelete)
            {
                foreach (var variable in VariablesToDelete)
                {
                    variablesToDelete.Add(variable);
                }
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
                // CLRDBG TODO: Try to verify this code path
                _bEntrypointHit = true;
                CmdContinueAsync();
                FireDeviceAppLauncherResume();
            }
            else if (reason == "breakpoint-hit")
            {
                string bkptno = results.Results.FindString("bkptno");
                ulong addr = cxt.pc ?? 0;
                AD7BoundBreakpoint bkpt = null;
                bool fContinue;
                TupleValue frame = results.Results.TryFind<TupleValue>("frame");
                bkpt = _breakpointManager.FindHitBreakpoint(bkptno, addr, frame, out fContinue); // use breakpoint number to resolve breakpoint
                if (bkpt != null)
                {
                    List<object> bplist = new List<object>();
                    bplist.Add(bkpt);
                    _callback.OnBreakpoint(thread, bplist.AsReadOnly());
                }
                else if (!_bEntrypointHit)
                {
                    _bEntrypointHit = true;
                    _callback.OnEntryPoint(thread);
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
                string exception = results.Results.FindString("exception");
                _callback.OnException(thread, "Exception", exception, 0);
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
                return path.Replace('\\', '/');
            else
            {
                path = path.Trim();
                path = path.Replace(@"\", @"\\");
                if (!ignoreSpaces && path.IndexOf(' ') != -1)
                {
                    path = '"' + path + '"';
                }
                return path;
            }
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
                        break;

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

        public void Execute(DebuggedThread thread)
        {
            // Should clear stepping state
            _worker.PostOperation(CmdContinueAsync);
        }

        public void Continue(DebuggedThread thread)
        {
            // Called after Stopping event
            Execute(thread);
        }

        public async Task Step(int threadId, enum_STEPKIND kind, enum_STEPUNIT unit)
        {
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
            if (_initialBreakArgs != null)
            {
                await CheckModules();
                _libraryLoaded.Clear();
                await HandleBreakModeEvent(_initialBreakArgs);
                _initialBreakArgs = null;
            }
            else
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
            string cmd = "-data-read-memory-bytes " + EngineUtils.AsAddr(address) + " " + count.ToString();
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
