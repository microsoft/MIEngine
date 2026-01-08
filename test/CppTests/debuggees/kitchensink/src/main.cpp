// NOTE THAT CHANGING THE LINE NUMBERS FOR EXISTING SOURCE CODE WILL REQUIRE CHANGING OTHER TESTS
// SEE https://devdiv.visualstudio.com/DevDiv/_git/OpenDebugAD7/pullrequest/37021 for an example

#include "main.h"
#include "arguments.h"
#include "nonterminating.h"
#include "calling.h"
#include "threading.h"
#include "expression.h"
#include "environment.h"
#include "inheritance.h"

using namespace std;

int main(int argc, char *argv[])
{
    cout << "KitchenSink for Business! (" << STRINGIFY(DEBUGGEE_COMPILER) << "/" << ARCH << " Edition)" << endl;

    Arguments argumentsFeature;
    argumentsFeature.argc = argc;
    argumentsFeature.argv = argv;
    argumentsFeature.Run();

    if (argumentsFeature.runNonTerminating == true)
    {
        // This feature just runs in an infinite loop.
        NonTerminating nonTerminatingFeature;
        nonTerminatingFeature.Run();
    }

    if (argumentsFeature.runCalling == true)
    {
        Calling callingFeature;
        callingFeature.Run();
    }

    if (argumentsFeature.runThreading == true)
    {
        Threading threadingFeature;
        threadingFeature.Run();
    }

    if (argumentsFeature.runExpression == true)
    {
        Expression expressionFeature;
        expressionFeature.Run();
    }

    if (argumentsFeature.runEnvironment == true)
    {
        Environment environmentFeature;;
        environmentFeature.Run();
    }

    if (argumentsFeature.runInheritance == true)
    {
        Inheritance inheritanceFeature;
        inheritanceFeature.Run();
    }

    cout << "Goodbye.\n";
    return 0;
}