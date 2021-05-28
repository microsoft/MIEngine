#include "threading.h"

Threading::Threading()
    : Feature("Threading"), runningWorkingThreadCount(0)
{
}

// Body of "worker" threads. Loop for loopCount seconds
void WorkerThreadLoop(Threading* t, int loopCount, string threadName)
{
    t->Log("Starting thread ", threadName, ". LoopCount: ", loopCount);
    t->runningWorkingThreadCount++;

    // Wait until main is ready before closing thread
    unique_lock<mutex> lk(t->mainClosingMutex);
    t->mainClosing.wait(lk);

    t->Log("Ending thread ", threadName, ". LoopCount: ", loopCount);
    t->runningWorkingThreadCount--;
}

void Threading::CoreRun()
{
    this->Log("Creating a few threads.");

    thread A(WorkerThreadLoop, this, 3, "A-Blue");
    thread B(WorkerThreadLoop, this, 2, "B-Green");
    thread C(WorkerThreadLoop, this, 0, "C-Orange");
    thread D(WorkerThreadLoop, this, 1, "D-Red");

    this->Log("Wait for threads to start...");
    while (this->runningWorkingThreadCount < 4)
    {
        this_thread::sleep_for(chrono::milliseconds(100));
    }

    this->Log("All threads running!");

    this->Log("Notify threads to close...");

    while (this->runningWorkingThreadCount > 0)
    {
        this->mainClosing.notify_all();
        this_thread::sleep_for(chrono::milliseconds(100));
    }

    A.join();
    B.join();
    C.join();
    D.join();
}