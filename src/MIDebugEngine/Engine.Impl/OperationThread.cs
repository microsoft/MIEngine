// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using Microsoft.DebugEngineHost;
using MICore;
using Logger = MICore.Logger;

namespace Microsoft.MIDebugEngine
{
    public delegate void Operation();
    public delegate Task AsyncOperation();
    public delegate Task AsyncProgressOperation(HostWaitLoop waitLoop);


    /// <summary>
    /// Worker thread used to process MI Debugger output and used to process AD7 commands
    /// </summary>
    internal class WorkerThread : IDisposable
    {
        private readonly AutoResetEvent _opSet;
        private readonly ManualResetEvent _runningOpCompleteEvent; // fired when either m_syncOp finishes, or the kick off of m_async
        private readonly Object _eventLock = new object(); // Locking on an event directly can cause Mono to stop responding.
        private readonly Queue<Operation> _postedOperations; // queue of fire-and-forget operations
        private readonly Queue<(AsyncOperation Operation, Action<Exception> OnError)> _postedAsyncOperations; // queue of fire-and-forget async operations

        public event EventHandler<Exception> PostedOperationErrorEvent;

        private class OperationDescriptor
        {
            /// <summary>
            /// Delegate that was added via 'RunOperation'. Is of type 'Operation' or 'AsyncOperation'
            /// </summary>
            public readonly Delegate Target;

            /// <summary>
            /// Handler invoked if the operation faults. For async operations, this may be invoked on a non-worker thread.
            /// Only set for PostAsyncOperation.
            /// </summary>
            public Action<Exception> ErrorHandler;

            public ExceptionDispatchInfo ExceptionDispatchInfo;
            public Task Task;
            private bool _isStarted;
            private bool _isComplete;

            public OperationDescriptor(Delegate target)
            {
                this.Target = target;
            }

            public bool IsComplete
            {
                get { return _isComplete; }
            }
            public void MarkComplete()
            {
                Debug.Assert(_isStarted == true, "MarkComplete called before MarkStarted?");
                Debug.Assert(_isComplete == false, "MarkComplete called more than once??");
                _isComplete = true;
            }

            public bool IsStarted
            {
                get { return _isStarted; }
            }
            public void MarkStarted()
            {
                Debug.Assert(_isStarted == false, "MarkStarted called more than once??");
                _isStarted = true;
            }
        }
        private OperationDescriptor _runningOp;

        private Thread _thread;
        private volatile bool _isClosed;

        public Logger Logger { get; private set; }

        public WorkerThread(Logger logger)
        {
            Logger = logger;
            _opSet = new AutoResetEvent(false);
            _runningOpCompleteEvent = new ManualResetEvent(true);
            _postedOperations = new Queue<Operation>();
            _postedAsyncOperations = new Queue<(AsyncOperation Operation, Action<Exception> OnError)>();

            _thread = new Thread(new ThreadStart(ThreadFunc));
            _thread.Name = "MIDebugger.PollThread";
            _thread.Start();
        }

        /// <summary>
        /// Send an operation to the worker thread, and block for it to finish. This is used for implementing
        /// most AD7 interfaces. This will wait for other 'RunOperation' calls to finish before starting.
        /// </summary>
        /// <param name="op">Delegate for the code to run on the worker thread</param>
        public void RunOperation(Operation op)
        {
            if (op == null)
                throw new ArgumentNullException(nameof(op));

            SetOperationInternal(op);
        }

        /// <summary>
        /// Send an operation to the worker thread, and block for it to finish (task returns complete). This is used for implementing
        /// most AD7 interfaces. This will wait for other 'RunOperation' calls to finish before starting.
        /// </summary>
        /// <param name="op">Delegate for the code to run on the worker thread. This returns a Task that we wait on.</param>
        public void RunOperation(AsyncOperation op)
        {
            if (op == null)
                throw new ArgumentNullException(nameof(op));

            SetOperationInternal(op);
        }

        public void RunOperation(string text, CancellationTokenSource canTokenSource, AsyncProgressOperation op)
        {
            if (op == null)
                throw new ArgumentNullException(nameof(op));

            SetOperationInternalWithProgress(op, text, canTokenSource);
        }

