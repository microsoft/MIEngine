// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using Microsoft.DebugEngineHost;
using Microsoft.VisualStudio.Debugger.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Logger = MICore.Logger;

namespace Microsoft.MIDebugEngine
{
    internal class DebuggedProcess : MICore.Debugger
    {
        public AD_PROCESS_ID Id { get; private set; }
        public AD7Engine Engine { get; private set; }
        public List<string> VariablesToDelete { get; private set; }
        public List<IVariableInformation> ActiveVariables { get; private set; }
        public VariableInformation ReturnValue { get; private set; }
        public SourceLineCache SourceLineCache { get; private set; }
        public ThreadCache ThreadCache { get; private set; }
        public Disassembly Disassembly { get; private set; }
        public ExceptionManager ExceptionManager { get; private set; }
        public CygwinFilePathMapper CygwinFilePathMapper { get; private set; }

        private List<DebuggedModule> _moduleList;
        private ISampleEngineCallback _callback;
        private bool _bLastModuleLoadFailed;
        private StringBuilder _pendingMessages;
        private WorkerThread _worker;
        private BreakpointManager _breakpointManager;
        private ResultEventArgs _initialBreakArgs;
        private List<string> _libraryLoaded;   // unprocessed library loaded messages
        private uint _loadOrder;
        private HostWaitDialog _waitDialog;
        public readonly Natvis.Natvis Natvis;
        private ReadOnlyCollection<RegisterDescription> _registers;
        private ReadOnlyCollection<RegisterGroup> _registerGroups;
        private readonly EngineTelemetry _engineTelemetry = new EngineTelemetry();
        private bool _needTerminalReset;
        private HashSet<Tuple<string, string>> _fileTimestampWarnings;
        private IProcessSequence _childProcessHandler;
        private bool _deleteEntryPointBreakpoint;
        private string _entryPointBreakpoint = string.Empty;

        public DebuggedProcess(bool bLaunched, LaunchOptions launchOptions, ISampleEngineCallback callback, WorkerThread worker, BreakpointManager bpman, AD7Engine engine, HostConfigurationStore configStore, HostWaitLoop waitLoop = null) : base(launchOptions, engine.Logger)
        {
            uint processExitCode = 0;
            _pendingMessages = new StringBuilder(400);
            _worker = worker;
            _breakpointManager = bpman;
            Engine = engine;
            _libraryLoaded = new List<string>();
            _loadOrder = 0;
            _deleteEntryPointBreakpoint = false;
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
            _fileTimestampWarnings = new HashSet<Tuple<string, string>>();

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
                string file = results.Results.TryFindString("id");
                if (!string.IsNullOrEmpty(file) && MICommandFactory.SupportsStopOnDynamicLibLoad())
                {
                    _libraryLoaded.Add(file);
                    if (_waitDialog != null)
                    {
                        _waitDialog.ShowWaitDialog(file);
                    }
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
                    AddModule(id, file, loadAddr, size, symsLoaded, symPath);
                }
            };

