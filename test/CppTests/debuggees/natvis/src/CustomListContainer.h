#include <cstddef>

// A simple NULL-terminated singly-linked list, visualized via <CustomListItems>
// (the loop engine: <Variable>/<Loop>/<Break>/<Item>/<Exec>). Nodes are appended
// to the tail so that iteration order (head -> next) matches insertion order.
class CustomListContainer
{
private:
    struct Node {
        int value;
        Node* next;
        Node(int v) : value(v), next(NULL) {}
    };

    Node* head;
    int count;

public:
    CustomListContainer() : head(NULL), count(0) {}

    void Append(int v) {
        Node* n = new Node(v);
        if (head == NULL) {
            head = n;
        } else {
            Node* cur = head;
            while (cur->next != NULL) cur = cur->next;
            cur->next = n;
        }
        count++;
    }
};
