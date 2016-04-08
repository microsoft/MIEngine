using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MICore;
using System.Diagnostics;
using Microsoft.DebugEngineHost;

namespace Microsoft.MIDebugEngine
{
    public interface ProcessSequence
    {
        Task Enable(bool enable);
        /// <summary>
        /// Handle a stopping event as part of a sequence of debugger operations
        /// </summary>
        /// <param name="debugEvent"></param>
        /// <returns>true if stopping event was consumed (process must be running), false otherwise (indicting the debugger should process the event)</returns>
        Task<bool> Stopped(Results debugEvent);
        /// <summary>
        /// Handle breakpoint created events
        /// </summary>
        /// <param name="debugEvent"></param>
        /// <returns>true if event was consumed, false otherwise (indicting the debugger should process the event)</returns>
        bool BreakpointCreated(Results debugEvent);
    }

    class DebugUnixChild : ProcessSequence
    {
        private string _forkBp;
        private string _vforkBp;

        private enum State
        {
            Init,
            Enabled,
            AtFork,
            AtVfork,
            AtSignal,
            AtExec
        }
        private State _state;
        private DebuggedProcess _process;
        private int _newpid;
        private LaunchOptions _launchOptions;
        private string _mainBreak;

        public DebugUnixChild(DebuggedProcess process, LaunchOptions launchOptions)
        {
            _process = process;
            _state = State.Init;
            _launchOptions = launchOptions;
        }

        private async Task ProcessChild()
        {
            Debug.Assert(_newpid != 0, "Child process id not found.");
            if (_newpid != 0)
            {
                uint inf = _process.InferiorByPid(_newpid);
                if (inf != 0)
                {
                    await _process.ConsoleCmdAsync("inferior " + inf.ToString());
                    if (!string.IsNullOrEmpty(_mainBreak))
                    {
                        await _process.MICommandFactory.BreakDelete(_mainBreak);
                        _mainBreak = null;
                    }
                    _state = State.AtSignal;
                    await _process.MICommandFactory.Signal("SIGSTOP");  // stop the child
                }
            }
        }

        private async Task RunChildToMain()
        {
            Debug.Assert(_newpid != 0, "Child process id not found.");
            if (_newpid != 0)
            {
                uint inf = _process.InferiorByPid(_newpid);
                if (inf != 0)
                {
                    await _process.ConsoleCmdAsync("inferior " + inf.ToString());
                    await SetBreakAtMain();
                    _state = State.AtExec;
                    await _process.MICommandFactory.ExecContinue();  // run the child
                }
            }
        }

        private async Task ContinueTheChild()
        {
            Debug.Assert(_state == State.AtExec, "wrong vfork processing state");
            if (_newpid != 0)
            {
                uint inf = _process.InferiorByPid(_newpid);
                if (inf != 0)
                {
                    await _process.ConsoleCmdAsync("inferior " + inf.ToString());
                    await _process.MICommandFactory.ExecContinue();  // run the child
                }
            }
        }

        private async Task<bool> DetachAndContinue()
        {
            uint inf = _process.InferiorByPid(_newpid);
            if (inf == 0)
                return false;    // cannot process the child
            await _process.ConsoleCmdAsync("inferior " + inf.ToString());
            await _process.MICommandFactory.TargetDetach();     // detach from the child
            await _process.ConsoleCmdAsync("inferior 1");
            lock(AD7Engine.ChildProcessLaunch)
            {
                AD7Engine.ChildProcessLaunch.Add(_newpid);
            }
            string engineName;
            Guid engineGuid;
            _process.Engine.GetEngineInfo(out engineName, out engineGuid);
            _launchOptions.BaseOptions.ProcessId = _newpid;
            _launchOptions.BaseOptions.ProcessIdSpecified = true;
            _state = State.Enabled;
            HostDebugger.Attach(_launchOptions.ExePath, LaunchOptions.GetOptionsString(_launchOptions.BaseOptions), engineGuid);
            await _process.MICommandFactory.ExecContinue();     // continue the parent
            return true;   // parent is running
        }

