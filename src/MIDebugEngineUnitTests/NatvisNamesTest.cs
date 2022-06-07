using Xunit;

using Microsoft.MIDebugEngine.Natvis;
using MICore;
using Xunit.Abstractions;

namespace MIDebugEngineUnitTests
{
    class TestLogger : ILogger
    {
        private static TestLogger s_instance;

        public static TestLogger Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new TestLogger();
                }

                return s_instance;
            }
        }

        private ITestOutputHelper _output;

        private TestLogger() {}

        internal void RegisterTestOutputHelper(ITestOutputHelper output)
        {
            _output = output;
        }

        public void WriteLine(string line)
        {
            _output?.WriteLine(line);
        }
    }

    public class NatvisNamesTest
    {
        public NatvisNamesTest(ITestOutputHelper output)
        {
            TestLogger.Instance.RegisterTestOutputHelper(output);
        }


        [Fact]
        public void TestPrimitiveParse()
        {
            Assert.NotNull(TypeName.Parse("bool", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("char", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("int", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("float", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("double", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("void", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("wchar_t", TestLogger.Instance));
        }

        [Fact]
        public void TestModifiedPrimitiveParse()
        {
            Assert.NotNull(TypeName.Parse("unsigned char", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("signed char", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("signed int", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("unsigned int", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("short int", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("signed short int", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("unsigned short int", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("long int", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("signed long int", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("unsigned long int", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("long long int", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("long double", TestLogger.Instance));
        }

        [Fact]
        public void TestComplexParse()
        {
            Assert.NotNull(TypeName.Parse("std::vector<int>", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("std::map<int, int>", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("std::map<char, int>::iterator", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("std::is_base_of<std::input_iterator_tag, tag>::value", TestLogger.Instance));
            Assert.NotNull(TypeName.Parse("mpl::if_<T, std::true_type, std::false_type>", TestLogger.Instance));
        }
    }
}