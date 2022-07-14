#include <stdlib.h>
#include "SimpleArray.h"

class SimpleArrayPointer
{
public:
    SimpleArrayPointer(SimpleArray simpleArray)
    {
        _simpleArray = simpleArray;
    }

    int[] operator () (int i) const {
        int ret[i];
        for (int pos = 0; pos < i; pos++) {
            ret[pos] = _simpleArray[pos];
        }
        return ret;
    }

    SimpleArray* _simpleArray;
};