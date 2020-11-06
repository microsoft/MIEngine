// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using MICore;
using Xunit;

namespace MICoreUnitTests
{
    public class CommandLockTests
    {
        [Fact]
        public void ExclusiveTest1()
        {
            var commandLock = new CommandLock();

            try
            {
                Task<ExclusiveLockToken> t1 = commandLock.AquireExclusive();
                Assert.True(t1.IsCompleted);

                Task<ExclusiveLockToken> t2 = commandLock.AquireExclusive();
                Assert.False(t2.IsCompleted);

                ExclusiveLockToken token = t1.Result;
                token.Close();
                Assert.True(t2.IsCompleted, "Closing t1 should signal t2");
                Assert.True(ExclusiveLockToken.IsNullOrClosed(token), "Closing the token should zero it out");
            }
            finally
            {
                commandLock.Close("Test complete");
            }
        }

        [Fact]
        public void SharedTest1()
        {
            var commandLock = new CommandLock();

            try
            {
                // should be able to aquire multiple shared locks
                Task t1 = commandLock.AquireShared();
                Assert.True(t1.IsCompleted);

                Task t2 = commandLock.AquireShared();
                Assert.True(t2.IsCompleted);

                Task<ExclusiveLockToken> t3 = commandLock.AquireExclusive();
                Assert.False(t3.IsCompleted);

                commandLock.ReleaseShared();
                Assert.False(t3.IsCompleted);

                commandLock.ReleaseShared();
                Assert.True(t3.IsCompleted);
            }
            finally
            {
                commandLock.Close("Test complete");
            }
        }

        [Fact]
        public void ExclusiveThenSharedTest()
        {
            var commandLock = new CommandLock();

            try
            {
                // Part 1 - try and aquire the shared lock while the exclusive lock is held
                Task<ExclusiveLockToken> t1 = commandLock.AquireExclusive();
                Assert.True(t1.IsCompleted);

                Task t2 = commandLock.AquireShared();
                Assert.False(t2.IsCompleted);

                ExclusiveLockToken token = t1.Result;
                token.Close();
                Assert.True(t2.IsCompleted, "Closing t1 should signal t2");
                Assert.True(ExclusiveLockToken.IsNullOrClosed(token), "Closing the token should zero it out");

                commandLock.ReleaseShared();


                // Part 2 - release the exclusive lock before attempting to aquire the shared lock
                t1 = commandLock.AquireExclusive();
                Assert.True(t1.IsCompleted);

                t1.Result.Close();

                t2 = commandLock.AquireShared();
                Assert.True(t2.IsCompleted, "Shared lock should be immediately aquired");
            }
            finally
            {
                commandLock.Close("Test complete");
            }
        }

        [Fact]
        public void SharedThenExclusiveTest()
        {
            var commandLock = new CommandLock();

            try
            {
                // should be able to aquire multiple shared locks
                Task t1 = commandLock.AquireShared();
                Assert.True(t1.IsCompleted);

                Task t2 = commandLock.AquireShared();
                Assert.True(t2.IsCompleted);

                commandLock.ReleaseShared();
                commandLock.ReleaseShared();

                Task<ExclusiveLockToken> t3 = commandLock.AquireExclusive();
                Assert.True(t3.IsCompleted);
            }
            finally
            {
                commandLock.Close("Test complete");
            }
        }

        [Fact]
        public void ConvertToSharedLockTest1()
        {
            // NOTE: This test covers the case that there ARE pending shared locks
            var commandLock = new CommandLock();

            try
            {
                Task<ExclusiveLockToken> t1 = commandLock.AquireExclusive();
                Assert.True(t1.IsCompleted);

                Task t2 = commandLock.AquireShared();
                Assert.False(t2.IsCompleted);

                ExclusiveLockToken token = t1.Result;
                token.ConvertToSharedLock();
                Assert.True(ExclusiveLockToken.IsNullOrClosed(token), "ConvertToSharedLock should have nulled out the token");

                Assert.True(t2.IsCompleted);
            }
            finally
            {
                commandLock.Close("Test complete");
            }
        }

        [Fact]
        public void ConvertToSharedLockTest2()
        {
            // NOTE: This test covers the case that there are NOT pending shared locks
            var commandLock = new CommandLock();

            try
            {
                Task<ExclusiveLockToken> t1 = commandLock.AquireExclusive();
                Assert.True(t1.IsCompleted);

                ExclusiveLockToken token = t1.Result;
                token.ConvertToSharedLock();
                Assert.True(ExclusiveLockToken.IsNullOrClosed(token), "ConvertToSharedLock should have nulled out the token");

                Task t2 = commandLock.AquireShared();
                Assert.True(t2.IsCompleted);
            }
            finally
            {
                commandLock.Close("Test complete");
            }
        }

        [Fact]
        public void CloseAbortsOperationsTest()
        {
            Task[] pendingTasks = new Task[2];

            var commandLock = new CommandLock();

            try
            {
                Task<ExclusiveLockToken> t1 = commandLock.AquireExclusive();
                Assert.True(t1.IsCompleted);

                pendingTasks[0] = commandLock.AquireExclusive();
                Assert.False(pendingTasks[0].IsCompleted);

                pendingTasks[1] = commandLock.AquireShared();
                Assert.False(pendingTasks[1].IsCompleted);
            }
            finally
            {
                commandLock.Close("CloseAbortsOperationsTest complete");
            }

            foreach (Task t in pendingTasks)
            {
                Assert.True(t.IsCompleted);
                Assert.IsType<DebuggerDisposedException>(t.Exception.InnerException);
                Assert.Equal("CloseAbortsOperationsTest complete", t.Exception.InnerException.Message);
            }
        }
    }
}
