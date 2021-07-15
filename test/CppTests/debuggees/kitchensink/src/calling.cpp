#include "calling.h"

Calling::Calling()
    : Feature("Calling")
{
}

// Averages a variable number of doubles. Pass the count into argCount
double average(int argCount, ...)
{
    va_list args;
    va_start(args, argCount);

    double total = 0;
    for (int i = 0; i < argCount; i++)
    {
        total += va_arg(args, double);
    }

    va_end(args);
    return total / argCount;
}

void recursiveCall(int count)
{
    if (count <= 0)
        return;

    recursiveCall(count - 1);
}

void c()
{
}

void b()
{
    c();
}

void a()
{
    b();
}

void Calling::CoreRun()
{
    this->Log("Calling recursive function.");
    recursiveCall(30);
    a();

    this->Log("Calling variable arg function.");
    double ave = average(10, 1.0, 3.0, 4.5, 11.0, -30.0, 17.4, 2.1, -4.0, 0.0, 12.1);
    this->Log("Average ", ave);
}