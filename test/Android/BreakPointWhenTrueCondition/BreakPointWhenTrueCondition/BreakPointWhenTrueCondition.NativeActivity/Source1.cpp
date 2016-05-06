
#include "Header1.h"

int MyClass :: Func() {
	int i = 1;
	int sum = 0;

	for (; i < 10; i++) {
		sum = sum + i;
	}

	return sum;
}

int Func1() {
	int i = 1;
	int sum = 0;

	for (; i < 10; i++) {
		sum = sum + i;
	}

	return sum;
}

void Func() {
	Func1();
	MyClass myClass;
	myClass.Func();

	return;
}