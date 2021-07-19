#pragma once
#include <vector>
using std::vector;

/*
Author : Williams Ma
Date   : 2016-7-24
Desc   : Encapsuate a class for suming of collection 
*/

class Foo
{
public:
	Foo();
	Foo(int num);
	int Sum();

private:
	bool Validate(int number);
	void FillContainer();
	int number;
	vector<int> m_collection;
};