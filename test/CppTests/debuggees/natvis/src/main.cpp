#include "SimpleLinkedList.h"
#include "BinarySearchTree.h"
#include "SimpleVector.h"
#include "SimpleArray.h"
#include "SimpleClass.h"

class SimpleDisplayObject
{
    public:
        SimpleDisplayObject(){}
};

int main(int argc, char** argv)
{
    SimpleDisplayObject obj_1;

    SimpleVector vec(10);
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

    SimpleArray arr(15);
    for (int i = 0 ; i < 15; i++)
    {
        arr[i] = i * i;
    }

    SimpleArrayPointer arrPointer(arr);

    SimpleClass *simpleClass = nullptr;    
    simpleClass = new SimpleClass();

    return 0;
}