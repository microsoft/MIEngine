
#include "Header1.h"

void func1() {
	int i = 0;
	int sum = 0;

	while (i < 5) {
		sum = sum + i;
		i++;
	}

	return;
}

void func2() {
	MyClass myClass;
	myClass.Func();

	return;
}

void MyClass::Func() {
	int i = 0;
	int sum = 0;

	while (i < 5) {
		sum = sum + i;
		i++;
	}

	return;
}

void func() {
	func1();
	func2();

	return;
}