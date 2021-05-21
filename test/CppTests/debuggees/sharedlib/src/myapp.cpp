#include "global.h"
#include "sharedlib.h"
#include "mylib_base.h"
#include <iostream>
#include <string>

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

int main(int argc, char* argv[])
{
    cout << "Start testing" << endl;

    string firstName = "Richard";

    string lastName = "Zeng";

    OpenMyLibrary();

    myBase* myclass = create();

    myclass->DisplayAge(30);

    myclass->DisplayName(firstName, lastName);

    destroy(myclass);

    CloseMyLibrary();

    cout << "Finish testing" << endl;

    return 0;
}
