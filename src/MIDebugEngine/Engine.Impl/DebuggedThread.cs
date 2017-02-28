// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MICore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            TargetId = (uint)id;
            AD7Thread ad7Thread = new MIDebugEngine.AD7Thread(engine, this);
            Client = ad7Thread;
            ChildThread = false;
        }

        public int Id { get; private set; }
        public uint TargetId { get; set; }
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
        private static uint s_targetId;
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

        static ThreadCache()
        {
            s_targetId = uint.MaxValue;
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
            if (_stateChange) // if new threads 
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
                _debugger.Logger.WriteLine("Stack walk failed on thread: " + thread.TargetId);
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

        internal async void ThreadCreatedEvent(int id, string groupId)
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
            }

            // Try and get the info for the new thread now.
            try
            {
                ResultValue resVal = null;
                if (id >= 0)
                {
                    uint? tid = null;
                    tid = (uint)id;
                    Results results = await _debugger.MICommandFactory.ThreadInfo(tid);
                    if (results.ResultClass != ResultClass.done)
                    {
                        Debug.Fail("Thread info not successful");
                    }
                    else
                    {
                        var tlist = results.Find<ValueListValue>("threads");

                        Debug.Assert(tlist.Content.Length == 1, "Expected 1 thread, received more than one thread.");
                        resVal = tlist.Content.FirstOrDefault(item => item.FindInt("id") == id);
                    }
                }

                lock (_threadList)
                {
                    // The thread groups section must happen before a new DebuggedThread might be created.
                    if (string.IsNullOrEmpty(groupId))
                    {
                        groupId = c_defaultGroupId;
                    }
                    if (!_threadGroups.ContainsKey(groupId))
                    {
                        _threadGroups[groupId] = new List<int>();
                    }
                    _threadGroups[groupId].Add(id);

                    if (resVal != null)
                    {
                        bool bNew = false;
                        var thread = SetThreadInfoFromResultValue(resVal, out bNew);
                        if (bNew)
                        {
                            _callback.OnThreadStart(thread);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Don't want to crash VS
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
            // only those threads in the s_defaultGroupId threadgroup are in the debugee, others are transient while attaching to a child process
            return _threadGroups[c_defaultGroupId].Contains(tid);
        }

        private async Task<List<ThreadContext>> WalkStack(DebuggedThread thread)
        {
            List<ThreadContext> stack = null;
            TupleValue[] frameinfo = await _debugger.MICommandFactory.StackListFrames(thread.Id, 0, 1000);
            if (frameinfo == null)
            {
                _debugger.Logger.WriteLine("Failed to get frame info");
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
            MITextPosition textPosition = MITextPosition.TryParse(this._debugger, frame);
            string func = frame.TryFindString("func");
            uint level = frame.FindUint("level");
            string from = frame.TryFindString("from");

            return new ThreadContext(pc, textPosition, func, level, from);
        }

        private bool TryGetTidFromTargetId(string targetId, out uint tid)
        {
            tid = 0;
            if (System.UInt32.TryParse(targetId, out tid) && tid != 0)
            {
                return true;
            }
            else if (targetId.StartsWith("Thread ", StringComparison.OrdinalIgnoreCase) &&
                     System.UInt32.TryParse(targetId.Substring("Thread ".Length), out tid) &&
                     tid != 0
            )
            {
                return true;
            }
            else if (targetId.StartsWith("Process ", StringComparison.OrdinalIgnoreCase) &&
                    System.UInt32.TryParse(targetId.Substring("Process ".Length), out tid) &&
                    tid != 0
            )
            {   // First thread in a linux process has tid == pid
                return true;
            }
            else if (targetId.StartsWith("Thread ", StringComparison.OrdinalIgnoreCase))
            {
                // In processes with pthreads the thread name is in form: "Thread <0x123456789abc> (LWP <thread-id>)"
                int lwp_pos = targetId.IndexOf("(LWP ", StringComparison.Ordinal);
                int paren_pos = targetId.LastIndexOf(')');
                int len = paren_pos - (lwp_pos + 5);
                if (len > 0 && System.UInt32.TryParse(targetId.Substring(lwp_pos + 5, len), out tid) && tid != 0)
                {
                    return true;
                }
            }
            else if (targetId.StartsWith("LWP ", StringComparison.OrdinalIgnoreCase) &&
                    System.UInt32.TryParse(targetId.Substring("LWP ".Length), out tid) &&
                    tid != 0
            )
            {
                // In gdb coredumps the thread name is in the form:" LWP <thread-id>"
                return true;
            }
            else
            {
                tid = --s_targetId;
                return true;
            }

            return false;
        }

        private DebuggedThread SetThreadInfoFromResultValue(ResultValue resVal, out bool newThread)
        {
            newThread = false;
            int threadId = resVal.FindInt("id");
            string targetId = resVal.TryFindString("target-id");

            DebuggedThread thread = FindThread(threadId, out newThread);
            thread.Alive = true;
            if (!String.IsNullOrEmpty(targetId))
            {
                uint tid = 0;
                if (TryGetTidFromTargetId(targetId, out tid))
                {
                    thread.TargetId = tid;
                }
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

                        if (bNew)
                        {
                            NewThreads.Add(thread);
                        }

                        int threadId = t.FindInt("id");
                        TupleValue[] frames = ((TupleValue)t).FindAll<TupleValue>("frame");
                        var stack = new List<ThreadContext>();
                        foreach (var frame in frames)
                        {
                            stack.Add(CreateContext(frame));
                        }
                        if (stack.Count > 0)
                        {
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
                foreach (var newt in newThreads)
                {
                    if (!newt.ChildThread)
                    {
                        _callback.OnThreadStart(newt);
                    }
                }
            }
            if (deadThreads != null)
            {
                foreach (var dead in deadThreads)
                {
                    if (!dead.ChildThread)
                    {
                        // Send the destroy event outside the lock
                        _callback.OnThreadExit(dead, 0);
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
