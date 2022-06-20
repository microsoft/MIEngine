#include <cstddef>

class SimpleLinkedList
{
private:
  class Node {
  public:
    int data;
    Node* next;

    Node(int value) {
      data = value;
    }
  };

  Node *head = NULL;
  Node *tail = NULL;
  int numElements;

public:
  SimpleLinkedList()
  {
    numElements = 0;
  }

  void AddTail(int val) {
    if (tail == NULL)
    {
      tail = new Node(val);
      head = tail;
    }
    else 
    {
      Node* n = new Node(val);
      tail->next = n;
      tail = tail->next;
    }
    numElements++;
  }

  int Get(int idx)
  {
    Node* cur = head;
    while (cur != NULL && idx >= 0)
    {
      if (idx == 0)
      {
        return cur->data;
      }
      idx--;
      cur = cur->next;
    }
    return -1;
  }
};