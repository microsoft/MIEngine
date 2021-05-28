#pragma once
#include <atomic>
#include "global.h"
#include <string>
#include <thread>
#include <condition_variable>
#include <chrono>

#include "feature.h"

using namespace std;

class Threading : public Feature
{
public:
    Threading();
    virtual void CoreRun();

    condition_variable mainClosing;
    std::atomic_int runningWorkingThreadCount;
    std::mutex mainClosingMutex;
};