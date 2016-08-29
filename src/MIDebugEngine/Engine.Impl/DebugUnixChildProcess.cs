// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MICore;
using System.Diagnostics;
using Microsoft.DebugEngineHost;
using System.Globalization;

namespace Microsoft.MIDebugEngine
{
    public interface ProcessSequence
    {
        Task Enable();
        /// <summary>
        /// Handle a stopping event as part of a sequence of debugger operations
        /// </summary>
        /// <param name="debugEvent"></param>
        /// <param name="tid"></param>
        /// <returns>true if stopping event was consumed (process must be running), false otherwise (indicting the debugger should process the event)</returns>
        Task<bool> Stopped(Results debugEvent, int tid);
        void ThreadCreatedEvent(Results results);
    }

    internal class DebugUnixChild : ProcessSequence
    {
        private enum State
        {
            AtFork,
            AtVfork,
            AtSignal,
            AtExec,
            Complete
        }
        private DebuggedProcess _process;
        private LaunchOptions _launchOptions;
        private string _mainBreak;

        private class ThreadProgress
        {
            public State State;
            public int Newpid;
            public int Newtid;
            public string Exe;
        }
        private Dictionary<int, ThreadProgress> _threadStates;

        public DebugUnixChild(DebuggedProcess process, LaunchOptions launchOptions)
        {
            _process = process;
            _threadStates = new Dictionary<int, ThreadProgress>();
            _launchOptions = launchOptions;
        }

        private async Task ProcessChild(ThreadProgress state)
        {
            Debug.Assert(state.Newpid != 0, "Child process id not found.");
            if (state.Newpid != 0)
            {
                uint inf = _process.InferiorByPid(state.Newpid);
                if (inf != 0)
                {
                    await _process.ConsoleCmdAsync("inferior " + inf.ToString());
                    if (!string.IsNullOrEmpty(_mainBreak))
                    {
                        await _process.MICommandFactory.BreakDelete(_mainBreak);
                        _mainBreak = null;
                    }
                    state.State = State.AtSignal;
                    await _process.MICommandFactory.Signal("SIGSTOP");  // stop the child
                }
            }
        }

        private async Task RunChildToMain(ThreadProgress state)
        {
            Debug.Assert(state.Newpid != 0, "Child process id not found.");
            if (state.Newpid != 0)
            {
                uint inf = _process.InferiorByPid(state.Newpid);
                if (inf != 0)
                {
                    await _process.ConsoleCmdAsync("inferior " + inf.ToString());
                    await SetBreakAtMain();
                    state.State = State.AtExec;
                    await _process.MICommandFactory.ExecContinue();  // run the child
                }
            }
        }

        private async Task ContinueTheChild(ThreadProgress state)
        {
            Debug.Assert(state.State == State.AtExec, "wrong vfork processing state");
            if (state.Newpid != 0)
            {
                uint inf = _process.InferiorByPid(state.Newpid);
                if (inf != 0)
                {
                    await _process.ConsoleCmdAsync("inferior " + inf.ToString());
                    await _process.MICommandFactory.ExecContinue();  // run the child
                }
            }
        }

        private async Task<bool> DetachAndContinue(ThreadProgress state)
        {
            await DetachFromChild(state);
            AD7Engine.AddChildProcess(state.Newpid);
            string engineName;
            Guid engineGuid;
            _process.Engine.GetEngineInfo(out engineName, out engineGuid);
            _launchOptions.BaseOptions.ProcessId = state.Newpid;
            _launchOptions.BaseOptions.ProcessIdSpecified = true;
            _launchOptions.BaseOptions.ExePath = state.Exe ?? _launchOptions.ExePath;
            HostDebugger.StartDebugChildProcess(_launchOptions.BaseOptions.ExePath, _launchOptions.GetOptionsString(), engineGuid);
            await _process.MICommandFactory.ExecContinue();     // continue the parent
            return true;   // parent is running
        }

        public async Task Enable()
        {
            await _process.MICommandFactory.SetOption("detach-on-fork", "off");
            await _process.MICommandFactory.Catch("fork");
            await _process.MICommandFactory.Catch("vfork");
        }

