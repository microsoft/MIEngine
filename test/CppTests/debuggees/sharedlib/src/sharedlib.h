#pragma once

#include <iostream>
#include <string>

using namespace std;

#ifdef WIN32_SHARED_LIBRARY
// *********** Win32 Shared Library Implementation *************

#include <windows.h>

typedef HINSTANCE LIBRARY_HANDLE;

LIBRARY_HANDLE OpenLibrary(string libraryName)
{
    return LoadLibraryA(libraryName.c_str());
}

bool CloseLibrary(LIBRARY_HANDLE library)
{
    return FreeLibrary(library) == TRUE;
}

void* GetLibraryFunction(LIBRARY_HANDLE library, string functionName)
{
    return (void*)GetProcAddress(library, functionName.c_str());
}

void LogLibraryError(string location)
{
    cout << "Error in " << location << ": " << GetLastError() << endl;
}

#else
// *********** POSIX Shared Library Implementation *************

#include <dlfcn.h>
typedef void* LIBRARY_HANDLE;

LIBRARY_HANDLE OpenLibrary(string libraryName)
{
    return dlopen(libraryName.c_str(), RTLD_LAZY);
}

bool CloseLibrary(LIBRARY_HANDLE library)
{
    return dlclose(library) == 0;
}

void* GetLibraryFunction(LIBRARY_HANDLE library, string functionName)
{
    return dlsym(library, functionName.c_str());
}

void LogLibraryError(string location)
{
    cout << "Error in " << location << ": " << dlerror() << endl;
}


#endif
