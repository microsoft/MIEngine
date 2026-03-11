#include "SimpleLinkedList.h"
#include "BinarySearchTree.h"
#include "SimpleVector.h"
#include "SimpleArray.h"
#include "SimpleClass.h"
#include "SimpleMatrix.h"
#include "SimpleTemplated.h"

class SimpleDisplayObject
{
public:
    SimpleDisplayObject() {}
};

int main(int argc, char** argv)
{
    SimpleDisplayObject obj_1;

    SimpleVector vec(2000);
    for (int i = 0; i < 2000; i++)
    {
        vec.Set(i, i);
    }
    vec.Set(5, 20);

    SimpleLinkedList ll;

    for (int i = 0; i < 100; i++)
    {
        ll.AddTail(i);
    }

    BinarySearchTree map;
    map.Insert(0);
    map.Insert(-100);
    map.Insert(15);
    map.Insert(-35);
    map.Insert(4);
    map.Insert(-72);

    SimpleArray arr(52);
    for (int i = 0; i < 52; i++)
    {
        arr[i] = i * i;
    }

    SimpleClass* simpleClass = nullptr;
    simpleClass = new SimpleClass();

    SimpleMatrix matrix(2, 256);

    SimpleContainer<int> container(42, 10);
    SimplePair<int, double> pair(1, 3.14);
    SimpleMap<double, char, int> genericMap(2.71, 'x', 5);
    SimpleMap<int, double, int> intKeyMap(7, 1.618, 3);

    return 0;
}
