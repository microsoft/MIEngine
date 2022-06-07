#include <stdlib.h>

class SimpleVector
{
public:
  SimpleVector(int size)
  {
    _start = (int *)calloc(size, sizeof(int));
    _size = size;
  }

  void Set(int idx, int value)
  {
    if (idx < 0 || idx >= _size)
    {
      return;
    }
    *(_start + idx) = value;
  }

  int* _start;
  int  _size;
};