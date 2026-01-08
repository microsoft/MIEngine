#pragma once
#include "feature.h"

// Base class
class Animal
{
protected:
    const char* name;
    int age;

public:
    Animal(const char* n);
    void setAge(int a);
    const char* getName() const;
    int getAge() const;
};

// Simple inheritance - Dog
class Dog : public Animal
{
private:
    const char* breed;
    bool isGoodBoy;
    int barkCount = 0;

public:
    Dog(const char* n, const char* b);
    void bark();
    const char* getBreed() const;
};

// Simple inheritance - Cat
class Cat : public Animal
{
private:
    int lives;
    bool isIndoor;
    int meowCount = 0;

public:
    Cat(const char* n, int l);
    void meow();
    int getLives() const;
};

// Simple inheritance - Bird
class Bird : public Animal
{
private:
    double wingSpan;
    bool canFly;
    int chirpCount = 0;

public:
    Bird(const char* n, double ws);
    void chirp();
    double getWingSpan() const;
};

// Multi-level inheritance base
class Mammal : public Animal
{
protected:
    bool hasFur;
    double bodyTemp;

public:
    Mammal(const char* n, bool f);
    bool getHasFur() const;
};

// Multi-level inheritance derived
class Pet : public Mammal
{
private:
    const char* owner;
    bool isVaccinated;

public:
    Pet(const char* n, bool f, const char* o);
    const char* getOwner() const;
};

// Multiple inheritance
class FlyingMammal : public Mammal
{
private:
    double wingspan;

public:
    FlyingMammal(const char* n, double ws);
    double getWingspan() const;
};

// Namespace for template classes
namespace Animals {
    // Template base class
    template<typename T>
    class Container
    {
    protected:
        T data;
        int capacity;

    public:
        Container(T d, int cap) : data(d), capacity(cap) {}
        T getData() const { return data; }
        int getCapacity() const { return capacity; }
    };

    // Template derived class
    template<typename T>
    class AnimalContainer : public Container<T>
    {
    private:
        const char* location;
        bool isSecure;

    public:
        AnimalContainer(T d, int cap, const char* loc) 
            : Container<T>(d, cap), location(loc), isSecure(true) {}
        const char* getLocation() const { return location; }
    };
}

// Feature class for testing
class Inheritance : public Feature
{
private:
    void testSimpleInheritance();
    void testMultiLevelInheritance();
    void testMultipleInheritance();
    void testPolymorphism();
    void testTemplateInheritance();

public:
    Inheritance();
    void CoreRun() override;
};
