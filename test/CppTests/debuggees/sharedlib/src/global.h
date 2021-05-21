#pragma once

#define STRINGIFY2( x) #x
#define STRINGIFY(x) STRINGIFY2(x)

#ifdef _MINGW
    #define WIN32_SHARED_LIBRARY
#endif

#ifdef _WIN32
    #define WIN32_SHARED_LIBRARY
#endif
