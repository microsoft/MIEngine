// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using MICore;

namespace MICoreUnitTests
{
    [TestClass]
    public class CommandLockTests
    {
        [TestMethod]
        public void ExclusiveTest1()
        {
            using (var commandLock = new CommandLock())
            {
                Task<ExclusiveLockToken> t1 = commandLock.AquireExclusive();
                Assert.IsTrue(t1.IsCompleted);

                Task<ExclusiveLockToken> t2 = commandLock.AquireExclusive();
                Assert.IsFalse(t2.IsCompleted);

                ExclusiveLockToken token = t1.Result;
                token.Close();
                Assert.IsTrue(t2.IsCompleted, "Closing t1 should signal t2");
                Assert.IsTrue(ExclusiveLockToken.IsNullOrClosed(token), "Closing the token should zero it out");
            }
        }

        [TestMethod]
        public void SharedTest1()
        {
            using (var commandLock = new CommandLock())
            {
                // should be able to aquire multiple shared locks
                Task t1 = commandLock.AquireShared();
                Assert.IsTrue(t1.IsCompleted);

                Task t2 = commandLock.AquireShared();
                Assert.IsTrue(t2.IsCompleted);

                Task<ExclusiveLockToken> t3 = commandLock.AquireExclusive();
                Assert.IsFalse(t3.IsCompleted);

                commandLock.ReleaseShared();
                Assert.IsFalse(t3.IsCompleted);

                commandLock.ReleaseShared();
                Assert.IsTrue(t3.IsCompleted);
            }
        }

        [TestMethod]
        public void ExclusiveThenSharedTest()
        {
            using (var commandLock = new CommandLock())
            {
                // Part 1 - try and aquire the shared lock while the exclusive lock is held
                Task<ExclusiveLockToken> t1 = commandLock.AquireExclusive();
                Assert.IsTrue(t1.IsCompleted);

                Task t2 = commandLock.AquireShared();
                Assert.IsFalse(t2.IsCompleted);

                ExclusiveLockToken token = t1.Result;
                token.Close();
                Assert.IsTrue(t2.IsCompleted, "Closing t1 should signal t2");
                Assert.IsTrue(ExclusiveLockToken.IsNullOrClosed(token), "Closing the token should zero it out");

                commandLock.ReleaseShared();


                // Part 2 - release the exclusive lock before attempting to aquire the shared lock
                t1 = commandLock.AquireExclusive();
                Assert.IsTrue(t1.IsCompleted);

                t1.Result.Close();

                t2 = commandLock.AquireShared();
                Assert.IsTrue(t2.IsCompleted, "Shared lock should be immediately aquired");
            }
        }

        [TestMethod]
        public void SharedThenExclusiveTest()
        {
            using (var commandLock = new CommandLock())
            {
                // should be able to aquire multiple shared locks
                Task t1 = commandLock.AquireShared();
                Assert.IsTrue(t1.IsCompleted);

                Task t2 = commandLock.AquireShared();
                Assert.IsTrue(t2.IsCompleted);

                commandLock.ReleaseShared();
                commandLock.ReleaseShared();

                Task<ExclusiveLockToken> t3 = commandLock.AquireExclusive();
                Assert.IsTrue(t3.IsCompleted);
            }
        }

        [TestMethod]
        public void ConvertToSharedLockTest1()
        {
            // NOTE: This test covers the case that there ARE pending shared locks
            using (var commandLock = new CommandLock())
            {
                Task<ExclusiveLockToken> t1 = commandLock.AquireExclusive();
                Assert.IsTrue(t1.IsCompleted);

                Task t2 = commandLock.AquireShared();
                Assert.IsFalse(t2.IsCompleted);

                ExclusiveLockToken token = t1.Result;
                token.ConvertToSharedLock();
                Assert.IsTrue(ExclusiveLockToken.IsNullOrClosed(token), "ConvertToSharedLock should have nulled out the token");

                Assert.IsTrue(t2.IsCompleted);
            }
        }

        [TestMethod]
        public void ConvertToSharedLockTest2()
        {
            // NOTE: This test covers the case that there are NOT pending shared locks
            using (var commandLock = new CommandLock())
            {
                Task<ExclusiveLockToken> t1 = commandLock.AquireExclusive();
                Assert.IsTrue(t1.IsCompleted);

                ExclusiveLockToken token = t1.Result;
                token.ConvertToSharedLock();
                Assert.IsTrue(ExclusiveLockToken.IsNullOrClosed(token), "ConvertToSharedLock should have nulled out the token");

                Task t2 = commandLock.AquireShared();
                Assert.IsTrue(t2.IsCompleted);
            }
        }

        [TestMethod]
        public void CloseAbortsOperationsTest()
        {
            Task[] pendingTasks = new Task[2];

            using (var commandLock = new CommandLock())
            {
                Task<ExclusiveLockToken> t1 = commandLock.AquireExclusive();
                Assert.IsTrue(t1.IsCompleted);

                pendingTasks[0] = commandLock.AquireExclusive();
                Assert.IsFalse(pendingTasks[0].IsCompleted);

                pendingTasks[1] = commandLock.AquireShared();
                Assert.IsFalse(pendingTasks[1].IsCompleted);

                commandLock.Close();
            }

            foreach (Task t in pendingTasks)
            {
                Assert.IsTrue(t.IsCompleted);
                Assert.IsInstanceOfType(t.Exception.InnerException, typeof(ObjectDisposedException));
            }
        }
    }
}
