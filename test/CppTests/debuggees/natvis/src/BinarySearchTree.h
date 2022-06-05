#include <cstddef>

class BinarySearchTree
{
private:
  class Node {
  public:
    int data;
    Node* left;
    Node* right;

    Node(int value) {
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

  Node* InsertNode(Node *root, int value)
  {
    if (root == NULL)
    {
      return new Node(value);
    }
    else
    {
      if (root->data > value)
      {
        root->left = InsertNode(root->left, value);
      }
      else
      {
        root->right = InsertNode(root->right, value);
      }
    }
  }
};