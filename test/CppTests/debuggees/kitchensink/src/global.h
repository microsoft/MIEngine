#pragma once

#define STRINGIFY2( x) #x
#define STRINGIFY(x) STRINGIFY2(x)

#if DEBUGGEE_ARCH==32
    #define ARCH "x86"
#elif DEBUGGEE_ARCH==64
    #define ARCH "x64"
#elif DEBUGGEE_ARCH==arm
    #define ARCH "arm"
#else
    #define ARCH "Unknown"
#endif
