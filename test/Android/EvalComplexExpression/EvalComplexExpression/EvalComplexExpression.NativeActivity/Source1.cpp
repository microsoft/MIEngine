
#include "Header1.h"
#include <string>
using namespace std;

void Func(int x) {
	int _x = x;
	_x++;
	return;
}

int Func(int x, double y) {
	int _y = (int)y;
	return x + y;
}

void Func() {
	int x = 0;
	double dx = 1.50;
	string str = "hello, world";

	return;
}