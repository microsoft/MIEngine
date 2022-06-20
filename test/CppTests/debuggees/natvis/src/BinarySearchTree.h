#include <cstddef>

class BinarySearchTree
{
private:
  class Node {
  public:
    int data;
    Node* left;
    Node* right;

    Node(int value)
    {
      data = value;
      left = NULL;
      right = NULL;
    }
  };

  Node *root = NULL;
  int numElements;

public:
  BinarySearchTree()
  {
    numElements = 0;
  }

  void Insert(int value)
  {
    root = InsertNode(root, value);
    numElements++;
  }

  Node* InsertNode(Node *node, int value)
  {
    if (node == NULL)
    {
      return new Node(value);
    }
    else
    {
      if (node->data > value)
      {
        node->left = InsertNode(node->left, value);
      }
      else
      {
        node->right = InsertNode(node->right, value);
      }
      return node;
    }
  }
};