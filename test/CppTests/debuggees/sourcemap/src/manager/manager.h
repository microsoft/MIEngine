#include <vector>
class Manager
{
public:
    Manager();
    void AddInt(int value);
    int RemoveInt();
    int Size();
    int Sum();
private: 
    std::vector<int> items;
};