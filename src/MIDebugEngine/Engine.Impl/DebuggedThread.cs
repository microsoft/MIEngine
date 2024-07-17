// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using Microsoft.DebugEngineHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.MIDebugEngine
{
    public class DebuggedThread
    {
        public DebuggedThread(int id, MIDebugEngine.AD7Engine engine)
        {
            Id = id;
            Name = "";
            TargetId = id.ToString(CultureInfo.InvariantCulture);
            AD7Thread ad7Thread = new MIDebugEngine.AD7Thread(engine, this);
            Client = ad7Thread;
            ChildThread = false;
        }

        public int Id { get; private set; }
        public string TargetId { get; set; }
        public Object Client { get; private set; }      // really AD7Thread
        public bool Alive { get; set; }
        public bool Default { get; set; }
        public string Name { get; set; }
        public bool ChildThread { get; set; }       // transient child thread, don't inform UI of this thread
    }

    internal class ThreadCache
    {
        private List<DebuggedThread> _threadList;
        private Dictionary<int, List<ThreadContext>> _stackFrames;
        private Dictionary<int, ThreadContext> _topContext;    // can retrieve the top frame without walking the stack
        private bool _stateChange;             // indicates that a thread has been created/destroyed since last thread-info
        private bool _full;                    // indicates whether the cache has already been filled via -thread-info
        private ISampleEngineCallback _callback;
        private DebuggedProcess _debugger;
        private List<DebuggedThread> _deadThreads;
        private List<DebuggedThread> _newThreads;
        private Dictionary<string, List<int>> _threadGroups;
        private static uint s_targetId = uint.MaxValue;
        private const string c_defaultGroupId = "i1";  // gdb's default group id, also used for any process without group ids

        private List<DebuggedThread> DeadThreads
        {
            get
            {
                if (_deadThreads == null)
                {
                    _deadThreads = new List<DebuggedThread>();
                }
                return _deadThreads;
            }
        }

        private List<DebuggedThread> NewThreads
        {
            get
            {
                if (_newThreads == null)
                {
                    _newThreads = new List<DebuggedThread>();
                }
                return _newThreads;
            }
        }

        internal ThreadCache(ISampleEngineCallback callback, DebuggedProcess debugger)
        {
            _threadList = new List<DebuggedThread>();
            _stackFrames = new Dictionary<int, List<ThreadContext>>();
            _topContext = new Dictionary<int, ThreadContext>();
            _threadGroups = new Dictionary<string, List<int>>();
            _threadGroups[c_defaultGroupId] = new List<int>();  // initialize the processes thread group
            _stateChange = true;
            _callback = callback;
            _debugger = debugger;
            _full = false;
            debugger.RunModeEvent += SendThreadEvents;
        }

        internal async Task<DebuggedThread[]> GetThreads()
        {
            bool stateChange = false;
            lock (_threadList)
            {
                stateChange = _stateChange;
            }

            if (stateChange)
            {
                await CollectThreadsInfo(0);
            }

            lock (_threadList)
            {
                return _threadList.ToArray();
            }
        }

        internal async Task<DebuggedThread> GetThread(int id)
        {
            DebuggedThread[] threads = await GetThreads();
            foreach (var t in threads)
            {
                if (t.Id == id)
                {
                    return t;
                }
            }
            return null;
        }

        internal async Task<List<ThreadContext>> StackFrames(DebuggedThread thread)
        {
            lock (_threadList)
            {
                if (!_threadList.Contains(thread))
                {
                    return null;    // thread must be dead
                }
                if (_stackFrames.ContainsKey(thread.Id))
                {
                    return _stackFrames[thread.Id];
                }
            }
            List<ThreadContext> stack = null;
            try
            {
                stack = await WalkStack(thread);
            }
            catch (UnexpectedMIResultException)
            {
                _debugger.Logger.WriteLine(LogLevel.Error, "Stack walk failed on thread: " + thread.TargetId);
                _stateChange = true;   // thread may have been deleted. Force a resync
            }
            lock (_threadList)
            {
                _stackFrames[thread.Id] = stack;
                _topContext[thread.Id] = (stack != null && stack.Count > 0) ? stack[0] : null;
                return _stackFrames[thread.Id];
            }
        }

        internal async Task<ThreadContext> GetThreadContext(DebuggedThread thread)
        {
            if (thread == null)
                return null;

            lock (_threadList)
            {
                if (_topContext.ContainsKey(thread.Id))
                {
                    return _topContext[thread.Id];
                }
                if (_full)
                {
                    return null;    // no context available for this thread
                }
            }

            return await CollectThreadsInfo(thread.Id);
        }

        internal void MarkDirty()
        {
            lock (_threadList)
            {
                _topContext.Clear();
                _stackFrames.Clear();
                _full = false;
            }
        }

        internal async Task ThreadCreatedEvent(int id, string groupId)
        {
            // Mark that the threads have changed
            lock (_threadList)
            {
                {
                    var thread = _threadList.Find(t => t.Id == id);
                    if (thread == null)
                    {
                        _stateChange = true;
                    }
                }

                // This must go before getting the thread-info for the thread since that method call is async.
                // The threadId must be added to the thread-group before the new thread is created or else it will
                // be marked as a child thread and then thread-created and thread-exited won't be sent to the UI
                if (string.IsNullOrEmpty(groupId))
                {
                    groupId = c_defaultGroupId;
                }

                if (!_threadGroups.ContainsKey(groupId))
                {
                    _threadGroups[groupId] = new List<int>();
                }
                _threadGroups[groupId].Add(id);
            }

            // Run Thread-info now to get the target-id
            ResultValue resVal = null;
            if (id >= 0)
            {
                uint? tid = null;
                tid = (uint)id;
                Results results = await _debugger.MICommandFactory.ThreadInfo(tid);
                if (results.ResultClass != ResultClass.done)
                {
                    // This can happen on some versions of gdb where thread-info is not supported while running, so only assert if we're also not running. 
                    if (this._debugger.ProcessState != ProcessState.Running)
                    {
                        Debug.Fail("Thread info not successful");
                    }
                }
                else
                {
                    var tlist = results.Find<ValueListValue>("threads");

                    // tlist.Content.Length could be 0 when the thread exits between it getting created and we request thread-info
                    Debug.Assert(tlist.Content.Length <= 1, "Expected at most 1 thread, received more than one thread.");
                    resVal = tlist.Content.FirstOrDefault(item => item.FindInt("id") == id);
                }
            }

            if (resVal != null)
            {
                lock (_threadList)
                {
                    bool bNew = false;
                    var thread = SetThreadInfoFromResultValue(resVal, out bNew);
                    Debug.Assert(thread.Id == id, "thread.Id and id should match");

                    if (bNew)
                    {
                        NewThreads.Add(thread);
                        SendThreadEvents(null, null);
                    }
                }
            }
        }

        internal void ThreadExitedEvent(int id)
        {
            DebuggedThread thread = null;
            lock (_threadList)
            {
                thread = _threadList.Find(t => t.Id == id);
                if (thread != null)
                {
                    DeadThreads.Add(thread);
                    _threadList.Remove(thread);
                    _stateChange = true;
                }
                foreach (var g in _threadGroups)
                {
                    if (g.Value.Contains(id))
                    {
                        g.Value.Remove(id);
                        break;
                    }
                }
            }
            if (thread != null)
            {
                SendThreadEvents(null, null);
            }
        }

        internal void ThreadGroupExitedEvent(string groupId)
        {
            lock (_threadList)
            {
                _threadGroups.Remove(groupId);
            }
        }

        private bool IsInParent(int tid)
        {
            // only those threads in the s_defaultGroupId threadgroup are in the debuggee, others are transient while attaching to a child process
            return _threadGroups.ContainsKey(c_defaultGroupId) && _threadGroups[c_defaultGroupId].Contains(tid);
        }

        private async Task<List<ThreadContext>> WalkStack(DebuggedThread thread)
        {
            List<ThreadContext> stack = null;
            TupleValue[] frameinfo = await _debugger.MICommandFactory.StackListFrames(thread.Id, 0, 1000);
            if (frameinfo == null)
            {
                _debugger.Logger.WriteLine(LogLevel.Error, "Failed to get frame info");
            }
            else
            {
                stack = new List<ThreadContext>();
                foreach (var frame in frameinfo)
                {
                    stack.Add(CreateContext(frame));
                }
            }
            return stack;
        }

        private ThreadContext CreateContext(TupleValue frame)
        {
            ulong? pc = frame.TryFindAddr("addr");

            // don't report source line info for modules marked as IgnoreSource
            bool ignoreSource = false;
            if (pc != null)
            {
                var module = _debugger.FindModule(pc.Value);
                if (module != null && module.IgnoreSource)
                {
                    ignoreSource = true;
                }
            }
            MITextPosition textPosition = !ignoreSource ? MITextPosition.TryParse(this._debugger, frame) : null;

            string func = frame.TryFindString("func");
            uint level = frame.FindUint("level");
            string from = frame.TryFindString("from");

            return new ThreadContext(pc, textPosition, func, level, from);
        }

        private DebuggedThread SetThreadInfoFromResultValue(ResultValue resVal, out bool isNewThread)
        {
            int threadId = resVal.FindInt("id");
            string targetId = resVal.TryFindString("target-id");

            DebuggedThread thread = FindThread(threadId, out isNewThread);
            thread.Alive = true;

            // Only update targetId if it is a new thread.
            if (isNewThread && !String.IsNullOrEmpty(targetId))
            {
                thread.TargetId = targetId;
            }
            if (resVal.Contains("name"))
            {
                thread.Name = resVal.FindString("name");
            }

            return thread;
        }

        private async Task<ThreadContext> CollectThreadsInfo(int cxtThreadId)
        {
            ThreadContext ret = null;
            // set of threads has changed or thread locations have been asked for
            Results threadsinfo = await _debugger.MICommandFactory.ThreadInfo();

            if (threadsinfo.ResultClass != ResultClass.done)
            {
                Debug.Fail("Failed to get thread info");
            }
            else
            {
                var tlist = threadsinfo.Find<ValueListValue>("threads");

                // update our thread list   
                lock (_threadList)
                {
                    foreach (var thread in _threadList)
                    {
                        thread.Alive = false;
                    }

                    foreach (var t in tlist.Content)
                    {
                        bool bNew = false;
                        var thread = SetThreadInfoFromResultValue(t, out bNew);
                        int threadId = thread.Id;

                        if (bNew)
                        {
                            NewThreads.Add(thread);
                        }

                        TupleValue[] frames = ((TupleValue)t).FindAll<TupleValue>("frame");

                        if (frames.Any())
                        {
                            List<ThreadContext> stack = new List<ThreadContext>();
                            stack.AddRange(frames.Select(frame => CreateContext(frame)));

                            _topContext[threadId] = stack[0];
                            if (threadId == cxtThreadId)
                            {
                                ret = _topContext[threadId];
                            }

                            if (stack.Count > 1)
                            {
                                _stackFrames[threadId] = stack;
                            }
                        }
                    }

                    foreach (var thread in _threadList.ToList())
                    {
                        if (!thread.Alive)
                        {
                            DeadThreads.Add(thread);
                            _threadList.Remove(thread);
                        }
                    }

                    _stateChange = false;
                    _full = true;
                }
            }
            return ret;
        }

        internal void SendThreadEvents(object sender, EventArgs e)
        {
            if (_debugger.Engine.ProgramCreateEventSent)
            {
                List<DebuggedThread> deadThreads;
                List<DebuggedThread> newThreads;
                lock (_threadList)
                {
                    deadThreads = _deadThreads;
                    _deadThreads = null;
                    newThreads = _newThreads;
                    _newThreads = null;
                }
                if (newThreads != null)
                {
                    if (newThreads.Count == _threadList.Count)
                    {
                        // These are the first threads. Send a processInfoUpdateEvent too.
                        AD7ProcessInfoUpdatedEvent.Send(_debugger.Engine, _debugger.LaunchOptions.ExePath, (uint)_debugger.PidByInferior("i1"));
                    }
                    foreach (var newt in newThreads)
                    {
                        // If we are child process debugging, check and see if its a child thread
                        if (!(_debugger.IsChildProcessDebugging && newt.ChildThread))
                        {
                            _callback.OnThreadStart(newt);
                        }
                    }
                }
                if (deadThreads != null)
                {
                    foreach (var dead in deadThreads)
                    {
                        // If we are child process debugging, check and see if its a child thread
                        if (!(_debugger.IsChildProcessDebugging && dead.ChildThread))
                        {
                            // Send the destroy event outside the lock
                            _callback.OnThreadExit(dead, 0);
                        }
                    }
                }
            }
        }

        private DebuggedThread FindThread(int id, out bool bNew)
        {
            DebuggedThread newthread;
            bNew = false;
            var thread = _threadList.Find(t => t.Id == id);
            if (thread != null)
                return thread;
            // thread not found, so create it, and return it
            newthread = new DebuggedThread(id, _debugger.Engine);
            if (!IsInParent(id))
            {
                newthread.ChildThread = true;
            }
            newthread.Default = false;
            _threadList.Add(newthread);
            bNew = true;
            return newthread;
        }
    }
}
