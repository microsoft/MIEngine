#include "nonterminating.h"

NonTerminating::NonTerminating()
    : Feature("NonTerminating")
{
}

void ThreadLoop(NonTerminating* t)
{
    t->Log("Starting thread in NonTerminating");
    while (!t->shouldExit)
    {
        this_thread::sleep_for(chrono::milliseconds(10));
    }
    t->Log("Ending NonTerminating thread");
}

void NonTerminating::CoreRun()
{
    // Threading introduces timining differences when debugging.
    // Add background thread for common attach scenarios.
    thread backgroundThread(ThreadLoop, this);

    this->Log("Starting infinite loop.");
    bool shouldExitLocal = false;
    while (!(this->shouldExit || shouldExitLocal))
    {
        this->DoSleep();
    }
    this->shouldExit = true;
    this->Log("Exited infinite loop.");

    backgroundThread.join();
}

void NonTerminating::DoSleep()
{
    this_thread::sleep_for(chrono::milliseconds(30));
}