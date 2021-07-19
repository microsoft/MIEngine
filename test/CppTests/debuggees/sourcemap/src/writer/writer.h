#pragma once
#include <stdio.h>
#include <string>

class Writer
{
public:
    Writer();
    void Write(const std::string msg);
    void Write(const int number);

    void WriteLine(const std::string msg);
    void WriteLine(const int number);
};