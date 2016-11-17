// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MICore
{
    /// <summary>
    /// Token returned from CommandLock.AquireExclusive
    /// </summary>
    sealed public class ExclusiveLockToken : IDisposable
    // NOTE: I tried to make this a value object, but value objects don't work quite as expected in async methods and calling 'Close'
    // wasn't updating the backing value object which was stored in the state machine class
    {
        private CommandLock _commandLock;
        private int _value;

        internal ExclusiveLockToken(CommandLock commandLock, int value)
        {
            // Either both commandLock and value are non-zero (non-Null case), or both are zero (Null case)
            if (commandLock == null)
                throw new ArgumentNullException("commandLock");

            if (value == 0)
                throw new ArgumentOutOfRangeException("value");

            _commandLock = commandLock;
            _value = value;
        }

        public static bool IsNullOrClosed(ExclusiveLockToken token)
        {
            return (token == null || token._value == 0);
        }

        public override bool Equals(object obj)
        {
            throw new NotImplementedException(); // this method should never be called
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public void Dispose()
        {
            Close();
        }

        public void ConvertToSharedLock()
        {
            int value = _value;
            if (value == 0)
            {
                throw new ObjectDisposedException("ExclusiveLockToken");
            }

            var commandLock = _commandLock;
            _value = 0;
            _commandLock = null;

            commandLock.ConvertExclusiveLockToShared(value);
        }

        public void Close()
        {
            int value = _value;
            if (value != 0)
            {
                var commandLock = _commandLock;
                _value = 0;
                _commandLock = null;

                commandLock.ReleaseExclusive(value);
            }
        }
    }

    sealed public class CommandLock
    {
        /// <summary>
        /// 0 if lock is free
        /// >0 if the shared lock is held
        /// -1 if closed
        /// -2 if in exclusive mode
        /// </summary>
        private int _lockStatus;

        private const int StatusFree = 0;
        private const int StatusClosed = -1;
        private const int StatusExclusive = -2;

        private int _prevExclusiveToken;
        private int _pendingSharedLockRequests;
        private TaskCompletionSource<int> _waitingSharedLockSource;
        private readonly Queue<TaskCompletionSource<ExclusiveLockToken>> _waitingExclusiveLockRequests = new Queue<TaskCompletionSource<ExclusiveLockToken>>();
        private string _closeMessage;

        public CommandLock()
        {
        }

        internal void Close(string closeMessage)
        {
            lock (this.LockObject)
            {
                _closeMessage = closeMessage;
                _lockStatus = StatusClosed;
                if (_waitingSharedLockSource != null)
                {
                    _waitingSharedLockSource.SetException(new DebuggerDisposedException(_closeMessage));
                    _waitingSharedLockSource = null;
                }
                foreach (TaskCompletionSource<ExclusiveLockToken> completionSource in _waitingExclusiveLockRequests)
                {
                    completionSource.SetException(new DebuggerDisposedException(_closeMessage));
                }
                _waitingExclusiveLockRequests.Clear();
            }
        }

        /// <summary>
        /// Aquires an exclusive lock -- one that allows neither another another exclusive lock holder, or a shared lock holder.
        /// Returned ExclusiveLockTokens must be closed.
        /// </summary>
        public Task<ExclusiveLockToken> AquireExclusive()
        {
            lock (this.LockObject)
            {
                if (_lockStatus == StatusClosed)
                {
                    throw new DebuggerDisposedException(_closeMessage);
                }

                if (_lockStatus == StatusFree)
                {
                    _lockStatus = StatusExclusive;
                    return Task.FromResult(GetNextExclusiveLockToken());
                }

                TaskCompletionSource<ExclusiveLockToken> completionSource = new TaskCompletionSource<ExclusiveLockToken>();
                _waitingExclusiveLockRequests.Enqueue(completionSource);

                return completionSource.Task;
            }
        }

        /// <summary>
        /// Aquires a shared lock -- that is it prevents other code from aquiring an exclusive lock, but still allows for other shared locks. 
        /// Successful calls to AquiredShared must be match with a 'ReleaseShared' call.
        /// </summary>
        public Task AquireShared()
        {
            lock (this.LockObject)
            {
                if (_lockStatus == StatusClosed)
                {
                    throw new DebuggerDisposedException(_closeMessage);
                }

                if (_lockStatus >= 0)
                {
                    _lockStatus++;
                    return Task.FromResult(0);
                }

                if (_waitingSharedLockSource == null)
                {
                    _waitingSharedLockSource = new TaskCompletionSource<int>();
                }

                _pendingSharedLockRequests++;
                return _waitingSharedLockSource.Task;
            }
        }

        // Internal method called from the ExclusiveLockToken class as part of closing an exclusive lock
        internal void ReleaseExclusive(int tokenValue)
        {
            Action actionAfterReleaseLock = null;

            lock (this.LockObject)
            {
                if (_lockStatus == StatusClosed)
                {
                    return;
                }
                else if (_lockStatus != StatusExclusive || tokenValue != _prevExclusiveToken)
                {
                    Debug.Fail("Very bad - bogus exclusive lock token provided");
                    throw new InvalidOperationException();
                }

                _lockStatus = StatusFree;

                actionAfterReleaseLock = GetAfterReleaseLockAction();
            }

            if (actionAfterReleaseLock != null)
            {
                actionAfterReleaseLock();
            }
        }

        // Internal method called from the ExclusiveLockToken class to convert an exclusive lock into a shared lock
        internal void ConvertExclusiveLockToShared(int tokenValue)
        {
            Action actionAfterReleaseLock = null;

            lock (this.LockObject)
            {
                if (_lockStatus == StatusClosed)
                {
                    return;
                }
                else if (_lockStatus != StatusExclusive || tokenValue != _prevExclusiveToken)
                {
                    Debug.Fail("Very bad - bogus exclusive lock token provided");
                    throw new InvalidOperationException();
                }

                _lockStatus = 1; // we now have one shared lock

                // release any other pending shared locks as well
                actionAfterReleaseLock = MaybeSignalPendingSharedLockRequests();
            }

            if (actionAfterReleaseLock != null)
            {
                actionAfterReleaseLock();
            }
        }

        public void ReleaseShared()
        {
            Action actionAfterReleaseLock = null;

            lock (this.LockObject)
            {
                if (_lockStatus == StatusClosed)
                {
                    return;
                }
                else if (_lockStatus <= 0)
                {
                    Debug.Fail("Very bad - ReleaseShared called incorrectly");
                    throw new InvalidOperationException();
                }

                _lockStatus--;

                if (_lockStatus == StatusFree)
                {
                    actionAfterReleaseLock = GetAfterReleaseLockAction();
                }
            }

            if (actionAfterReleaseLock != null)
            {
                actionAfterReleaseLock();
            }
        }

        // NOTE: This method MUST be called with this.LockObject held
        private Action GetAfterReleaseLockAction()
        {
            Debug.Assert(_lockStatus == StatusFree, "Why is GetAfterReleaseLockAction called when the lock is not free?");

            if (_waitingExclusiveLockRequests.Count > 0)
            {
                TaskCompletionSource<ExclusiveLockToken> completionSource = _waitingExclusiveLockRequests.Dequeue();
                _lockStatus = StatusExclusive;

                var newLockToken = GetNextExclusiveLockToken();
                return () => completionSource.SetResult(newLockToken);
            }
            else
            {
                return MaybeSignalPendingSharedLockRequests();
            }
        }

        // NOTE: This method MUST be called with this.LockObject held
        private ExclusiveLockToken GetNextExclusiveLockToken()
        {
            _prevExclusiveToken++;
            if (_prevExclusiveToken <= 0) // if somehow we circle arround, go back to 1
                _prevExclusiveToken = 1;

            return new ExclusiveLockToken(this, _prevExclusiveToken);
        }

        // NOTE: This method MUST be called with this.LockObject held
        private Action MaybeSignalPendingSharedLockRequests()
        {
            Debug.Assert(_lockStatus >= 0, "Why is MaybeGetSharedLockAction called when the lock is not free/reading?");

            if (_waitingSharedLockSource != null)
            {
                TaskCompletionSource<int> completionSource = _waitingSharedLockSource;
                _waitingSharedLockSource = null;

                Debug.Assert(_pendingSharedLockRequests > 0, "Invalid value for m_pendingSharedLockRequests");
                _lockStatus += _pendingSharedLockRequests;
                _pendingSharedLockRequests = 0;

                return () => completionSource.SetResult(0);
            }
            else
            {
                return null; // no action needs to be preformed
            }
        }

        private object LockObject { get { return _waitingExclusiveLockRequests; } }
    }
}
