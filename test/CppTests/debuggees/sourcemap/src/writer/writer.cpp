#include <iostream>
#include "writer.h"

Writer::Writer()
{ }

void Writer::Write(const std::string msg)
{
    std::cout << msg << std::endl;
}

void Writer::WriteLine(const std::string msg)
{
    this->Write(msg);
    std::cout << std::endl;
}

void Writer::Write(const int number)
{
    std::cout << number;
}

void Writer::WriteLine(const int number)
{
    std::cout << "Writing number: " << number << std::endl;
}