        public async Task SetBreakAtMain()
        {
            Results results = await _process.MICommandFactory.BreakInsert("main", condition: null, enabled: true);
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

        private async Task<bool> DetachFromChild(ThreadProgress state)
        {
            uint inf = _process.InferiorByPid(state.Newpid);
            if (inf == 0)
                return false;    // cannot process the child
            await _process.ConsoleCmdAsync("inferior " + inf.ToString());
            await _process.MICommandFactory.TargetDetach();     // detach from the child
            await _process.ConsoleCmdAsync("inferior 1");
            return true;
        }

        public void ThreadCreatedEvent(Results evnt)
        {
            int tid = evnt.FindInt("id");
            string groupId = evnt.FindString("group-id");
            int pid = _process.PidByInferior(groupId);
            foreach (var p in _threadStates)
            {
                if (p.Value.Newpid == pid)
                {
                    p.Value.Newtid = tid;
                }
            }
        }

        private ThreadProgress StateFromTid(int tid)
        {
            if (_threadStates.ContainsKey(tid))
            {
                return _threadStates[tid];
            }
            foreach (var p in _threadStates)
            {
                if (p.Value.Newtid == tid)
                {
                    return p.Value;
                }
            }
            return null;
        }

        public async Task<bool> Stopped(Results results, int tid)
        {
            string reason = results.TryFindString("reason");
            ThreadProgress s = StateFromTid(tid);

            if (reason == "fork")
            {
                s = new ThreadProgress();
                s.State = State.AtFork;
                s.Newpid = results.FindInt("newpid");
                _threadStates[tid] = s;
                await _process.Step(tid, VisualStudio.Debugger.Interop.enum_STEPKIND.STEP_OUT, VisualStudio.Debugger.Interop.enum_STEPUNIT.STEP_LINE);
                return true;
            }
            else if (reason == "vfork")
            {
                s = new ThreadProgress();
                s.State = State.AtVfork;
                s.Newpid = results.FindInt("newpid");
                _threadStates[tid] = s;
                await _process.MICommandFactory.SetOption("schedule-multiple", "on");
                await _process.MICommandFactory.Catch("exec", onlyOnce: true);
                var thread = await _process.ThreadCache.GetThread(tid);
                await _process.Continue(thread);
                return true;
            }

            if (s == null)
            {
                return false;   // no activity being tracked on this thread
            }

            switch (s.State)
            {
                case State.AtFork:
                    await ProcessChild(s);
                    break;
                case State.AtVfork:
                    await _process.MICommandFactory.SetOption("schedule-multiple", "off");
                    if ("exec" == results.TryFindString("reason"))
                    {
                        // The process doesn't handle the SIGSTOP correctly (just ignores it) when the process is at the start of program 
                        // (after exec). Let it run some code so that it will correctly respond to the SIGSTOP.
                        s.Exe = results.TryFindString("new-exec");
                        await RunChildToMain(s);
                    }
                    else
                    {
                        // sometimes gdb misses the breakpoint at exec and execution will proceed to a breakpoint in the child
                        _process.Logger.WriteLine("Missed catching the exec after vfork. Spawning the child's debugger.");
                        s.State = State.AtExec;
                        goto missedExec;
                    }
                    break;
                case State.AtSignal:    // both child and parent are stopped
                    s.State = State.Complete;
                    return await DetachAndContinue(s);
                case State.AtExec:
                missedExec:
                    if (tid == s.Newtid)    // stopped in the child
                    {
                        await ProcessChild(s);
                    }
                    else // sometime the parent will get a spurious signal before the child hits main
                    {
                        await ContinueTheChild(s);
                    }
                    break;
                case State.Complete:
                    _threadStates.Remove(tid);
                    if (reason == "signal-received" && results.TryFindString("signal-name") == "SIGSTOP")
                    {
                        // SIGSTOP was propagated to the parent
                        await _process.MICommandFactory.Signal("SIGCONT");
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
            return true;
        }
    }
}