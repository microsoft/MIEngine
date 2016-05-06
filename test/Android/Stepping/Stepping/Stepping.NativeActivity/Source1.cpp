
#include "Header1.h"
#include "Header2.h"

void func1() {
	func2();
	func2();

	return;
}

void func2() {
	int x = 0;
	int y = 1;

	func3();

	return;
}