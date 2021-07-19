#include "mylib.h"
#include <iostream>
#include <string>

using namespace std;

int myClass::DisplayAge(int age)
{
    age++;
    cout << "my age: " << age << endl;
    return age;
}

string myClass::DisplayName(string firstName, string lastName)
{
    string name = firstName + lastName;
    cout << "my name: " << name << endl;
    return name;
}

extern "C" myClass* Create()
{
    return new myClass;
}

extern "C" void Destroy(myClass *myclass)
{
    delete myclass;
}