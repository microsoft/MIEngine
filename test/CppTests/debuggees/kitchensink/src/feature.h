#pragma once
#include "global.h"
#include <iostream>
#include <vector>
#include <string>

using namespace std;

class Feature
{
public:
    Feature(string name);
    virtual ~Feature() {}

    string GetName();
    void Run();
    void Log();

    // These logging function seem to need to be inline, I'm guessing it is
    // because the signatures are created on demand.
    template <typename T> void Log(const T& t)
    {
        cout << t << endl;
    }

    template <typename First, typename... Rest> void Log(const First& first, const Rest&... rest)
    {
        // Logs the first item, then recurse
        cout << first;
        this->Log(rest...);
    }

    virtual void CoreRun() = 0;
private:
    string _name;
};