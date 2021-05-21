#pragma once
#include "global.h"
#include <iostream>
#include <vector>
#include <string>
#include <stdarg.h>

#include "feature.h"

using namespace std;

class Arguments : public Feature
{
public:
    Arguments();
    virtual void CoreRun();

    int argc = 0;
    char ** argv = NULL;

    bool runNonTerminating = false;
    bool runCalling = false;
    bool runThreading = false;
    bool runExpression = false;
    bool runEnvironment = false;
};