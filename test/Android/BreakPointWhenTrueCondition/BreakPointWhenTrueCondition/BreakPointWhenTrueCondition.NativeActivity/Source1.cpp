
#include "Header1.h"

int Func1() {
	int i = 1;
	int sum = 0;
	MyClass myClass;
	myClass.isTrue = true;

	for (; i < 10; i++) {
		sum = sum + i;
	}

	return sum;
}

void Func() {
	Func1();

	return;
}