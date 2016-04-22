// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MICore;
using System.Diagnostics;

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
            _threadGroups["i1"] = new List<int>();  // initialize the processes thread group
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

        internal void ThreadCreatedEvent(int id, string groupId)
        {
            lock (_threadList)
            {
                var thread = _threadList.Find(t => t.Id == id);
                if (thread == null)
                {
                    _stateChange = true;
                }
                if (!_threadGroups.ContainsKey(groupId))
                {
                    _threadGroups[groupId] = new List<int>();
                }
                _threadGroups[groupId].Add(id);
            }
        }

        internal void ThreadExitedEvent(int id)
        {
            lock (_threadList)
            {
                var thread = _threadList.Find(t => t.Id == id);
                if (thread != null)
                {
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
        }

        internal void ThreadGroupExitedEvent(string groupId)
        {
            lock (_threadList)
            {
                if (_threadGroups.ContainsKey(groupId))
                {
                    _threadGroups.Remove(groupId);
                }
            }
        }

        private bool IsInParent(int tid)
        {
            // only those threads in the "i1" threadgroup are in the debugee, others are transient while attaching to a child process
            return _threadGroups["i1"].Contains(tid);
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
            MITextPosition textPosition = MITextPosition.TryParse(frame);
            string func = frame.TryFindString("func");
            uint level = frame.FindUint("level");
            string from = frame.TryFindString("from");

            return new ThreadContext(pc, textPosition, func, level, from);
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
                        int threadId = t.FindInt("id");
                        string targetId = t.TryFindString("target-id");
                        string state = t.FindString("state");
                        TupleValue[] frames = ((TupleValue)t).FindAll<TupleValue>("frame");

                        bool bNew;
                        DebuggedThread thread = FindThread(threadId, out bNew);
                        thread.Alive = true;
                        if (!String.IsNullOrEmpty(targetId))
                        {
                            uint tid = 0;
                            if (System.UInt32.TryParse(targetId, out tid) && tid != 0)
                            {
                                thread.TargetId = tid;
                            }
                            else if (targetId.StartsWith("Thread ", StringComparison.OrdinalIgnoreCase) &&
                                     System.UInt32.TryParse(targetId.Substring("Thread ".Length), out tid) &&
                                     tid != 0
                            )
                            {
                                thread.TargetId = tid;
                            }
                            else if (targetId.StartsWith("Process ", StringComparison.OrdinalIgnoreCase) &&
                                    System.UInt32.TryParse(targetId.Substring("Process ".Length), out tid) &&
                                    tid != 0
                            )
                            {   // First thread in a linux process has tid == pid
                                thread.TargetId = tid;
                            }
                            else if (targetId.StartsWith("Thread ", StringComparison.OrdinalIgnoreCase))
                            {
                                // In processes with pthreads the thread name is in form: "Thread <0x123456789abc> (LWP <thread-id>)"
                                int lwp_pos = targetId.IndexOf("(LWP ");
                                int paren_pos = targetId.LastIndexOf(')');
                                int len = paren_pos - (lwp_pos + 5);
                                if (len > 0 && System.UInt32.TryParse(targetId.Substring(lwp_pos + 5, len), out tid) && tid != 0)
                                {
                                    thread.TargetId = tid;
                                }
                            }
                            else
                            {
                                thread.TargetId = --s_targetId;
                            }
                        }
                        if (t.Contains("name"))
                        {
                            thread.Name = t.FindString("name");
                        }
                        if (bNew)
                        {
                            if (_newThreads == null)
                            {
                                _newThreads = new List<DebuggedThread>();
                            }
                            _newThreads.Add(thread);
                        }
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
                    foreach (var thread in _threadList)
                    {
                        if (!thread.Alive)
                        {
                            if (_deadThreads == null)
                            {
                                _deadThreads = new List<DebuggedThread>();
                            }
                            _deadThreads.Add(thread);
                        }
                    }
                    if (_deadThreads != null)
                        foreach (var dead in _deadThreads)
                        {
                            _threadList.Remove(dead);
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
                foreach (var newt in newThreads)
                {
                    if (!newt.ChildThread)
                    {
                        _callback.OnThreadStart(newt);
                    }
                }
            if (deadThreads != null)
                foreach (var dead in deadThreads)
                {
                    if (!dead.ChildThread)
                    {
                        // Send the destroy event outside the lock
                        _callback.OnThreadExit(dead, 0);
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
