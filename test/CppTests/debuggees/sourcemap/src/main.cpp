#ifdef _WIN32
#include "writer\writer.h"
#include "manager\manager.h"
#else
#include "writer/writer.h"
#include "manager/manager.h"
#endif

int main(int argv, char** argc)
{
    Writer writer;

    writer.Write("Hello World");
    Manager mgr;

    for (int i = 90; i > 0; i -= 10)
    {
        mgr.AddInt(i);
    }

    writer.Write("Mgr has ");
    writer.Write(mgr.Size());
    writer.WriteLine(" items.");

    writer.Write("The sum of Mgr is: ");
    writer.WriteLine(mgr.Sum());
    
    writer.Write("Removing ");
    writer.Write(mgr.RemoveInt());
    writer.Write(". The sum is now: ");
    writer.Write(mgr.Sum());
    return 0;
}