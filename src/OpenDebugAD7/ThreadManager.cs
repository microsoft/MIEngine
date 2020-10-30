using Microsoft.VisualStudio.Debugger.Interop;
using System.Collections.Concurrent;

namespace OpenDebugAD7
{
    internal class ThreadManager
    {
        private readonly ConcurrentDictionary<int, IDebugThread2> m_threads = new ConcurrentDictionary<int, IDebugThread2>();

        internal IDebugThread2 TryGetThread(int threadId)
        {
            if (m_threads.TryGetValue(threadId, out IDebugThread2 thread))
            {
                return thread;
            }

            return null;
        }

        internal bool TryRemoveThread(int threadId)
        {
            return m_threads.TryRemove(threadId, out IDebugThread2 _);
        }
        
        internal bool TryAddThread(IDebugThread2 thread)
        {
            return m_threads.TryAdd(thread.Id(), thread);
        }

        internal ConcurrentDictionary<int, IDebugThread2> Copy()
        {
            return new ConcurrentDictionary<int, IDebugThread2>(m_threads);
        }
    }
}
