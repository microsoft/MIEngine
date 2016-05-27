
#include "Header1.h"
#include "Header2.h"

int* func6(func4 f4, func5 f5) {
	f4();
	int ret = f5(1, 2);
	
	return &ret;
}

void func7() {
	int x = 0;

	return;
}

int func8(int x, int y) {
	return x + y;
}