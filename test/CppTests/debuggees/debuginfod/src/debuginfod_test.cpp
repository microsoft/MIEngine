// Test program that calls into library functions where debuginfod would
// attempt to download debug symbols/source. When stepping through regex
// or libc internals, GDB triggers debuginfod lookups if enabled.

#include <iostream>
#include <regex>
#include <string>

int do_regex_match(const std::string& input, const std::string& pattern)
{
    std::regex re(pattern);
    std::smatch match;
    if (std::regex_search(input, match, re))
    {
        std::cout << "Match: " << match[0] << std::endl;
        return 1; // breakpoint line
    }
    return 0;
}

int main()
{
    std::string text = "Hello debuginfod test 12345";
    std::string pattern = R"(\d+)";

    int result = do_regex_match(text, pattern); // step into here
    std::cout << "Result: " << result << std::endl;
    return result;
}