        public async Task Enable(bool enable)
        {
            if (enable && _state == State.Init)
            {
                await _process.MICommandFactory.SetOption("detach-on-fork", "off");
                await _process.MICommandFactory.Catch("fork");
                await _process.MICommandFactory.Catch("vfork");
                _state = State.Enabled;
                _newpid = 0;
            }
            else if (!enable)
            {
                if (_forkBp != null)
                {
                    await _process.MICommandFactory.BreakDelete(_forkBp);
                    _forkBp = null;
                }
                if (_vforkBp != null)
                {
                    await _process.MICommandFactory.BreakDelete(_vforkBp);
                    _vforkBp = null;
                }
                _state = State.Init;
            }
        }

        public async Task SetBreakAtMain()
        {
            Results results = await _process.MICommandFactory.BreakInsert("main", null);
            var bkpt = results.Find("bkpt");
            if (bkpt is ValueListValue)
            {
                _mainBreak = ((ValueListValue)bkpt).Content[0].FindString("number");
            }
            else
            {
                _mainBreak = bkpt.TryFindString("number");
            }
        }

        public async Task<bool> Stopped(Results results)
        {
            string reason = null;

            switch (_state)
            {
                case State.Enabled:
                    reason = results.TryFindString("reason");
                    if (reason == "fork")
                    {
                        int threadId = results.FindInt("thread-id");
                        _newpid = results.FindInt("newpid");
                        _state = State.AtFork;
                        await _process.Step(threadId, VisualStudio.Debugger.Interop.enum_STEPKIND.STEP_OUT, VisualStudio.Debugger.Interop.enum_STEPUNIT.STEP_LINE);
                        break;
                    }
                    else if (reason == "vfork")
                    {
                        int threadId = results.FindInt("thread-id");
                        _newpid = results.FindInt("newpid");
                        await _process.MICommandFactory.SetOption("schedule-multiple", "on");
                        await _process.MICommandFactory.Catch("exec", onlyOnce: true);
                        var thread = await _process.ThreadCache.GetThread(threadId);
                        _state = State.AtVfork;
                        await _process.Continue(thread);
                        break;
                    }
                    return false;
                case State.AtFork:
                    await ProcessChild();
                    break;
                case State.AtVfork:
                    if ("exec" == results.TryFindString("reason"))
                    {
                        await _process.MICommandFactory.SetOption("schedule-multiple", "off");
                        await RunChildToMain();
                    }
                    else
                    {
                        // sometimes gdb misses the breakpoint at exec and execution will proceed to a breakpoint in the child
                        _process.Logger.WriteLine("Missed catching the exec after vfork. Spawning the child's debugger.");
                        _state = State.AtExec;
                        goto missedExec;
                    }
                    break;
                case State.AtSignal:    // both child and parent are stopped
                    return await DetachAndContinue();
                case State.AtExec:
   missedExec:
                    if (results.TryFindString("reason") == "breakpoint-hit")
                    {
                        await ProcessChild();
                    }
                    else // sometime the parent will get a spurious signal before the child hits main
                    {
                        await ContinueTheChild();
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }

        public bool BreakpointCreated(Results debugEvent)
        {
            if (debugEvent.Contains("bkpt"))
            {
                ResultValue b = debugEvent.Find("bkpt");
                if (b is TupleValue)
                {
                    TupleValue bkpt = b as TupleValue;
                    string type = bkpt.TryFindString("type");
                    string ctype = bkpt.TryFindString("catch-type");
                    string num = bkpt.TryFindString("number");
                    if (type == "catch" && ctype == "fork")
                    {
                        _forkBp = num;
                        return true;
                    }
                    else if (type == "catch" && ctype == "vfork")
                    {
                        _vforkBp = num;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}