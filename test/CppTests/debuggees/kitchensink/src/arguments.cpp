#include "arguments.h"

Arguments::Arguments()
    : Feature("Arguments")
{
}

void Arguments::CoreRun()
{
    this->Log("Parsing Arguments.");

    if (this->argc > 1)
    {
        this->Log("Count: ", this->argc - 1);
        for (int i = 0; i < this->argc - 1; i++)
        {
            char* arg = this->argv[i + 1];
            std::string argStr(arg);
            this->Log("Arg ", i + 1, ": ", argStr);

            if (argStr.compare("-fCalling") == 0)
            {
                this->runCalling = true;
            }

            if (argStr.compare("-fThreading") == 0)
            {
                this->runThreading = true;
            }

            if (argStr.compare("-fNonTerminating") == 0)
            {
                this->runNonTerminating = true;
            }

            if (argStr.compare("-fExpression") == 0)
            {
                this->runExpression = true;
            }

            if (argStr.compare("-fEnvironment") == 0)
            {
                this->runEnvironment = true;
            }
        }
    }
}