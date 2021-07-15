#pragma once
#include "global.h"
#include <iostream>
#include <vector>
#include <string>
#include <thread>
#include <stdarg.h>
#include <condition_variable>
#include <chrono>

#include "feature.h"

using namespace std;

class NonTerminating : public Feature
{
public:
    NonTerminating();
    virtual void CoreRun();

    bool shouldExit = false;

private:
    void DoSleep();
};