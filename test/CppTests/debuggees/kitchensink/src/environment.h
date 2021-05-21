#pragma once
#include "global.h"
#include <iostream>
#include <vector>
#include <string>
#include <stdarg.h>

#include "feature.h"

using namespace std;

class Environment : public Feature
{
public:
    Environment();
    virtual void CoreRun();
};