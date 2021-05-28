#include <string>
#include <vector>
#include "expression.h"

using namespace std;


int accumulate(int x)
{
    if (x <= 1)
        return 1;
    return x + accumulate(x - 1);
}

extern "C" void func()
{
    bool isCalled = true;
    double d = 10.10;
    accumulate(3);
}

void Expression::checkPrimitiveTypes()
{
    bool mybool = true;
    char mychar = 'A';
    int myint = 100;
    float myfloat = 299.0f;
    double mydouble = 321.00;
    wchar_t mywchar = 'z';

    this->checkArrayAndPointers();
}

void Expression::checkArrayAndPointers()
{
    int arr[] = { 0,1,2,3,4 };
    int *pArr = arr;

    this->checkClassOnStackAndHeap();
}

void Expression::checkClassOnStackAndHeap()
{
    Test::Student student;
    student.name = "John";
    student.age = 10;

    Test::Student* pStu = new Test::Student();
    pStu->name = "Bob";
    pStu->age = 9;
    delete pStu;
    pStu = nullptr;
    this->checkSpecialValues();
}

void Expression::checkSpecialValues()
{
    char mynull = '\0';
    double mydouble = 1.0, zero = 0.0;
    mydouble /= zero;

    this->checkPrettyPrinting();
}

void Expression::checkPrettyPrinting()
{
    string str = "hello, world";

    vector<int> vec;
    for (int i = 0; i < 5; i++) {
        vec.push_back(i);
    }

    this->checkCallStack(&func);
}

void Expression::checkCallStack(void(*callback)())
{
    float _f = 1.0f;
    callback();
}

void Expression::CoreRun()
{
    this->checkPrimitiveTypes();
}

Expression::Expression() : Feature("Expression")
{

}

int Test::max(int x, int y)
{
    if ((x - y) >= 0)
        return x;
    else
        return y;
}

double Test::max(double x, double y)
{
    if ((x - y) >= 0)
        return x;
    else
        return y;
}
