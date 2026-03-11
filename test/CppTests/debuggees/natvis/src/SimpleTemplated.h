// Simplified container types used to test that the natvis visualizer
// picks the most specific wildcard match, modeled after real STL patterns.

// Like std::vector<T> — single template parameter container
template<typename T>
class SimpleContainer
{
public:
    SimpleContainer(T val, int count) : _value(val), _size(count) {}
    T _value;
    int _size;
};

// Like std::pair<K, V> — two template parameters
template<typename K, typename V>
class SimplePair
{
public:
    SimplePair(K key, V value) : _key(key), _value(value) {}
    K _key;
    V _value;
};

// Like std::map<Key, Value, Compare> — three template parameters where
// a specialized visualizer for a concrete key type should win over
// a generic all-wildcard visualizer.
template<typename Key, typename Value, typename Compare>
class SimpleMap
{
public:
    SimpleMap(Key k, Value v, int count) : _key(k), _value(v), _size(count) {}
    Key _key;
    Value _value;
    int _size;
};
