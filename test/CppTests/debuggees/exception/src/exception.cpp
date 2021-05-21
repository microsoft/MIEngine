#include "exception.h"

int myException::RaisedUnhandledException(int myvar)
{
	int result = 10;
	int temp = -1;
	temp++;
	myvar = result / temp;
	result = EvalFunc(myvar, myvar);
	result++;
	return result;
}

int myException::RaisedHandledException(int a)
{
	int global = 100;
	int result = 0;
	try
	{
		global++;
		RecursiveFunc(global);
		if (result == 0)
		{
			throw newException(global);
		}
		else
		{
			result = global / result;
		}
	}
	catch (newException ex)
	{
		result = a + global;
		int errorCode = ex.code;
		global++;
	}
	return result;
}

int myException::EvalFunc(int var1, int var2)
{
	int result = var1 + var2;
	return result;
}

int myException::RecursiveFunc(int a)
{
	if (a == 0)
	{
		return 1;
	}
	else
	{
		return RecursiveFunc(a - 1);
	}
}

void myException::RaisedThrowNewException()
{
	throw newException(200);
}

void myException::RaisedReThrowException()
{
	int var = 100;
	try
	{
		try
		{
			var = var - 100;
			if (var == 0)
			{
				RaisedThrowNewException();
			}
		}
		catch (newException ex1)
		{
			int errorCode = ex1.code;
			var = EvalFunc(errorCode, errorCode);
			throw newException(var);
		}
	}
	catch (newException ex2)
	{
		int errorCode = ex2.code;
		var = 0;
	}
}

newException::newException(int c)
{
	code = c;
}

newException::~newException()
{
	code = 0;
}