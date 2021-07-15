#include "feature.h"

Feature::Feature(string name)
{
    this->_name = name;
}

string Feature::GetName()
{
    return this->_name;
}

void Feature::Log()
{
    cout << endl;
}

void Feature::Run()
{
    this->Log();
    this->Log("##### Feature '", this->GetName(), "' #####");
    this->CoreRun();
    this->Log("----- Feature '", this->GetName(), "' -----");
}
