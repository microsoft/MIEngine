#include <cstddef>

class ConditionalLinkedList
{
private:
    struct Node {
        int data;
        Node* next;
        Node(int value) : data(value), next(NULL) {}
    };

    Node* head;
    int numElements;
    bool isActive;

public:
    ConditionalLinkedList(bool active) : head(NULL), numElements(0), isActive(active) {}

    void Add(int val) {
        Node* n = new Node(val);
        n->next = head;
        head = n;
        numElements++;
    }
};
