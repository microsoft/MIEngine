#include "foo.h"
#include <iostream>
#include <string>
using namespace std;

Foo::Foo()
{
	cout << "default constructor called" << endl;
	this->number = 10;
	FillContainer();
}

Foo::Foo(int number)
{
	cout << "one-argument constructor called" << endl;
	if (Validate(number))
	{
		this->number = number;
		FillContainer();
	}
	else
	{
		this->number = 10;
		FillContainer();
	}
}

bool Foo::Validate(int number)
{
	if (number <= 0 || number>100)
	{
		return false;
	}
	else
	{
		return true;
	}
}

void Foo::FillContainer()
{
	for (int i = 1; i <= this->number; ++i)
	{
		m_collection.push_back(i);
	}
}

int Foo::Sum()
{
	vector<int>::const_iterator iterBegin = m_collection.begin();
	vector<int>::const_iterator iterEnd = m_collection.end();
	int sum = 0;
	int first = m_collection[0];
	cout << first << endl;

	while (iterBegin != iterEnd)
	{
		sum += *iterBegin;
		++iterBegin;
	}

	return sum;
}