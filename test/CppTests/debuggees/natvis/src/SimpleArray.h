#include <stdlib.h>

class SimpleArray
{
public:
  SimpleArray(int size)
  {
    _array = (int *)calloc(size, sizeof(int));
    _size = size;
  }

  int operator [] (int i) const {
    return _array[i];
  }
  int& operator [] (int i) {
    return _array[i];
  }

  int* _array;
  int  _size;
};