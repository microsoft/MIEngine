
#include "Header1.h"
#include <thread>

void *func1(void *) {
	func2(1);
}

void func2(int x) {
	func3(2, 3);

	return;
}

void func3(int x, int y) {
	int z = x + y;
}

void func4() {
	func5();

	return;
}

void func5() {
	int x = 0;

	return;
}

void func() {
	func4();

	// Start a new thread
	pthread_t pt1;
	pthread_create(&pt1, NULL, func1, 0);
	pthread_join(pt1, NULL);

	return;
}