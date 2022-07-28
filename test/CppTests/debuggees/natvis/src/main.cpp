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

    SimpleVector vec(52);
    vec.Set(5, 20);

    SimpleLinkedList ll;

    for (int i = 0; i < 100; i++)
    {
        ll.AddTail(i);
    }

    int data[100] = {
        86, -471, 230, -205, -198, 299, 187, 96, -346, 121,
        -334, -390, -105, -250, -279, -478, 35, 16, -21, 323,
        315, 78, 461, -418, 497, 113, 234, 472, 370, -187,
        -282, -23, 32, -314, -25, 215, 153, 37, -50, 254,
        331, 141, 44, 392, -43, 449, 467, 364, 427, 92,
        498, 375, 475, -153, 6, 490, 390, -381, 348, 412,
        -305, -172, 336, -387, -197, -19, 446, 62, 30, -489,
        -309, 369, -148, 435, -362, 433, -388, 200, -278, -166,
        -481, -119, -350, -399, 294, -120, 429, -139, -349, -437,
        -368, 368, 27, -35, 123, 243, 17, 76, -6, 485
    };

    BinarySearchTree map;
    for (int i = 0; i < 100; i++)
    {
        map.Insert(data[i]);
    }

    SimpleArray arr(152);
    for (int i = 0 ; i < 152; i++)
    {
        arr[i] = i * i;
    }

    SimpleClass *simpleClass = nullptr;    
    simpleClass = new SimpleClass();

    return 0;
}