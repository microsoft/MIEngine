#include "environment.h"
#include <cstdlib>

Environment::Environment()
    : Feature("Environment")
{
}

void Environment::CoreRun()
{
    const char* varName1 = "VAR_NAME_1";
    this->Log("Getting environment variable: ", varName1);
    const char* varValue1 = std::getenv(varName1);
    this->Log("Obtained variable");
}
