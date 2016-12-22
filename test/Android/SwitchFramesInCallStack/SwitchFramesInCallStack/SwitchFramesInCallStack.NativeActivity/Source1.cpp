
#include "Header1.h"
#include "Header2.h"

bool func3(int *x, int *y) {
	++x;
	++y;
	
	func6(func7, func8);

	return x == y;
}

int func2(int x, int y) {
	++x;
	--y;

	func3(&x, &y);

	return x + y;
}

void func1() {
	int x = 10, y = 20;

	func2(x, y);

	return;
}