        /// <summary>
        /// Queue an async operation to run on the worker thread and return immediately, without waiting for the
        /// operation to start or finish. Posted async operations run one at a time, in the order posted, and
        /// serialize behind any in-flight operation. Faults are reported to <paramref name="onError"/>.
        /// </summary>
        public void PostAsyncOperation(AsyncOperation op, Action<Exception> onError)
        {
            if (op == null)
                throw new ArgumentNullException(nameof(op));
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));

            if (_isClosed)
                throw new ObjectDisposedException("WorkerThread");

            lock (_postedAsyncOperations)
            {
                if (_isClosed)
                    throw new ObjectDisposedException("WorkerThread");

                _postedAsyncOperations.Enqueue((op, onError));
                _opSet.Set();
            }
        }

        public void Close()
        {
            if (_isClosed)
                return;

            // block out posting any more operations
            lock (_postedOperations)
            {
                if (_isClosed)
                    return;

                _isClosed = true;

                // Wait for any pending running operations to finish
                while (true)
                {
                    _runningOpCompleteEvent.WaitOne();

                    lock (_eventLock)
                    {
                        if (_runningOp == null)
                        {
                            _opSet.Set();
                            break;
                        }
                    }
                }
            }

            // Fail any queued async operations that will never run now that we are closed, instead of
            // dropping them silently. The poll thread won't promote them once _isClosed is set.
            while (true)
            {
                Action<Exception> onError;
                lock (_postedAsyncOperations)
                {
                    if (_postedAsyncOperations.Count == 0)
                        break;

                    onError = _postedAsyncOperations.Dequeue().OnError;
                }

                InvokeErrorHandler(onError, new ObjectDisposedException("WorkerThread"));
            }
        }

        internal void SetOperationInternal(Delegate op)
        {
            // If this is called on the Worker thread it will deadlock
            Debug.Assert(!IsPollThread());

            while (true)
            {
                if (_isClosed)
                    throw new ObjectDisposedException("WorkerThread");

                _runningOpCompleteEvent.WaitOne();

                if (TrySetOperationInternal(op))
                {
                    return;
                }
            }
        }

        internal void SetOperationInternalWithProgress(AsyncProgressOperation op, string text, CancellationTokenSource canTokenSource)
        {
            // If this is called on the Worker thread it will deadlock
            Debug.Assert(!IsPollThread());

            while (true)
            {
                if (_isClosed)
                    throw new ObjectDisposedException("WorkerThread");

                _runningOpCompleteEvent.WaitOne();

                if (TrySetOperationInternalWithProgress(op, text, canTokenSource))
                {
                    return;
                }
            }
        }

        public void PostOperation(Operation op)
        {
            if (op == null)
                throw new ArgumentNullException(nameof(op));

            if (_isClosed)
                throw new ObjectDisposedException("WorkerThread");

            lock (_postedOperations)
            {
                if (_isClosed)
                    throw new ObjectDisposedException("WorkerThread");

                _postedOperations.Enqueue(op);
                if (_postedOperations.Count == 1)
                {
                    _opSet.Set();
                }
            }
        }

        private bool TrySetOperationInternal(Delegate op)
        {
            bool claimed = false;
            try
            {
                lock (_eventLock)
                {
                    if (_isClosed)
                        throw new ObjectDisposedException("WorkerThread");

                    if (_runningOp == null)
                    {
                        _runningOpCompleteEvent.Reset();

                        OperationDescriptor runningOp = new OperationDescriptor(op);
                        _runningOp = runningOp;

                        _opSet.Set();

                        _runningOpCompleteEvent.WaitOne();
                        claimed = true;

                        Debug.Assert(runningOp.IsComplete, "Why isn't the running op complete?");

                        if (runningOp.ExceptionDispatchInfo != null)
                        {
                            runningOp.ExceptionDispatchInfo.Throw();
                        }

                        return true;
                    }
                }

                return false;
            }
            finally
            {
                // The running-op slot was just freed and _eventLock is now released. If promotion on the poll
                // thread lost the TryEnter race against this method, re-arm _opSet so a queued async operation
                // is promoted immediately instead of stranded until the next unrelated wakeup.
                if (claimed && HasPostedAsyncOperation())
                {
                    _opSet.Set();
                }
            }
        }

        private bool TrySetOperationInternalWithProgress(AsyncProgressOperation op, string text, CancellationTokenSource canTokenSource)
        {
            var waitLoop = new HostWaitLoop(text);

            bool claimed = false;
            try
            {
                lock (_eventLock)
                {
                    if (_isClosed)
                        throw new ObjectDisposedException("WorkerThread");

                    if (_runningOp == null)
                    {
                        _runningOpCompleteEvent.Reset();

                        OperationDescriptor runningOp = new OperationDescriptor(new AsyncOperation(() => { return op(waitLoop); }));
                        _runningOp = runningOp;

                        _opSet.Set();

                        waitLoop.Wait(_runningOpCompleteEvent, canTokenSource);
                        claimed = true;

                        Debug.Assert(runningOp.IsComplete, "Why isn't the running op complete?");

                        if (runningOp.ExceptionDispatchInfo != null)
                        {
                            runningOp.ExceptionDispatchInfo.Throw();
                        }

                        return true;
                    }
                }

                return false;
            }
            finally
            {
                // The running-op slot was just freed and _eventLock is now released. If promotion on the poll
                // thread lost the TryEnter race against this method, re-arm _opSet so a queued async operation
                // is promoted immediately instead of stranded until the next unrelated wakeup.
                if (claimed && HasPostedAsyncOperation())
                {
                    _opSet.Set();
                }
            }
        }

        // Called on the poll thread to promote the next posted async operation into the running-op slot when it
        // is free. Returns true if an operation was moved into the slot.
        private bool TryStartPostedAsyncOperation()
        {
            Debug.Assert(IsPollThread(), "TryStartPostedAsyncOperation must run on the poll thread.");

            // Cheap early-out so the poll loop does not contend on _eventLock when there is nothing to promote.
            lock (_postedAsyncOperations)
            {
                if (_isClosed || _postedAsyncOperations.Count == 0)
                    return false;
            }

            // Never block on _eventLock here: a client in TrySetOperationInternal holds it across
            // _runningOpCompleteEvent.WaitOne() until its operation completes, and only the poll thread can
            // complete that operation, so blocking here would deadlock. If a client is mid-set, skip promotion;
            // it will be retried on a later poll-loop iteration or wakeup.
            if (!Monitor.TryEnter(_eventLock))
                return false;

            try
            {
                if (_isClosed || _runningOp != null)
                    return false;

                (AsyncOperation Operation, Action<Exception> OnError) posted;
                lock (_postedAsyncOperations)
                {
                    if (_postedAsyncOperations.Count == 0)
                        return false;

                    posted = _postedAsyncOperations.Dequeue();
                }

                _runningOpCompleteEvent.Reset();

                // Unlike TrySetOperationInternal, no one waits for completion; faults are routed to the handler.
                _runningOp = new OperationDescriptor(posted.Operation) { ErrorHandler = posted.OnError };

                return true;
            }
            finally
            {
                Monitor.Exit(_eventLock);
            }
        }

        private bool HasPostedAsyncOperation()
        {
            lock (_postedAsyncOperations)
            {
                return _postedAsyncOperations.Count > 0;
            }
        }

        // Thread routine for the poll loop. It handles calls coming in from the debug engine as well as polling for debug events.
        private void ThreadFunc()
        {
            while (!_isClosed)
            {
                // Wait for an operation to be set
                _opSet.WaitOne();

                // Run until we go through a loop where there was nothing to do
                bool ranOperation;
                do
                {
                    ranOperation = false;

                    if (_runningOp == null)
                    {
                        TryStartPostedAsyncOperation();
                    }

                    OperationDescriptor runningOp = _runningOp;
                    if (runningOp != null && !runningOp.IsStarted)
                    {
                        runningOp.MarkStarted();
                        ranOperation = true;

                        bool completeAsync = false;
                        Operation syncOp = runningOp.Target as Operation;
                        if (syncOp != null)
                        {
                            try
                            {
                                syncOp();
                            }
                            catch (Exception opException) when (ExceptionHelper.BeforeCatch(opException, Logger, reportOnlyCorrupting: true))
                            {
                                runningOp.ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(opException);
                            }
                        }
                        else
                        {
                            AsyncOperation asyncOp = (AsyncOperation)runningOp.Target;

                            try
                            {
                                runningOp.Task = asyncOp();
                            }
                            catch (Exception opException) when (ExceptionHelper.BeforeCatch(opException, Logger, reportOnlyCorrupting: true))
                            {
                                runningOp.ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(opException);
                            }

                            if (runningOp.Task != null)
                            {
                                runningOp.Task.ContinueWith(OnAsyncRunningOpComplete, TaskContinuationOptions.ExecuteSynchronously);
                                completeAsync = true;
                            }
                        }

                        if (!completeAsync)
                        {
                            // Capture the fault before clearing the slot so a synchronous throw is still reported.
                            Action<Exception> errorHandler = runningOp.ErrorHandler;
                            ExceptionDispatchInfo exceptionDispatchInfo = runningOp.ExceptionDispatchInfo;

                            runningOp.MarkComplete();

                            Debug.Assert(_runningOp == runningOp, "How did m_runningOp change?");
                            _runningOp = null;
                            _runningOpCompleteEvent.Set();

                            if (errorHandler != null && exceptionDispatchInfo != null)
                            {
                                InvokeErrorHandler(errorHandler, exceptionDispatchInfo.SourceException);
                            }
                        }
                    }


                    Operation postedOperation = null;
                    lock (_postedOperations)
                    {
                        if (_postedOperations.Count > 0)
                        {
                            postedOperation = _postedOperations.Dequeue();
                        }
                    }

                    if (postedOperation != null)
                    {
                        ranOperation = true;

                        try
                        {
                            postedOperation();
                        }
                        catch (Exception e) when (ExceptionHelper.BeforeCatch(e, Logger, reportOnlyCorrupting: false))
                        {
                            if (PostedOperationErrorEvent != null)
                            {
                                PostedOperationErrorEvent(this, e);
                            }
                        }
                    }
                }
                while (ranOperation);
            }
        }

        internal void OnAsyncRunningOpComplete(Task t)
        {
            Debug.Assert(_runningOp != null, "How did m_runningOp get cleared?");
            Debug.Assert(t == _runningOp.Task, "Why is a different task completing?");

            if (t.Exception != null)
            {
                if (t.Exception.InnerException != null)
                {
                    _runningOp.ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(t.Exception.InnerException);
                }
                else
                {
                    _runningOp.ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(t.Exception);
                }
            }
            _runningOp.MarkComplete();

            // Capture the fault before clearing the slot so it is routed to the handler, not discarded.
            Action<Exception> errorHandler = _runningOp.ErrorHandler;
            ExceptionDispatchInfo exceptionDispatchInfo = _runningOp.ExceptionDispatchInfo;

            // Invoke the handler while the running-op slot is still held so it does not run concurrently
            // with the next queued operation. This may still run on the task's completion thread.
            if (errorHandler != null && exceptionDispatchInfo != null)
            {
                InvokeErrorHandler(errorHandler, exceptionDispatchInfo.SourceException);
            }

            _runningOp = null;
            _runningOpCompleteEvent.Set();

            lock (_postedAsyncOperations)
            {
                if (_postedAsyncOperations.Count > 0)
                {
                    _opSet.Set();
                }
            }
        }

        private void InvokeErrorHandler(Action<Exception> errorHandler, Exception exception)
        {
            try
            {
                errorHandler(exception);
            }
            catch (Exception e) when (ExceptionHelper.BeforeCatch(e, Logger, reportOnlyCorrupting: false))
            {
                if (PostedOperationErrorEvent != null)
                {
                    PostedOperationErrorEvent(this, e);
                }
            }
        }

        internal bool IsPollThread()
        {
            return Thread.CurrentThread == _thread;
        }

        void IDisposable.Dispose()
        {
            Close();
        }
    }
}
