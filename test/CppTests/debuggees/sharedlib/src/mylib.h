#pragma once

#include <string>
#include "mylib_base.h"

using namespace std;

class myClass :public myBase
{
public:
    int DisplayAge(int age);
    string DisplayName(string firstName, string lastName);
};
