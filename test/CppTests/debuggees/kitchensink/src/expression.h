#include <string>
#include "feature.h"

using namespace std;

extern "C" void func();
int accumulate(int x);
extern void (*callback)();

class Expression: public Feature
{
public:
	Expression();
	virtual void CoreRun();
private:
	void checkCallStack(void(*callback)());
	void checkPrettyPrinting();
	void checkPrimitiveTypes();
	void checkArrayAndPointers();
	void checkClassOnStackAndHeap();
	void checkSpecialValues();
	void checkTreeOfValue();
};

namespace Test
{
	class Student
	{
	public:
		string name;
		int age;
		Student(){}
	};

	int max(int x, int y);
	double max(double, double y);
}