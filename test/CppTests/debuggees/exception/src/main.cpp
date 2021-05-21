#include "exception.h"
#include <iostream>
#include <string>

using namespace std;

int main(int argc, char* argv[])
{
	cout << "Start testing" << endl;

	bool isUnhandledException = false;

	bool isHandledException = false;

	bool isReThrowException = false;

	if (argc > 1)
	{
		for (int i = 1; i < argc; i++)
		{
			string input = argv[i];
			if (input.compare("-CallRaisedUnhandledException") == 0)
			{
				isUnhandledException = true;
			}
			else if (input.compare("-CallRaisedHandledException") == 0)
			{
				isHandledException = true;
			}
			else if (input.compare("-CallRaisedReThrowException") == 0)
			{
				isReThrowException = true;
			}
		}
	}

	int myVar1 = 100;

	int myVar2 = 200;

	int mySum = myVar1 + myVar2;

	myException *pInstance = new myException();

	int result = pInstance->RecursiveFunc(mySum);

	if (isHandledException)
	{
		pInstance->RaisedHandledException(myVar1);
	}

	if (isUnhandledException)
	{
		pInstance->RaisedUnhandledException(myVar2);
	}

	pInstance->EvalFunc(myVar1, myVar2);

	if (isReThrowException)
	{
		pInstance->RaisedReThrowException();
	}

	if (pInstance != NULL)
	{
		delete pInstance;
	}

	cout << "Finish testing" << endl;

	return 0;
}