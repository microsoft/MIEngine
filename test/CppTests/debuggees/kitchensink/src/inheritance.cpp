#include "inheritance.h"
#include <string>

Animal::Animal(const char* n) : name(n), age(0)
{
}

void Animal::setAge(int a)
{
    age = a;
}

const char* Animal::getName() const
{
    return name;
}

int Animal::getAge() const
{
    return age;
}

Dog::Dog(const char* n, const char* b) : Animal(n), breed(b), isGoodBoy(true)
{
}

void Dog::bark()
{
    barkCount++;
}

const char* Dog::getBreed() const
{
    return breed;
}

Cat::Cat(const char* n, int l) : Animal(n), lives(l), isIndoor(true)
{
}

void Cat::meow()
{
    meowCount++;
}

int Cat::getLives() const
{
    return lives;
}

Bird::Bird(const char* n, double ws) : Animal(n), wingSpan(ws), canFly(true)
{
}

void Bird::chirp()
{
    chirpCount++;
}

double Bird::getWingSpan() const
{
    return wingSpan;
}

Mammal::Mammal(const char* n, bool f) : Animal(n), hasFur(f), bodyTemp(37.0)
{
}

bool Mammal::getHasFur() const
{
    return hasFur;
}

Pet::Pet(const char* n, bool f, const char* o) : Mammal(n, f), owner(o), isVaccinated(false)
{
}

const char* Pet::getOwner() const
{
    return owner;
}

FlyingMammal::FlyingMammal(const char* n, double ws) : Mammal(n, true), wingspan(ws)
{
}

double FlyingMammal::getWingspan() const
{
    return wingspan;
}

void Inheritance::testSimpleInheritance()
{
    Dog dog("Buddy", "Golden Retriever");
    dog.setAge(3);
    dog.bark();
    dog.bark();

    Cat cat("Whiskers", 9);
    cat.setAge(5);
    cat.meow();

    Bird bird("Tweety", 15.5);
    bird.setAge(2);
    bird.chirp();

    int simpleBreakpoint = 1;
    this->testMultiLevelInheritance();
}

void Inheritance::testMultiLevelInheritance()
{
    // Test multi-level inheritance
    Mammal mammal("Generic", true);
    mammal.setAge(10);

    Pet pet("Fluffy", true, "Alice");
    pet.setAge(4);

    int multiLevelBreakpoint = 1;
    this->testMultipleInheritance();
}

void Inheritance::testMultipleInheritance()
{
    // Test multiple inheritance
    FlyingMammal bat("Batty", 25.0);
    bat.setAge(1);

    int multipleBreakpoint = 1;
    this->testPolymorphism();
}

void Inheritance::testPolymorphism()
{
    // Test polymorphism with base class pointers
    Dog dog1("Max", "Labrador");
    dog1.setAge(5);
    
    Cat cat1("Shadow", 7);
    cat1.setAge(3);

    // Base class pointers to derived objects
    Animal* animalPtr1 = &dog1;
    Animal* animalPtr2 = &cat1;

    int polymorphismBreakpoint = 1;
    this->testTemplateInheritance();
}

void Inheritance::testTemplateInheritance()
{
    Animals::Container<int> intContainer(42, 100);
    Animals::AnimalContainer<int> intAnimalContainer(99, 200, "Warehouse A");
    
    Animals::Container<double> doubleContainer(3.14, 50);
    Animals::AnimalContainer<double> doubleAnimalContainer(2.71, 75, "Lab B");

    int templateBreakpoint = 1;
}

void Inheritance::CoreRun()
{
    this->testSimpleInheritance();
}

Inheritance::Inheritance() : Feature("Inheritance")
{
}
