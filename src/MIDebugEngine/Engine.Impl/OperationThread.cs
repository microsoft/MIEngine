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
        /// Send an async operation to the worker thread, claiming the running-op slot but returning without
        /// waiting for it to complete. Later operations still serialize behind it. Faults are reported to
        /// <paramref name="onError"/>.
        /// </summary>
        public void PostAsyncOperation(AsyncOperation op, Action<Exception> onError)
        {
            if (op == null)
                throw new ArgumentNullException(nameof(op));
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));

            PostAsyncOperationInternal(op, onError);
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

        internal void PostAsyncOperationInternal(AsyncOperation op, Action<Exception> onError)
        {
            // If this is called on the Worker thread it will deadlock
            Debug.Assert(!IsPollThread());

            while (true)
            {
                if (_isClosed)
                    throw new ObjectDisposedException("WorkerThread");

                // Wait for the slot so this serializes behind any in-flight operation.
                _runningOpCompleteEvent.WaitOne();

                if (TryPostAsyncOperationInternal(op, onError))
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

        private bool TrySetOperationInternalWithProgress(AsyncProgressOperation op, string text, CancellationTokenSource canTokenSource)
        {
            var waitLoop = new HostWaitLoop(text);

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

        private bool TryPostAsyncOperationInternal(AsyncOperation op, Action<Exception> onError)
        {
            lock (_eventLock)
            {
                if (_isClosed)
                    throw new ObjectDisposedException("WorkerThread");

                if (_runningOp == null)
                {
                    _runningOpCompleteEvent.Reset();

                    _runningOp = new OperationDescriptor(op) { ErrorHandler = onError };

                    _opSet.Set();

                    // Unlike TrySetOperationInternal, do not wait for completion; faults are routed to onError.
                    return true;
                }
            }

            return false;
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