            if (_launchOptions is LocalLaunchOptions)
            {
                LocalLaunchOptions localLaunchOptions = (LocalLaunchOptions)_launchOptions;

                if (!localLaunchOptions.IsValidMiDebuggerPath())
                {
                    throw new Exception(MICoreResources.Error_InvalidMiDebuggerPath);
                }

                if (PlatformUtilities.IsOSX() &&
                    localLaunchOptions.DebuggerMIMode != MIMode.Lldb &&
                    !UnixUtilities.IsBinarySigned(localLaunchOptions.MIDebuggerPath, engine.Logger))
                {
                    string message = String.Format(CultureInfo.CurrentCulture, ResourceStrings.Warning_DarwinDebuggerUnsigned, localLaunchOptions.MIDebuggerPath);
                    _callback.OnOutputMessage(new OutputMessage(
                        message + Environment.NewLine,
                        enum_MESSAGETYPE.MT_MESSAGEBOX,
                        OutputMessage.Severity.Warning));
                }

                ITransport localTransport;

                // Attempt to support RunInTerminal first when it is a local launch and it is not debugging a coredump.
                // Also since we use gdb-set new-console on in windows for external console, we don't need to RunInTerminal
                if (HostRunInTerminal.IsRunInTerminalAvailable()
                    && string.IsNullOrWhiteSpace(localLaunchOptions.MIDebuggerServerAddress)
                    && string.IsNullOrWhiteSpace(localLaunchOptions.DebugServer)
                    && IsCoreDump == false
                    && (PlatformUtilities.IsWindows() ? !localLaunchOptions.UseExternalConsole : true)
                    && !PlatformUtilities.IsOSX())
                {
                    localTransport = new RunInTerminalTransport();

                    if (PlatformUtilities.IsLinux() || PlatformUtilities.IsOSX())
                    {
                        // Only need to clear terminal for Linux and OS X local launch
                        _needTerminalReset = (!localLaunchOptions.ProcessId.HasValue && _launchOptions.DebuggerMIMode == MIMode.Gdb);
                    }
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

                // Only need to know the debugger pid on Linux and OS X local launch to detect whether
                // the debugger is closed. If the debugger is not running anymore, the response (^exit)
                // to the -gdb-exit command is faked to allow MIEngine to shut down.
                // For RunInTransport, this needs to be updated via a callback.
                if (localTransport is RunInTerminalTransport)
                {
                    ((RunInTerminalTransport)localTransport).RegisterDebuggerPidCallback(SetDebuggerPid);
                }
                else
                {
                    SetDebuggerPid(localTransport.DebuggerPid);
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
            else if (_launchOptions is UnixShellPortLaunchOptions)
            {
                this.Init(new MICore.UnixShellPortTransport(), _launchOptions, waitLoop);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(launchOptions));
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
                if (!this.IsClosed)
                {
                    _worker.PostOperation(CmdExitAsync);
                }
                else
                {
                    // If we are already closed, make sure that something sends program destroy
                    _callback.OnProcessExit(processExitCode);
                }

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

            DebuggerAbortedEvent += delegate (object o, DebuggerAbortedEventArgs eventArgs)
            {
                // NOTE: Exceptions leaked from this method may cause VS to crash, be careful

                // The MI debugger process unexpectedly exited.
                _worker.PostOperation(() =>
                    {
                        _engineTelemetry.SendDebuggerAborted(MICommandFactory, GetLastSentCommandName(), eventArgs.ExitCode);

                        // If the MI Debugger exits before we get a resume call, we have no way of sending program destroy. So just let start debugging fail.
                        if (!_connected)
                        {
                            return;
                        }

                        _callback.OnError(string.Concat(eventArgs.Message, " ", ResourceStrings.DebuggingWillAbort));
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
                    await ResetConsole();
                }

                if (this.MICommandFactory.SupportsStopOnDynamicLibLoad() && !_launchOptions.WaitDynamicLibLoad)
                {
                    await CmdAsync("-gdb-set stop-on-solib-events 0", ResultClass.None);
                }

                await this.EnsureModulesLoaded();


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

                StoppingEventArgs results = args as MICore.Debugger.StoppingEventArgs;
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
                    await HandleBreakModeEvent(results, results.AsyncRequest);
                }
                catch (Exception e) when (ExceptionHelper.BeforeCatch(e, Logger, reportOnlyCorrupting: true))
                {
                    if (this.IsStopDebuggingInProgress)
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
                // In lldb, the format is ^error,message=""
                // In gdb/vsdbg it is ^error,msg=""
                string message = result.Results.TryFindString("msg");
                if (String.IsNullOrWhiteSpace(message))
                {
                    message = result.Results.TryFindString("message");
                }
                // if the command was abort (usually because of breakpoints failing to bind) then gdb writes messages into the output
                if (this.MICommandFactory.Mode == MIMode.Gdb && message == "Command aborted.")
                {
                    message = MICoreResources.Error_CommandAborted;
                    if (ProcessState == ProcessState.Running)
                    {
                        // assume that it was a continue command that got aborted and return to stopped state:
                        // this occurs when using openocd to debug embedded devices and it runs out of hardware breakpoints.
                        int currentThread = MICommandFactory.CurrentThread;
                        if (currentThread == 0)
                        {
                            currentThread = 1;  // default to main thread is current doesn't have a valid value for some reason
                        }
                        ScheduleStdOutProcessing(string.Format(CultureInfo.CurrentCulture, @"*stopped,reason=""exception-received"",signal-name=""SIGINT"",thread-id=""{1}"",exception=""{0}""", MICoreResources.Info_UnableToContinue, currentThread));
                    }
                }
                _callback.OnError(message);
            };

            ThreadCreatedEvent += async delegate (object o, EventArgs args)
            {
                try
                {
                    ResultEventArgs result = (ResultEventArgs)args;
                    await ThreadCache.ThreadCreatedEvent(result.Results.FindInt("id"), result.Results.TryFindString("group-id"));
                    _childProcessHandler?.ThreadCreatedEvent(result.Results);
                }
                catch (Exception e) when (ExceptionHelper.BeforeCatch(e, Logger, reportOnlyCorrupting: true))
                {
                    // Avoid crashing VS
                }
            };

            ThreadExitedEvent += delegate (object o, EventArgs args)
            {
                ResultEventArgs result = (ResultEventArgs)args;
                ThreadCache.ThreadExitedEvent(result.Results.FindInt("id"));
            };

            ThreadGroupExitedEvent += delegate (object o, EventArgs args)
            {
                ResultEventArgs result = (ResultEventArgs)args;
                ThreadCache.ThreadGroupExitedEvent(result.Results.FindString("id"));
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

            BreakChangeEvent += async delegate (object o, EventArgs args)
            {
                try
                {
                    await _breakpointManager.BreakpointModified(o, args);
                }
                catch (Exception e) when (ExceptionHelper.BeforeCatch(e, Logger, reportOnlyCorrupting: true))
                { }
            };
        }

        /// <summary>
        /// GetFileName - returns all characters after the last directory separator
        /// If no directory spearator is found or at least one charactar after the separator is not found 
        /// then return the original string.
        /// </summary>
        private static string GetFileName(string path)
        {
            int index = path.LastIndexOfAny(new char[] { '/', '\\' });
            if (index >= 0 && index < path.Length - 1)
            {
                return path.Substring(index + 1);
            }
            else // no path separator or no characters after the separator, return the original string
            {
                return path;
            }
        }

        private async Task EnsureModulesLoaded()
        {
            if (_libraryLoaded.Count != 0)
            {
                string moduleNames = string.Join(", ", _libraryLoaded);

                try
                {
                    // custom symbol loading?
                    //  Lookup each file in the exception list.
                    //      If there then 
                    //          if loadAll==false then load file
                    //      else
                    //          if loadAll==true then load file
                    if (!_launchOptions.CanAutoLoadSymbols())
                    {
                        foreach (string file in _libraryLoaded)
                        {
                            string filename = GetFileName(file);
                            if (_launchOptions.SymbolInfoExceptionList.Contains(filename))
                            {
                                if (!_launchOptions.SymbolInfoLoadAll)
                                {
                                    await LoadSymbols(filename);
                                }
                            }
                            else
                            {
                                if (_launchOptions.SymbolInfoLoadAll)
                                {
                                    await LoadSymbols(filename);
                                }
                            }
                        }
                    }

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
                List<LaunchCommand> commands = await GetInitializeCommands();
                _childProcessHandler?.Enable();

                total = commands.Count;
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
                        else
                        {
                            if (command.SuccessHandler != null)
                            {
                                await command.SuccessHandler(results.ToString());
                            }

                            if (command.SuccessResultsHandler != null)
                            {
                                await command.SuccessResultsHandler(results);
                            }
                        }
                    }
                    else
                    {
                        string resultString = await ConsoleCmdAsync(command.CommandText, allowWhileRunning: false, ignoreFailures: command.IgnoreFailures);
                        if (command.SuccessHandler != null)
                        {
                            await command.SuccessHandler(resultString);
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

        private async Task<List<LaunchCommand>> GetInitializeCommands()
        {
            List<LaunchCommand> commands = new List<LaunchCommand>();

            commands.AddRange(_launchOptions.SetupCommands);

            if (_launchOptions.DebuggerMIMode == MIMode.Gdb)
            {
                commands.Add(new LaunchCommand("-interpreter-exec console \"set pagination off\""));
            }

            // When user specifies loading directives then the debugger cannot auto load symbols, the MIEngine must intervene at each solib-load event and make a determination
            commands.Add(new LaunchCommand("-gdb-set auto-solib-add " + (_launchOptions.CanAutoLoadSymbols() ? "on" : "off")));

            // If the absolute prefix so path has not been specified, then don't set it to null
            // because the debugger might already have a default.
            if (!string.IsNullOrEmpty(_launchOptions.AbsolutePrefixSOLibSearchPath))
            {
                commands.Add(new LaunchCommand("-gdb-set solib-absolute-prefix " + _launchOptions.AbsolutePrefixSOLibSearchPath));
            }

            // On Windows ';' appears to correctly works as a path seperator and from the documentation, it is ':' on unix
            string pathEntrySeperator = _launchOptions.UseUnixSymbolPaths ? ":" : ";";
            string escapedSearchPath = string.Join(pathEntrySeperator, _launchOptions.GetSOLibSearchPath().Select(path => EscapeSymbolPath(path, ignoreSpaces: true)));
            if (!string.IsNullOrWhiteSpace(escapedSearchPath))
            {
                if (_launchOptions.DebuggerMIMode == MIMode.Gdb)
                {
                    // Do not place quotes around so paths for gdb
                    commands.Add(new LaunchCommand("-gdb-set solib-search-path " + escapedSearchPath + pathEntrySeperator, ResourceStrings.SettingSymbolSearchPath));
                }
                else
                {
                    // surround so lib path with quotes in other cases
                    commands.Add(new LaunchCommand("-gdb-set solib-search-path \"" + escapedSearchPath + pathEntrySeperator + "\"", ResourceStrings.SettingSymbolSearchPath));
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

            if (MICommandFactory.SupportsChildProcessDebugging())
            {
                if (_launchOptions.DebugChildProcesses)
                {
                    _childProcessHandler = new DebugUnixChild(this, this._launchOptions);  // TODO: let the user enable/disable this functionality
                }
            }

            // Custom launch options replace the built in launch steps. This is used on iOS
            // and Linux attach scenarios.
            if (_launchOptions.CustomLaunchSetupCommands != null)
            {
                commands.AddRange(_launchOptions.CustomLaunchSetupCommands);

                SetTargetArch(_launchOptions.TargetArchitecture);
            }
            else
            {
                LocalLaunchOptions localLaunchOptions = _launchOptions as LocalLaunchOptions;
                if (this.IsCoreDump)
                {
                    // Add executable information
                    this.AddExecutablePathCommand(commands);

                    // Important: this must occur after file-exec-and-symbols but before anything else.
                    this.AddGetTargetArchitectureCommand(commands);

                    // Add core dump information (linux/mac does not support quotes around this path but spaces in the path do work)
                    string coreDump = this.UseUnixPathSeparators ? _launchOptions.CoreDumpPath : this.EnsureProperPathSeparators(_launchOptions.CoreDumpPath);
                    string coreDumpCommand = _launchOptions.DebuggerMIMode == MIMode.Lldb ? String.Concat("target create --core ", coreDump) : String.Concat("-target-select core ", coreDump);
                    string coreDumpDescription = String.Format(CultureInfo.CurrentCulture, ResourceStrings.LoadingCoreDumpMessage, _launchOptions.CoreDumpPath);
                    commands.Add(new LaunchCommand(coreDumpCommand, coreDumpDescription, ignoreFailures: false));
                }
                else if (_launchOptions.ProcessId.HasValue)
                {
                    // This is an attach

                    CheckCygwin(commands, localLaunchOptions);

                    if (this.MICommandFactory.Mode == MIMode.Gdb)
                    {
                        if (_launchOptions is UnixShellPortLaunchOptions)
                        {
                            // This code path is probably applicable when the ExePath is not specified and can be used to determine the full executable path.
                            // For now it is limited to Linux and debugger running on remote machine.
                            Debug.Assert(_launchOptions.ExePath == null);

                            DetermineAndAddExecutablePathCommand(commands, _launchOptions as UnixShellPortLaunchOptions);
                        }
                        else if (!string.IsNullOrWhiteSpace(_launchOptions.ExePath))
                        {
                            this.AddExecutablePathCommand(commands);
                        }
                    }

                    // Important: this must occur after file-exec-and-symbols but before anything else.
                    this.AddGetTargetArchitectureCommand(commands);

                    // check for remote
                    string destination = localLaunchOptions?.MIDebuggerServerAddress;
                    if (!string.IsNullOrWhiteSpace(destination))
                    {
                        commands.Add(new LaunchCommand("-target-select remote " + destination, string.Format(CultureInfo.CurrentCulture, ResourceStrings.ConnectingMessage, destination)));
                    }
                    else // gdbserver is already attached when using LocalLaunchOptions
                    {
                        Action<string> failureHandler = (string miError) =>
                        {
                            if (miError.Trim().StartsWith("ptrace:", StringComparison.OrdinalIgnoreCase))
                            {
                                string message = string.Format(CultureInfo.CurrentCulture, ResourceStrings.Error_PTraceFailure, _launchOptions.ProcessId, MICommandFactory.Name, miError);
                                throw new LaunchErrorException(message);
                            }
                            else
                            {
                                string message = string.Format(CultureInfo.CurrentCulture, ResourceStrings.Error_ExePathInvalid, _launchOptions.ExePath, MICommandFactory.Name, miError);
                                throw new LaunchErrorException(message);
                            }
                        };

                        commands.Add(new LaunchCommand("-target-attach " + _launchOptions.ProcessId.Value.ToString(CultureInfo.InvariantCulture), ignoreFailures: false, failureHandler: failureHandler));
                    }

                    if (this.MICommandFactory.Mode == MIMode.Lldb)
                    {
                        // LLDB finishes attach in break mode. Gdb does finishes in run mode. Issue a continue in lldb to match the gdb behavior
                        commands.Add(new LaunchCommand("-exec-continue", ignoreFailures: false));
                    }

                    return commands;
                }
                else
                {
                    // The default launch is to start a new process

                    if (!string.IsNullOrWhiteSpace(_launchOptions.WorkingDirectory))
                    {
                        string escapedDir = this.EnsureProperPathSeparators(_launchOptions.WorkingDirectory);
                        commands.Add(new LaunchCommand("-environment-cd " + escapedDir));
                    }

                    // TODO: The last clause for LLDB may need to be changed when we support LLDB on Linux as LLDB's tty redirection doesn't work.
                    if (localLaunchOptions != null &&
                        localLaunchOptions.UseExternalConsole &&
                        (PlatformUtilities.IsWindows() ||
                            (PlatformUtilities.IsOSX() && this.MICommandFactory.Mode == MIMode.Lldb)))
                    {
                        commands.Add(new LaunchCommand("-gdb-set new-console on", ignoreFailures: true));
                    }

                    CheckCygwin(commands, localLaunchOptions);

                    this.AddExecutablePathCommand(commands);

                    // Important: this must occur after file-exec-and-symbols but before anything else.
                    this.AddGetTargetArchitectureCommand(commands);

                    // LLDB requires -exec-arguments after -file-exec-and-symbols has been run, or else it errors
                    if (!string.IsNullOrWhiteSpace(_launchOptions.ExeArguments))
                    {
                        commands.Add(new LaunchCommand("-exec-arguments " + _launchOptions.ExeArguments));
                    }

                    Func<Results, Task> breakMainSuccessResultsHandler = (Results bkptResult) =>
                    {
                        if (bkptResult.Contains("bkpt"))
                        {
                            ResultValue b = bkptResult.Find("bkpt");
                            TupleValue bkpt = null;
                            if (b is TupleValue)
                            {
                                bkpt = b as TupleValue;
                            }
                            else if (b is ValueListValue) // Used when main breakpoint binds in more than one location
                            {
                                // Grab the first one as this is usually the <MULTIPLE> one that we can unbind them all with.
                                // This is usually "1" when the children manifest as "1.1", "1.2", etc
                                bkpt = (b as ValueListValue).Content[0] as TupleValue;
                            }

                            if (bkpt != null)
                            {
                                this._entryPointBreakpoint = bkpt.FindString("number");
                                this._deleteEntryPointBreakpoint = true;
                            }
                        }
                        return Task.FromResult(0);
                    };

                    // Builds '-break-insert' for 'main'.
                    StringBuilder breakInsertCommand = await this.MICommandFactory.BuildBreakInsert(condition: null, enabled: true);
                    breakInsertCommand.Append("main");

                    commands.Add(new LaunchCommand(breakInsertCommand.ToString(), ignoreFailures: true, successResultsHandler: breakMainSuccessResultsHandler));

                    if (null != localLaunchOptions)
                    {
                        string destination = localLaunchOptions.MIDebuggerServerAddress;
                        if (!string.IsNullOrWhiteSpace(destination))
                        {
                            commands.Add(new LaunchCommand("-target-select remote " + destination, string.Format(CultureInfo.CurrentCulture, ResourceStrings.ConnectingMessage, destination)));
                        }

                    }

                    // Environment variables are set for the debuggee only with the modes that support that
                    foreach (EnvironmentEntry envEntry in _launchOptions.Environment)
                    {
                        commands.Add(new LaunchCommand(MICommandFactory.GetSetEnvironmentVariableCommand(envEntry.Name, envEntry.Value)));
                    }
                }
            }

            return commands;
        }

        private void CheckCygwin(List<LaunchCommand> commands, LocalLaunchOptions localLaunchOptions)
        {
            // If running locally on windows, determine if gdb is running from cygwin
            if (localLaunchOptions != null && PlatformUtilities.IsWindows() && this.MICommandFactory.Mode == MIMode.Gdb)
            {
                // mingw will not implement this command, but to be safe, also check if the results contains the string cygwin.
                LaunchCommand lc = new LaunchCommand("show configuration", null, true, null, (string resStr) =>
                {
                    if (resStr.Contains("cygwin") || resStr.Contains("msys"))
                    {
                        this.IsCygwin = true;
                        this.CygwinFilePathMapper = new CygwinFilePathMapper(this);

                        _engineTelemetry.SendWindowsRuntimeEnvironment(EngineTelemetry.WindowsRuntimeEnvironment.Cygwin);
                    }
                    else
                    {
                        this.IsMinGW = true;
                        // Gdb on windows and not cygwin implies mingw
                        _engineTelemetry.SendWindowsRuntimeEnvironment(EngineTelemetry.WindowsRuntimeEnvironment.MinGW);
                    }

                    return Task.FromResult(0);
                });
                commands.Add(lc);
            }
        }

        private void AddExecutablePathCommand(IList<LaunchCommand> commands)
        {
            string exe = this.EnsureProperPathSeparators(_launchOptions.ExePath);
            string description = string.Format(CultureInfo.CurrentCulture, ResourceStrings.LoadingSymbolMessage, _launchOptions.ExePath);

            Action<string> failureHandler = (string miError) =>
            {
                string message = string.Format(CultureInfo.CurrentCulture, ResourceStrings.Error_ExePathInvalid, _launchOptions.ExePath, MICommandFactory.Name, miError);
                throw new LaunchErrorException(message);
            };

            commands.Add(new LaunchCommand("-file-exec-and-symbols " + exe, description, ignoreFailures: false, failureHandler: failureHandler));
        }

        private void DetermineAndAddExecutablePathCommand(IList<LaunchCommand> commands, UnixShellPortLaunchOptions launchOptions)
        {
            // TODO: connecting to OSX via SSH doesn't work yet. Show error after connection manager dialog gets dismissed.

            // Runs a shell command to get the full path of the exe.
            // /proc file system does not exist on OSX. And querying lsof on privilaged process fails with no output on Mac, while on Linux the command succeedes with 
            // embedded error text in lsof output like "(readlink error)". 
            string absoluteExePath;

            // Must have a processId
            Debug.Assert(_launchOptions.ProcessId.HasValue, "ProcessId should have a value.");

            if (launchOptions.UnixPort.IsOSX())
            {
                // Usually the first FD=txt in the output of lsof points to the executable.
                absoluteExePath = string.Format(CultureInfo.InvariantCulture, "shell lsof -p {0} | awk '$4 == \"txt\" {{ print $9 }}'|awk 'NR==1 {{print $1}}'", _launchOptions.ProcessId.Value);
            }
            else if (launchOptions.UnixPort.IsLinux())
            {
                absoluteExePath = string.Format(CultureInfo.InvariantCulture, @"shell readlink -f /proc/{0}/exe", _launchOptions.ProcessId.Value);
            }
            else
            {
                throw new LaunchErrorException(ResourceStrings.Error_UnsupportedPlatform);
            }

            Action<string> failureHandler = (string miError) =>
            {
                string message = string.Format(CultureInfo.CurrentCulture, ResourceStrings.Error_FailedToGetExePath, miError);
                throw new LaunchErrorException(message);
            };

            Func<string, Task> successHandler = async (string exePath) =>
            {
                string trimmedExePath = exePath.Trim();
                try
                {
                    // If the folder contains a space, we need to quote the path.
                    if (trimmedExePath.Contains(' '))
                    {
                        trimmedExePath = "\"" + trimmedExePath + "\"";
                    }

                    await CmdAsync("-file-exec-and-symbols " + trimmedExePath, ResultClass.done);
                }
                catch (UnexpectedMIResultException miException)
                {
                    string message = string.Format(CultureInfo.CurrentCulture, ResourceStrings.Error_ExePathInvalid, trimmedExePath, MICommandFactory.Name, miException.MIError);
                    throw new LaunchErrorException(message);
                }
            };

            commands.Add(new LaunchCommand(absoluteExePath, ignoreFailures: false, failureHandler: failureHandler, successHandler: successHandler));
        }

        private TargetArchitecture DefaultArch()
        {
            if (LaunchOptions.TargetArchitecture != TargetArchitecture.Unknown)
            {
                return LaunchOptions.TargetArchitecture;
            }
            else
            {
                // Use X64 as default if the arch couldn't be detected and wasn't specified
                // in the launch options
                WriteOutput(ResourceStrings.Warning_UsingDefaultArchitecture);
                return TargetArchitecture.X64;
            }
        }

        private void AddGetTargetArchitectureCommand(IList<LaunchCommand> commands)
        {
            // User may specify the wrong architecture, e.g. ARM instead of ARM64, so use the target's real architecture if available:
            // 1. if the command factory can discover the target architecture then use that
            // 2. else if the user specified an architecture then use that
            // 3. otherwise default to x64
            SetTargetArch(DefaultArch()); // set the default value based on user input

            Func<string, Task> successHandler = (string resultsStr) =>
            {
                var archFromTarget = MICommandFactory.ParseTargetArchitectureResult(resultsStr);

                if (archFromTarget != TargetArchitecture.Unknown)
                {
                    SetTargetArch(archFromTarget);
                }

                return Task.FromResult(0);
            };

            string cmd = MICommandFactory.GetTargetArchitectureCommand();

            if (cmd != null)
            {
                // schedule a command to fetch the the debuggers actual target achitecture 
                commands.Add(new LaunchCommand(cmd, ignoreFailures: true, successHandler: successHandler));
            }
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

        private async Task HandleBreakModeEvent(ResultEventArgs results, BreakRequest breakRequest)
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

            if (_childProcessHandler != null && await _childProcessHandler.Stopped(results.Results, tid))
            {
                return;
            }

            // Any existing variable objects at this point are from the last time we were in break mode, and are
            //  therefore invalid.  Dispose them so they're marked for cleanup.
            lock (this.ActiveVariables)
            {
                foreach (IVariableInformation varInfo in this.ActiveVariables)
                {
                    varInfo.Dispose();
                }
                this.ActiveVariables.Clear();
                ReturnValue = null; // already disposed above
            }

            ThreadCache.MarkDirty();
            MICommandFactory.DefineCurrentThread(tid);

            DebuggedThread thread = await ThreadCache.GetThread(tid);
            if (thread == null)
            {
                if (!this.IsStopDebuggingInProgress)
                {
                    Debug.Fail("Failed to find thread on break event.");
                    throw new Exception(String.Format(CultureInfo.CurrentCulture, ResourceStrings.MissingThreadBreakEvent, tid));
                }
                else
                {
                    // It's possible that the SIGINT was sent because GDB is trying to terminate a running debuggee and stop debugging
                    // See https://devdiv.visualstudio.com/DevDiv/VS%20Diag%20IntelliTrace/_workItems?_a=edit&id=236275&triage=true
                    // for a repro
                    return;
                }
            }

            await this.EnsureModulesLoaded();
            await ThreadCache.StackFrames(thread);  // prepopulate the break thread in the thread cache
            ThreadContext cxt = await ThreadCache.GetThreadContext(thread);

            if (cxt == null)
            {
                // Something went seriously wrong. For instance, this can happen when the primary thread
                // of an app exits on linux while background threads continue to run with pthread_exit on the main thread
                // See https://devdiv.visualstudio.com/DefaultCollection/DevDiv/VS%20Diag%20IntelliTrace/_workItems?_a=edit&id=197616&triage=true
                // for a repro
                Debug.Fail("Failed to find thread on break event.");
                throw new Exception(String.Format(CultureInfo.CurrentCulture, ResourceStrings.MissingThreadBreakEvent, tid));
            }

            ThreadCache.SendThreadEvents(this, null);   // make sure that new threads have been pushed to the UI

            // If didn't hit a breakpoints then delete all pending deletions on break mode
            // For breakpoint stops deletion will be handled below.
            if (reason != "breakpoint-hit")
            {
                await _breakpointManager.DeleteBreakpointsPendingDeletion();
            }

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

            if (String.IsNullOrWhiteSpace(reason) && !this.EntrypointHit)
            {
                breakRequest = BreakRequest.None;   // don't let stopping interfere with launch processing

                // MinGW sends a stopped event on attach. gdb<->gdbserver also sends a stopped event when first attached.
                // If this is a gdb<->gdbserver connection, ignore this as the entryPoint
                if (IsLocalLaunchUsingServer())
                {
                    // If the stopped event occurs on gdbserver, ignore it unless it contains a filename.
                    TupleValue frame = results.Results.TryFind<TupleValue>("frame");
                    if (frame.Contains("file"))
                    {
                        this.EntrypointHit = true;
                    }
                }
                else
                {
                    this.EntrypointHit = true;
                }

                CmdContinueAsync();
                FireDeviceAppLauncherResume();
            }
            else if (reason == "entry-point-hit")
            {
                this.EntrypointHit = true;
                await this.OnEntrypointHit();
                _callback.OnEntryPoint(thread);
            }
            else if (reason == "breakpoint-hit")
            {
                string bkptno = results.Results.FindString("bkptno");
                ulong addr = cxt.pc ?? 0;

                bool fContinue;
                TupleValue frame = results.Results.TryFind<TupleValue>("frame");
                AD7BoundBreakpoint[] bkpt = _breakpointManager.FindHitBreakpoints(bkptno, addr, frame, out fContinue);
                await _breakpointManager.DeleteBreakpointsPendingDeletion();

                if (bkpt != null)
                {
                    if (frame != null && addr != 0)
                    {
                        string sourceFile = frame.TryFindString("fullname");
                        if (!String.IsNullOrEmpty(sourceFile))
                        {
                            await this.VerifySourceFileTimestamp(addr, sourceFile);
                        }
                    }

                    if (!this.EntrypointHit)
                    {
                        // Hitting a bp before the entrypoint overrules entrypoint processing.
                        this.EntrypointHit = true;
                        await this.OnEntrypointHit();
                    }

                    List<object> bplist = new List<object>();
                    bplist.AddRange(bkpt);
                    _callback.OnBreakpoint(thread, bplist.AsReadOnly());
                }
                else if (!this.EntrypointHit)
                {
                    this.EntrypointHit = true;
                    await this.OnEntrypointHit();

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
                        CmdContinueAsyncConditional(breakRequest);
                    }
                    else
                    {
                        // not one of our breakpoints, so stop with a message
                        _callback.OnException(thread, "Unknown breakpoint", "", 0);
                    }
                }
            }
            else if (reason == "watchpoint-trigger")
            {
                var wpt = results.Results.Find("wpt");
                string bkptno = wpt.FindString("number");
                ulong addr = cxt.pc ?? 0;

                bool fContinue;
                AD7BoundBreakpoint bkpt = _breakpointManager.FindHitWatchpoint(bkptno, out fContinue);
                if (bkpt != null)
                {
                    List<object> bplist = new List<object>();
                    bplist.Add(bkpt);
                    _callback.OnBreakpoint(thread, bplist.AsReadOnly());
                }
                else
                {
                    if (fContinue)
                    {
                        //we hit a bp pending deletion
                        //post the CmdContinueAsync operation so it does not happen until we have deleted all the pending deletes
                        CmdContinueAsyncConditional(breakRequest);
                    }
                    else
                    {
                        // not one of our breakpoints, so stop with a message
                        _callback.OnException(thread, "Unknown watchpoint", "", 0);
                    }
                }
            }
            // step over/into
            // NB: unfortunately this event does not provide a return value: https://sourceware.org/bugzilla/show_bug.cgi?id=26354
            else if (reason == "end-stepping-range")
                _callback.OnStepComplete(thread);
            // step out
            else if (reason == "function-finished")
            {
                string resultVar = results.Results.TryFindString("gdb-result-var"); // a gdb value history var like "$1"
                if (!string.IsNullOrEmpty(resultVar))
                {
                    ReturnValue = new VariableInformation("$ReturnValue", resultVar, cxt, Engine, (AD7Thread)thread.Client, isParameter: false);
                    await ReturnValue.Eval();
                }
                _callback.OnStepComplete(thread);
            }
            else if (reason == "signal-received")
            {
                string name = results.Results.TryFindString("signal-name");
                if ((name == "SIG32") || (name == "SIG33"))
                {
                    // we are going to ignore these (Sigma) signals for now
                    CmdContinueAsyncConditional(breakRequest);
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
                    bool stoppedAtSIGSTOP = false;
                    if (sigName == "SIGSTOP" && _launchOptions.ProcessId.HasValue)
                    {
                        if (AD7Engine.RemoveChildProcess(_launchOptions.ProcessId.Value))
                        {
                            stoppedAtSIGSTOP = true;
                        }
                    }
                    string message = results.Results.TryFindString("signal-meaning");
                    if (stoppedAtSIGSTOP)
                    {
                        await MICommandFactory.Signal("SIGCONT");
                    }
                    else
                    {
                        _callback.OnException(thread, sigName, message, code);
                    }
                }
            }
            else if (reason == "exception-received")
            {
                string exceptionName = results.Results.TryFindString("exception-name");
                if (string.IsNullOrEmpty(exceptionName))
                    exceptionName = "Exception";

                string description = results.Results.FindString("exception");
                Guid? exceptionCategory;
                ExceptionBreakpointStates state;
                MICommandFactory.DecodeExceptionReceivedProperties(results.Results, out exceptionCategory, out state);

                _callback.OnException(thread, exceptionName, description, 0, exceptionCategory, state);
            }
            else
            {
                if (breakRequest == BreakRequest.None)
                {
                    Debug.Fail("Unknown stopping reason");
                    _callback.OnException(thread, "Unknown", "Unknown stopping event", 0);
                }
            }
            if (IsExternalBreakRequest(breakRequest))
            {
                _callback.OnStopComplete(thread);
            }
        }

        /// <summary>
        /// Tasks to run when the entry point is hit.
        /// </summary>
        private async Task OnEntrypointHit()
        {
            if (this.MICommandFactory.Mode == MIMode.Lldb)
            {
                // When the terminal window is closed, a SIGHUP is sent to lldb-mi and LLDB's default is to stop.
                // We want to not stop (break) when this happens and the SIGHUP to be sent to the debuggee process.
                // LLDB requires this command to be issued after the process has started.
                await ConsoleCmdAsync("process handle --pass true --stop false --notify false SIGHUP", allowWhileRunning: false, ignoreFailures: true);
            }

            if (this._deleteEntryPointBreakpoint && !String.IsNullOrWhiteSpace(this._entryPointBreakpoint))
            {
                // Try and delete the entrypoint breakpoint. We only try this once but in some cases this won't succeed
                await MICommandFactory.BreakDelete(this._entryPointBreakpoint, ResultClass.None);
                this._deleteEntryPointBreakpoint = false;
            }
        }

        private static bool IsExternalBreakRequest(BreakRequest breakRequest)
        {
            return breakRequest == BreakRequest.Async || breakRequest == BreakRequest.Stop;
        }

        private void CmdContinueAsyncConditional(BreakRequest request)
        {
            if (!IsExternalBreakRequest(request))
            {
                CmdContinueAsync();
            }
        }

        private async Task VerifySourceFileTimestamp(ulong addr, string sourceFilePath)
        {
            await this.EnsureModulesLoaded();

            string targetModulePath = this._launchOptions.ExePath;
            DebuggedModule targetModule = _moduleList.FirstOrDefault(m => m.AddressInModule(addr));
            if (targetModule != null)
            {
                targetModulePath = targetModule.Name;
            }

            Tuple<string, string> key = Tuple.Create(sourceFilePath, targetModulePath);
            if (_fileTimestampWarnings.Contains(key))
            {
                // We've already warned about this file
                return;
            }

            try
            {
                if (!File.Exists(sourceFilePath) || !File.Exists(targetModulePath))
                {
                    return;
                }

                DateTime sourceFileTimestamp = File.GetLastWriteTimeUtc(sourceFilePath);
                DateTime moduleFileTimestamp = File.GetLastWriteTimeUtc(targetModulePath);

                if (sourceFileTimestamp > moduleFileTimestamp)
                {
                    // Source file is newer than the module - warn the user
                    _fileTimestampWarnings.Add(key);

                    string message = String.Format(CultureInfo.CurrentCulture, ResourceStrings.Warning_SourceFileOutOfDate_Arg2, sourceFilePath, targetModulePath);
                    _callback.OnOutputMessage(new OutputMessage(message + Environment.NewLine, enum_MESSAGETYPE.MT_OUTPUTSTRING, OutputMessage.Severity.Warning));
                }
            }
            catch (IOException)
            {
                // Ignore exceptions related to getting file information
            }
        }

        internal WorkerThread WorkerThread
        {
            get { return _worker; }
        }

        /// <summary>
        /// Use to ensure path separators are correct for files that exist on the target debugger's machine.
        /// If you are debugging on Windows to a remote instance of gdb or gdbserver, it will update it to Unix path separators.
        /// </summary>
        internal string EnsureProperPathSeparators(string path)
        {
            if (this.UseUnixPathSeparators)
            {
                path = PlatformUtilities.WindowsPathToUnixPath(path);
            }
            else
            {
                path = path.Trim();
                path = path.Replace(@"\", @"\\");
            }

            if (path.IndexOfAny(new char[] { ' ', '\'' }) != -1)
            {
                path = '"' + path + '"';
            }
            return path;
        }

        /// <summary>
        /// This method should be used to escape paths that are used by GDB (and NOT gdbserver) locally. 
        /// Any path that gdbserver would use in remote server scenarios should use EnsureProperPathSeparators instead.
        /// </summary>
        internal string EscapeSymbolPath(string path, bool ignoreSpaces = false)
        {
            if (this.UseUnixSymbolPaths)
            {
                path = PlatformUtilities.WindowsPathToUnixPath(path);
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

        /// <summary>
        /// Check to see when to use Unix Path separators. Will check if we're doing a local Windows Launch. If it is, then
        /// verify if a) we're doing a device launch and b) if MIDebuggerServerAddress is used. Device launch needs Windows path separators, 
        /// but when gdbserver is used on Linux, then we need Unix separators.
        /// </summary>
        internal bool UseUnixPathSeparators
        {
            get
            {
                if (PlatformUtilities.IsWindows() && _launchOptions is LocalLaunchOptions)
                {
                    // If not a device launch (Android) and MIDebuggerServerAddress is specified, then we also need to use Unix symbol paths
                    return _launchOptions.DeviceAppLauncher == null &&
                        !String.IsNullOrWhiteSpace(((LocalLaunchOptions)_launchOptions).MIDebuggerServerAddress);
                }

                return true;
            }
        }

        internal bool UseUnixSymbolPaths { get { return _launchOptions.UseUnixSymbolPaths; } }

        internal void LoadSymbols(DebuggedModule module)
        {
            if (MICommandFactory.Mode == MIMode.Gdb)
            {
                if (!module.SymbolsLoaded && !string.IsNullOrWhiteSpace(module.SymbolPath))
                {
                    Task evalTask = Task.Run(async () =>
                    {
                        await LoadSymbols(GetFileName(module.Name));
                        await CheckModules();
                    });
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private async Task<string> LoadSymbols(string filename)
        {
            return await ConsoleCmdAsync("sharedlibrary " + filename, allowWhileRunning: false);
        }

        private async Task CheckModules()
        {
            // NOTE: The version of GDB that comes in the Android SDK doesn't support -file-list-shared-library
            // so we need to use the console command
            //string results = await MICommandFactory.GetSharedLibrary();
            string results = await ConsoleCmdAsync("info sharedlibrary", allowWhileRunning: false);

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
                    if (line.StartsWith("From", StringComparison.Ordinal)) // header line, ignore
                    {
                        continue;
                    }
                    else if (line.StartsWith("0x", StringComparison.Ordinal))  // module with load address
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
                    if (line.StartsWith("Yes", StringComparison.Ordinal))
                    {
                        symbolsLoaded = true;
                        line = line.Substring(3);
                    }
                    else if (line.StartsWith("No", StringComparison.Ordinal))
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
                    AddModule(line, line, startAddr, endAddr - startAddr, symbolsLoaded, line);
                }
            }
        }

        private DebuggedModule AddModule(string id, string name, ulong baseAddr, ulong size, bool symbolsLoaded, string symPath)
        {
            var module = FindModule(id);
            if (module == null)
            {
                module = new DebuggedModule(id, name, baseAddr, size, symbolsLoaded, symPath, _loadOrder++);
                lock (_moduleList)
                {
                    // Temporary kludge to work around VS asking user for source when __sanitizer::Die breakpoint is hit.
                    // Will be removed when asan implementation switches to using the unhandled exception dialog.
                    if (id.Contains("libasan.so"))
                    {
                        module.IgnoreSource = true;
                    }
                    _moduleList.Add(module);
                }

                _callback.OnModuleLoad(module);
            }
            else if (!module.SymbolsLoaded && symbolsLoaded)
            {
                module.SymbolsLoaded = true;
                _callback.OnSymbolsLoaded(module);
            }
            return module;
        }

        // this is called on any thread, so we need to dispatch the command via
        // the Worker thread, to end up in DispatchCommand
        protected override void ScheduleStdOutProcessing(string line)
        {
            _worker.PostOperation(() =>
            {
                // Docker connections buffer the text so we need to split it up into individual lines.
                char[] newLineCharSeparator = new char[] { '\n' };
                string[] parsedLines = line.Split(newLineCharSeparator, StringSplitOptions.RemoveEmptyEntries);
                foreach( var item in parsedLines)
                {
                    ProcessStdOutLine(item);
                }                
            });
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
                switch (kind)
                {
                    case enum_STEPKIND.STEP_INTO:
                        await MICommandFactory.ExecStepInstruction(threadId);
                        break;
                    case enum_STEPKIND.STEP_OVER:
                        await MICommandFactory.ExecNextInstruction(threadId);
                        break;
                    case enum_STEPKIND.STEP_OUT:
                        await MICommandFactory.ExecFinish(threadId);
                        break;
                    default:
                        throw new NotImplementedException();
                }
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
                if (MICommandFactory.SupportsStopOnDynamicLibLoad())
                {
                    await EnsureModulesLoaded();
                }

                await HandleBreakModeEvent(_initialBreakArgs, BreakRequest.None);
                _initialBreakArgs = null;
            }
            else if (this.IsCoreDump)
            {
                // Set initial state of debug engine to stopped with emulated results
                this.OnStateChanged("stopped", await this.GenerateStoppedRecordResults());
            }
            else
            {
                bool attach = _launchOptions.ProcessId.HasValue;

                if (!attach)
                {
                    this.SourceLineCache.Clear();

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
            // Collect the file, fullname, and line fields if they are available. They may be missing if the frame is for
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

        public void Detach()
        {
            // Special casing sending the fake stopped event for lldb. 
            // GDB prints out thread group exit events on mi command "-target-detach" which is handed by method HandleThreadGroupExited
            // GDB or the debuggee can terminate and those are handled by Terminate and TerminateProcess methods.
            if (MICommandFactory.Mode == MIMode.Lldb)
            {
                ScheduleStdOutProcessing(@"*stopped,reason=""disconnected""");
            }
        }

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

        public DebuggedModule FindModule(ulong addr)
        {
            lock (_moduleList)
            {
                return _moduleList.Find((m) => m.AddressInModule(addr));
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

            ValueListValue localsAndParameters = await MICommandFactory.StackListVariables(PrintValue.NoValues, thread.Id, ctx.Level);

            foreach (var localOrParamResult in localsAndParameters.Content)
            {
                string name = localOrParamResult.FindString("name");
                bool isParam = localOrParamResult.TryFindString("arg") == "1";
                SimpleVariableInformation simpleInfo = new SimpleVariableInformation(name, isParam);
                VariableInformation vi = await simpleInfo.CreateMIDebuggerVariable(ctx, Engine, thread);
                variables.Add(vi);
            }

            if (ReturnValue != null && ctx.Level == 0 && ReturnValue.Client.Id == thread.Id)
                variables.Add(ReturnValue);

            return variables;
        }

        //This method gets the value/type info for the method parameters without creating an MI debugger variable for them. For use in the callstack window
        //NOTE: eval is not called
        public async Task<List<SimpleVariableInformation>> GetParameterInfoOnly(AD7Thread thread, ThreadContext ctx)
        {
            List<SimpleVariableInformation> parameters = new List<SimpleVariableInformation>();

            ValueListValue localAndParameters = await MICommandFactory.StackListVariables(PrintValue.SimpleValues, thread.Id, ctx.Level);

            foreach (var results in localAndParameters.Content.Where(r => r.TryFindString("arg") == "1"))
            {
                parameters.Add(new SimpleVariableInformation(results.FindString("name"), /*isParam*/ true, results.FindString("value"), results.FindString("type")));
            }

            return parameters;
        }

        //This method gets the value/type info for the method parameters of all frames without creating an mi debugger variable for them. For use in the callstack window
        //NOTE: eval is not called
        public async Task<List<ArgumentList>> GetParameterInfoOnly(AD7Thread thread, bool values, bool types, uint low, uint high)
        {
            // If values are requested, request simple values, otherwise we'll use -var-create to get the type of argument it is.
            var frames = await MICommandFactory.StackListArguments(values ? PrintValue.SimpleValues : PrintValue.NoValues, thread.Id, low, high);
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
                            // If the types of the arguments are requested, get that from a call to -var-create
                            string typeString = null;
                            if (types)
                            {
                                Debug.Assert(!values, "GetParameterInfoOnly should not reach here if values is true");
                                Results results = await MICommandFactory.VarCreate(n, thread.Id, (uint)level, 0, ResultClass.None);
                                // Only get the type if the result is "done"
                                if (results.ResultClass == ResultClass.done)
                                {
                                    typeString = results.TryFindString("type");

                                    string varName = results.TryFindString("name");
                                    if (!String.IsNullOrWhiteSpace(varName))
                                    {
                                        // Remove the variable we created as we don't track it.
                                        await MICommandFactory.VarDelete(varName);
                                    }
                                }
                            }

                            args.Add(new SimpleVariableInformation(n, /*isParam*/ true, /*value*/null, String.IsNullOrWhiteSpace(typeString) ? null : typeString));
                        }
                    }
                }
                parameters.Add(new ArgumentList(level, args));
            }
            return parameters;
        }

        internal async Task<uint> ReadProcessMemory(ulong address, uint count, byte[] bytes)
        {
            string cmd = "-data-read-memory-bytes " + EngineUtils.AsAddr(address, Is64BitArch) + " " + count.ToString(CultureInfo.InvariantCulture);
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

        internal async Task<Tuple<ulong, ulong>> FindValidMemoryRange(ulong address, uint count, int offset)
        {
            var ret = new Tuple<ulong, ulong>(0, 0);    // init to an empty range
            string cmd = String.Format(CultureInfo.InvariantCulture, "-data-read-memory-bytes -o {0} {1} {2}", offset.ToString(CultureInfo.InvariantCulture), EngineUtils.AsAddr(address, Is64BitArch), count.ToString(CultureInfo.InvariantCulture));
            Results results = await CmdAsync(cmd, ResultClass.None);
            if (results.ResultClass == ResultClass.error)
            {
                return ret;
            }
            ValueListValue mem = results.Find<ValueListValue>("memory");
            if (mem.IsEmpty())
            {
                return ret;
            }
            TupleValue res = mem.Content[0] as TupleValue;
            if (res == null)
            {
                return ret;
            }
            ulong start = res.FindAddr("begin");
            ulong end = res.FindAddr("end");
            return new Tuple<ulong, ulong>(start, end);
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

        /// <summary>
        /// Finds the line associated with a start address.
        /// </summary>
        public async Task<uint> LineForStartAddress(string file, ulong startAddress)
        {
            List<ulong> addresses = new List<ulong>();
            SourceLineMap srcLines = await SourceLineCache.GetLinesForFile(file);
            if (srcLines == null || srcLines.Count == 0)
            {
                srcLines = await SourceLineCache.GetLinesForFile(System.IO.Path.GetFileName(file));
            }
            if (srcLines == null || srcLines.Count == 0)
            {
                return 0;
            }

            SourceLine srcLine;
            if (srcLines.TryGetValue(startAddress, out srcLine))
            {
                return srcLine.Line;
            }
            return 0;
        }

        public async Task<List<ulong>> StartAddressesForLine(string file, uint line)
        {
            List<ulong> addresses = new List<ulong>();
            string compFile;
            SourceLineMap srcLines;
            if (MapCurrentSrcToCompileTimeSrc(file, out compFile))  // found a remote mapping for this source file
            {
                file = compFile;
                srcLines = await SourceLineCache.GetLinesForFile(file);
            }
            else
            {
                srcLines = await SourceLineCache.GetLinesForFile(file);
                if (srcLines == null || srcLines.Count == 0)
                {
                    srcLines = await SourceLineCache.GetLinesForFile(System.IO.Path.GetFileName(file));
                }
            }
            if (srcLines != null && srcLines.Count > 0)
            {
                bool gotoNextFunc = false;
                foreach (KeyValuePair<ulong, SourceLine> l in srcLines)
                {
                    if (gotoNextFunc)
                    {
                        if (l.Value.Line == 0)
                        {
                            gotoNextFunc = false;
                        }
                    }
                    else if (line == l.Value.Line)
                    {
                        addresses.Add(l.Value.AddrStart);
                        gotoNextFunc = true;
                    }
                }
            }
            if (addresses.Count == 0)
            {
                // ask the underlying debugger for the line info
                addresses = await MICommandFactory.StartAddressesForLine(file, line);
            }
            return addresses;
        }

        /// <summary>
        /// The clear is done by sending reset string (ESC, c) to terminal STDERR
        /// </summary>
        /// <returns></returns>
        private Task<string> ResetConsole()
        {
            return ConsoleCmdAsync(@"shell echo -e \\033c 1>&2", allowWhileRunning: false);
        }

        public bool IsChildProcessDebugging => _childProcessHandler != null;

        public bool MapCurrentSrcToCompileTimeSrc(string currentSrc, out string compilerSrc)
        {
            if (_launchOptions.SourceMap != null)
            {
                StringComparison comp = PlatformUtilities.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                foreach (var e in _launchOptions.SourceMap)
                {
                    if (e.UseForBreakpoints && currentSrc.StartsWith(e.EditorPath, comp))
                    {
                        var file = currentSrc.Substring(e.EditorPath.Length);
                        if (string.IsNullOrEmpty(file)) // matched the whole string
                        {
                            compilerSrc = e.CompileTimePath;  // return the matches compile time path
                            return true;
                        }
                        // must do the path break at a directory boundry, i.e. at a '\' or '/' char
                        char firstFilechar = file[0];
                        char lastDirectoryChar = e.EditorPath[e.EditorPath.Length - 1];
                        if (firstFilechar == Path.DirectorySeparatorChar || firstFilechar == Path.AltDirectorySeparatorChar)
                        {
                            file = file.Trim(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });   // Trim the directory separator(s)
                        }
                        else if (lastDirectoryChar != Path.DirectorySeparatorChar && lastDirectoryChar != Path.AltDirectorySeparatorChar)
                        {
                            continue;   // match didn't end at a directory separator, not actually a match
                        }
                        compilerSrc = Path.Combine(e.CompileTimePath, file);    // map to the compiled location
                        if (compilerSrc.IndexOf('\\') > 0)
                        {
                            compilerSrc = PlatformUtilities.WindowsPathToUnixPath(compilerSrc); // use Unix notation for the compiled path
                        }
                        return true;
                    }
                }
            }
            compilerSrc = currentSrc;
            return false;
        }

        public bool MapCompileTimeSrcToCurrentSrc(string compilerSrc, out string currentName)
        {
            if (_launchOptions.SourceMap != null)
            {
                // Convert to Client source paths
                string hostOSCompilerSrc = PlatformUtilities.PathToHostOSPath(compilerSrc);

                StringComparison comp = _launchOptions.UseUnixSymbolPaths ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                foreach (var e in _launchOptions.SourceMap)
                {
                    if (string.IsNullOrEmpty(e.CompileTimePath))
                    {
                        continue;   // don't try to map back if path has an empty compiler src tree
                    }
                    if (hostOSCompilerSrc.StartsWith(e.CompileTimePath, comp))
                    {
                        var file = hostOSCompilerSrc.Substring(e.CompileTimePath.Length);
                        if (string.IsNullOrEmpty(file)) // matched the whole string
                        {
                            if (hostOSCompilerSrc.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) || hostOSCompilerSrc.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                            {
                                break;  // directory matched, use default.
                            }
                            else
                            {
                                // Is a file
                                currentName = e.EditorPath;  // return the matches compile time path
                                return true;
                            }
                        }
                        // must do the path break at a directory boundry, i.e. at a '\' or '/' char
                        char firstFilechar = file[0];
                        char lastDirectoryChar = e.CompileTimePath[e.CompileTimePath.Length - 1];
                        if (file[0] == Path.DirectorySeparatorChar || file[0] == Path.AltDirectorySeparatorChar)
                        {
                            file = file.Trim(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });   // Trim the directory separator(s)
                        }
                        else if (lastDirectoryChar != Path.DirectorySeparatorChar && lastDirectoryChar != Path.AltDirectorySeparatorChar)
                        {
                            continue;   // match didn't end at a directory separator, not actually a match
                        }
                        currentName = Path.Combine(e.EditorPath, file);    // map to the compiled location
                        return true;
                    }
                }
            }
            currentName = compilerSrc;
            return false;
        }

        public string GetMappedFileFromTuple(TupleValue tuple)
        {
            string file = tuple.Contains("fullname") ? tuple.FindString("fullname") : tuple.TryFindString("file");
            string currentName = string.Empty;
            if (!string.IsNullOrEmpty(file))
            {
                MapCompileTimeSrcToCurrentSrc(file, out currentName);
            }
            return currentName;
        }
    }
}
