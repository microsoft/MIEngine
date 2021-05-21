#include <string>

using namespace std;

class myBase
{
public:
    virtual int DisplayAge(int age) = 0;
    virtual string DisplayName(string firstName, string lastName) = 0;
};
