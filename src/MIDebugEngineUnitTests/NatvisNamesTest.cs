using System;
using System.Collections.Generic;
using Xunit;

using Microsoft.MIDebugEngine.Natvis;
using MICore;
using Xunit.Abstractions;
using System.Globalization;
using Microsoft.DebugEngineHost;

namespace MIDebugEngineUnitTests
{
    class TestLogger : ILogChannel
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

        internal void RegisterTestOutputHelper(ITestOutputHelper output)
        {
            _output = output;
        }

        public void WriteLine(LogLevel level, string line)
        {
            _output?.WriteLine(line);
        }

        public void WriteLine(LogLevel level, string format, params object[] args)
        {
            _output?.WriteLine(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        // Unused as ITestOutputHelper does not have a Flush implementation
        public void Flush() { }

        // Unused as ITestOutputHelper does not have a Close implementation
        public void Close() { }

        // Unused as ITestOutputHelper does not have LogLevels
        public void SetLogLevel(LogLevel level) { }
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

        [Fact]
        public void FindBestMatch_SelectsMostSpecificWildcard()
        {
            var typeName = TypeName.Parse("std::tuple<int, int, int>", TestLogger.Instance);

            var oneWild = TypeName.Parse("std::tuple<*>", TestLogger.Instance);
            var twoWild = TypeName.Parse("std::tuple<*,*>", TestLogger.Instance);
            var threeWild = TypeName.Parse("std::tuple<*,*,*>", TestLogger.Instance);

            var candidates = new List<TypeName> { oneWild, twoWild, threeWild };

            var best = Natvis.FindBestMatch(candidates, typeName, c => c);

            Assert.NotNull(best);
            Assert.Same(threeWild, best);
        }

        [Fact]
        public void FindBestMatch_ReturnsNullWhenNoMatch()
        {
            var typeName = TypeName.Parse("std::vector<int>", TestLogger.Instance);
            var pattern = TypeName.Parse("std::map<*>", TestLogger.Instance);

            var candidates = new List<TypeName> { pattern };

            var best = Natvis.FindBestMatch(candidates, typeName, c => c);

            Assert.Null(best);
        }

        [Fact]
        public void FindBestMatch_ExactArgCountWinsOverFewerWildcards()
        {
            var typeName = TypeName.Parse("MyType<int, double>", TestLogger.Instance);

            var oneWild = TypeName.Parse("MyType<*>", TestLogger.Instance);
            var twoWild = TypeName.Parse("MyType<*,*>", TestLogger.Instance);

            var candidates = new List<TypeName> { oneWild, twoWild };

            var best = Natvis.FindBestMatch(candidates, typeName, c => c);

            Assert.NotNull(best);
            Assert.Same(twoWild, best);
        }

        [Fact]
        public void FindBestMatch_OverspecifiedPatternDoesNotMatch()
        {
            var typeName = TypeName.Parse("MyType<int, double>", TestLogger.Instance);

            var threeWild = TypeName.Parse("MyType<*,*,*>", TestLogger.Instance);

            var candidates = new List<TypeName> { threeWild };

            var best = Natvis.FindBestMatch(candidates, typeName, c => c);

            Assert.Null(best);
        }

        [Fact]
        public void FindBestMatch_SingleCandidateReturnsIt()
        {
            var typeName = TypeName.Parse("std::tuple<int>", TestLogger.Instance);
            var oneWild = TypeName.Parse("std::tuple<*>", TestLogger.Instance);

            var candidates = new List<TypeName> { oneWild };

            var best = Natvis.FindBestMatch(candidates, typeName, c => c);

            Assert.NotNull(best);
            Assert.Same(oneWild, best);
        }

        [Fact]
        public void FindBestMatch_ConcreteArgBeatsWildcardAtSameCount()
        {
            // T<int, *> should beat T<*, *> for type T<int, double>
            var typeName = TypeName.Parse("MyType<int, double>", TestLogger.Instance);

            var allWild = TypeName.Parse("MyType<*,*>", TestLogger.Instance);
            var oneConcreteOneWild = TypeName.Parse("MyType<int,*>", TestLogger.Instance);

            // Place the less-specific pattern first to ensure it doesn't win by ordering
            var candidates = new List<TypeName> { allWild, oneConcreteOneWild };

            var best = Natvis.FindBestMatch(candidates, typeName, c => c);

            Assert.NotNull(best);
            Assert.Same(oneConcreteOneWild, best);
        }

        [Fact]
        public void FindBestMatch_FullyConcreteBeatsAllWildcards()
        {
            // T<int, double> (exact) should beat T<*, *> for type T<int, double>
            var typeName = TypeName.Parse("MyType<int, double>", TestLogger.Instance);

            var allWild = TypeName.Parse("MyType<*,*>", TestLogger.Instance);
            var exact = TypeName.Parse("MyType<int,double>", TestLogger.Instance);

            var candidates = new List<TypeName> { allWild, exact };

            var best = Natvis.FindBestMatch(candidates, typeName, c => c);

            Assert.NotNull(best);
            Assert.Same(exact, best);
        }

        [Fact]
        public void FindBestMatch_MoreArgCountBeatsMoreConcreteWithFewerArgs()
        {
            // MyType<*,*,*> (3 args, 0 concrete) should beat MyType<int,*> (2 args, 1 concrete)
            // because arg count is the primary metric, even when the fewer-arg pattern has more concrete args
            var typeName = TypeName.Parse("MyType<int, double, float>", TestLogger.Instance);

            var oneConcreteOneWild = TypeName.Parse("MyType<int,*>", TestLogger.Instance);
            var threeWild = TypeName.Parse("MyType<*,*,*>", TestLogger.Instance);

            var candidates = new List<TypeName> { oneConcreteOneWild, threeWild };

            var best = Natvis.FindBestMatch(candidates, typeName, c => c);

            Assert.NotNull(best);
            Assert.Same(threeWild, best);
        }

        [Fact]
        public void FindBestMatch_FewerArgPatternWithConcreteMatchesWhenTrailingWildcard()
        {
            // Pattern <*, *, int, *> has 4 args but type has 5.
            // The trailing wildcard absorbs the extra arg, and the concrete int at position 3 must match.
            // This mirrors a natvis like: TaskQueue<*, *, MyScheduler, *> matching
            // TaskQueue<Task, HighPriority, MyScheduler, StdAlloc, FileLogger>
            var typeName = TypeName.Parse("TaskQueue<int, double, int, double, double>", TestLogger.Instance);

            var fewerArgsWithConcrete = TypeName.Parse("TaskQueue<*,*,int,*>", TestLogger.Instance);

            var candidates = new List<TypeName> { fewerArgsWithConcrete };

            var best = Natvis.FindBestMatch(candidates, typeName, c => c);

            Assert.NotNull(best);
            Assert.Same(fewerArgsWithConcrete, best);
        }

        [Fact]
        public void FindBestMatch_FewerArgPatternWithConcreteMismatchDoesNotMatch()
        {
            // Pattern <*, *, int, *> should NOT match when position 3 is char, not int.
            var typeName = TypeName.Parse("TaskQueue<int, double, char, double, double>", TestLogger.Instance);

            var fewerArgsWithConcrete = TypeName.Parse("TaskQueue<*,*,int,*>", TestLogger.Instance);

            var candidates = new List<TypeName> { fewerArgsWithConcrete };

            var best = Natvis.FindBestMatch(candidates, typeName, c => c);

            Assert.Null(best);
        }

        [Fact]
        public void IsPrecededByMemberAccessOperator_DotOperator()
        {
            // "foo.bar" – identifier "bar" at index 4
            Assert.True(Natvis.IsPrecededByMemberAccessOperator("foo.bar", 4));
        }

        [Fact]
        public void IsPrecededByMemberAccessOperator_ArrowOperator()
        {
            // "ptr->member" – identifier "member" at index 5
            Assert.True(Natvis.IsPrecededByMemberAccessOperator("ptr->member", 5));
        }

        [Fact]
        public void IsPrecededByMemberAccessOperator_ScopeResolution()
        {
            // "ns::Class" – identifier "Class" at index 4
            Assert.True(Natvis.IsPrecededByMemberAccessOperator("ns::Class", 4));
        }

        [Fact]
        public void IsPrecededByMemberAccessOperator_DotWithWhitespace()
        {
            // "foo . bar" – identifier "bar" at index 6
            Assert.True(Natvis.IsPrecededByMemberAccessOperator("foo . bar", 6));
        }

        [Fact]
        public void IsPrecededByMemberAccessOperator_ArrowWithWhitespace()
        {
            // "ptr -> member" – identifier "member" at index 7
            Assert.True(Natvis.IsPrecededByMemberAccessOperator("ptr -> member", 7));
        }

        [Fact]
        public void IsPrecededByMemberAccessOperator_ScopeResolutionWithWhitespace()
        {
            // "ns :: Class" – identifier "Class" at index 6
            Assert.True(Natvis.IsPrecededByMemberAccessOperator("ns :: Class", 6));
        }

        [Fact]
        public void IsPrecededByMemberAccessOperator_IdentifierAtStart()
        {
            // "bar" – identifier at start of string, index 0
            Assert.False(Natvis.IsPrecededByMemberAccessOperator("bar", 0));
        }

        [Fact]
        public void IsPrecededByMemberAccessOperator_NoOperator()
        {
            // "foo + bar" – identifier "bar" at index 6, preceded by '+'
            Assert.False(Natvis.IsPrecededByMemberAccessOperator("foo + bar", 6));
        }

        [Fact]
        public void IsPrecededByMemberAccessOperator_PrecededByOpenParen()
        {
            // "(bar" – identifier "bar" at index 1
            Assert.False(Natvis.IsPrecededByMemberAccessOperator("(bar", 1));
        }

        [Fact]
        public void IsPrecededByMemberAccessOperator_SingleColonIsNotOperator()
        {
            // "a:b" – single colon is not a scope-resolution operator
            Assert.False(Natvis.IsPrecededByMemberAccessOperator("a:b", 2));
        }

        [Fact]
        public void IsPrecededByMemberAccessOperator_SingleDashIsNotArrow()
        {
            // "a-b" – single dash without '>' is not an arrow operator
            Assert.False(Natvis.IsPrecededByMemberAccessOperator("a-b", 2));
        }
    }
}