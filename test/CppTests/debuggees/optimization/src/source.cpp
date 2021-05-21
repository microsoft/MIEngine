#include <iostream>
#include <string>
#include "mylib_base.h"
#include "foo.h"
#include "..//..//..//sharedlib//src//global.h"
#include "..//..//..//sharedlib//src//sharedlib.h"

using namespace std;

typedef myBase* p_create();
typedef void p_destroy(myBase*);

p_create* create;
p_destroy* destroy;

LIBRARY_HANDLE pHandle;

void OpenMyLibrary()
{
	string platform = STRINGIFY(DEBUGGEE_PLATFORM);
	create = NULL;
	destroy = NULL;

	string dllName = "mylib.dll";
	string soName = "./mylib.so";
	string libraryName = (platform.compare("WINDOWS") == 0) ? dllName : soName;
	pHandle = OpenLibrary(libraryName);
	if (!pHandle)
	{
		LogLibraryError("OpenLibrary");
		return;
	}

	create = (p_create*)GetLibraryFunction(pHandle, "Create");
	if (!create)
	{
		LogLibraryError("Get Create");
	}

	destroy = (p_destroy*)GetLibraryFunction(pHandle, "Destroy");
	if (!destroy)
	{
		LogLibraryError("Get Destroy");
	}
}

void CloseMyLibrary()
{
	create = NULL;
	destroy = NULL;

	if (pHandle != NULL)
	{
		bool closed = CloseLibrary(pHandle);
		if (!closed)
		{
			LogLibraryError("CloseLibarary");
		}
	}
}

int main()
{
	Foo * pFoo = new Foo();
	
	cout << "default sum: " << pFoo->Sum() << endl;

	pFoo = new Foo(10);
	cout << " new sum:" << pFoo->Sum() << endl;

	if (pFoo)
	{
		delete pFoo;
		pFoo = NULL;
	}

	cout << "Start testing" << endl;

	string firstName = "Richard";

	string lastName = "Zeng";

	OpenMyLibrary();

	myBase* myclass = create();

	int age = myclass->DisplayAge(30);

	myclass->DisplayName(firstName, lastName);

	destroy(myclass);

	CloseMyLibrary();

	cout << "Finish testing" << endl;

	return 0;
}