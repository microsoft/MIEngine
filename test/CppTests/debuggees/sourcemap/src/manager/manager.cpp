#include "manager.h"

Manager::Manager() 
{ }

void Manager::AddInt(int number)
{
    this->items.push_back(number);
}

int Manager::RemoveInt()
{
    int val = this->items.back();
    this->items.pop_back();
    return val;
}

int Manager::Size()
{
    return (int)this->items.size();
}

int Manager::Sum()
{
    int sum = 0;
    for (auto & it : items)
    {
        sum += it;
    }
    return sum;
}