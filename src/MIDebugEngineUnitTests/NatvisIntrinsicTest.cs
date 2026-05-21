using System.Collections.Generic;
using Xunit;
using Microsoft.MIDebugEngine.Natvis;

namespace MIDebugEngineUnitTests
{
    /// <summary>
    /// Unit tests for the NatVis &lt;Intrinsic&gt; expansion logic:
    /// ResolveIntrinsicCalls and its helpers FindMatchingParen,
    /// SplitArguments, and SubstituteIntrinsicParameters.
    /// </summary>
    public class NatvisIntrinsicTest
    {
        // ── helpers ──────────────────────────────────────────────────────────

        private static IntrinsicType MakeIntrinsic(string name, string expression, params (string name, string type)[] parameters)
        {
            var intrinsic = new IntrinsicType();
            intrinsic.Name = name;
            intrinsic.Expression = expression;
            if (parameters.Length > 0)
            {
                var ps = new IntrinsicParameterType[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    ps[i] = new IntrinsicParameterType();
                    ps[i].Name = parameters[i].name;
                    ps[i].Type = parameters[i].type;
                }
                intrinsic.Parameter = ps;
            }
            return intrinsic;
        }

        private static Dictionary<string, IntrinsicType> Dict(params IntrinsicType[] intrinsics)
        {
            var d = new Dictionary<string, IntrinsicType>();
            foreach (var i in intrinsics)
                d[i.Name] = i;
            return d;
        }

        // ── FindMatchingParen ─────────────────────────────────────────────────

        [Fact]
        public void FindMatchingParen_SimpleCall()
        {
            // "foo(bar)" — open paren at 3, close at 7
            Assert.Equal(7, Natvis.FindMatchingParen("foo(bar)", 3));
        }

        [Fact]
        public void FindMatchingParen_NestedParens()
        {
            // "f(g(x))" — outer open at 1, outer close at 6
            Assert.Equal(6, Natvis.FindMatchingParen("f(g(x))", 1));
        }

        [Fact]
        public void FindMatchingParen_EmptyArgs()
        {
            // "f()" — open at 1, close at 2
            Assert.Equal(2, Natvis.FindMatchingParen("f()", 1));
        }

        [Fact]
        public void FindMatchingParen_Unmatched_ReturnsMinusOne()
        {
            Assert.Equal(-1, Natvis.FindMatchingParen("f(abc", 1));
        }

        // ── SplitArguments ────────────────────────────────────────────────────

        [Fact]
        public void SplitArguments_NoArgs_EmptyString()
        {
            // Empty string → no arguments. In practice ResolveIntrinsicCalls
            // guards with IsNullOrWhiteSpace before calling SplitArguments, so
            // zero-arg calls never reach it; but the helper itself should be consistent.
            var result = Natvis.SplitArguments("");
            Assert.Empty(result);
        }

        [Fact]
        public void SplitArguments_SingleArg()
        {
            var result = Natvis.SplitArguments("42");
            Assert.Equal(new[] { "42" }, result);
        }

        [Fact]
        public void SplitArguments_MultipleArgs()
        {
            var result = Natvis.SplitArguments("a, b, c");
            Assert.Equal(new[] { "a", "b", "c" }, result);
        }

        [Fact]
        public void SplitArguments_NestedParens_NotSplit()
        {
            // "f(a, b), c" — the comma inside f(...) is not a split point
            var result = Natvis.SplitArguments("f(a, b), c");
            Assert.Equal(new[] { "f(a, b)", "c" }, result);
        }

        [Fact]
        public void SplitArguments_ComparisonOperator_SplitsCorrectly()
        {
            // "a > 0, b" — '>' is a comparison operator, not a bracket; the comma is
            // at depth 0 and must be treated as a split point.
            // (Angle brackets are intentionally not tracked to avoid this ambiguity.)
            var result = Natvis.SplitArguments("a > 0, b");
            Assert.Equal(new[] { "a > 0", "b" }, result);
        }

        // ── SubstituteIntrinsicParameters ─────────────────────────────────────

        [Fact]
        public void SubstituteIntrinsicParameters_NoParameters_BodyUnchanged()
        {
            string result = Natvis.SubstituteIntrinsicParameters("jd + 1", null, new List<string>());
            Assert.Equal("jd + 1", result);
        }

        [Fact]
        public void SubstituteIntrinsicParameters_SingleParam()
        {
            var ps = new[] { new IntrinsicParameterType { Name = "count", Type = "int" } };
            string result = Natvis.SubstituteIntrinsicParameters("sizeof(int) * count", ps, new List<string> { "3" });
            Assert.Equal("sizeof(int) * 3", result);
        }

        [Fact]
        public void SubstituteIntrinsicParameters_WholeWordOnly()
        {
            // "val" should not be replaced inside "interval"
            var ps = new[] { new IntrinsicParameterType { Name = "val", Type = "int" } };
            string result = Natvis.SubstituteIntrinsicParameters("interval + val", ps, new List<string> { "99" });
            Assert.Equal("interval + 99", result);
        }

        [Fact]
        public void SubstituteIntrinsicParameters_MultipleParams()
        {
            var ps = new[]
            {
                new IntrinsicParameterType { Name = "a", Type = "int" },
                new IntrinsicParameterType { Name = "b", Type = "int" }
            };
            string result = Natvis.SubstituteIntrinsicParameters("a + b", ps, new List<string> { "1", "2" });
            Assert.Equal("1 + 2", result);
        }

        // ── ResolveIntrinsicCalls ─────────────────────────────────────────────

        [Fact]
        public void ResolveIntrinsicCalls_NullDict_ReturnsUnchanged()
        {
            string result = Natvis.ResolveIntrinsicCalls("day() + 1", null);
            Assert.Equal("day() + 1", result);
        }

        [Fact]
        public void ResolveIntrinsicCalls_EmptyDict_ReturnsUnchanged()
        {
            string result = Natvis.ResolveIntrinsicCalls("day() + 1", new Dictionary<string, IntrinsicType>());
            Assert.Equal("day() + 1", result);
        }

        [Fact]
        public void ResolveIntrinsicCalls_UnknownName_ReturnsUnchanged()
        {
            var dict = Dict(MakeIntrinsic("month", "jd / 30"));
            string result = Natvis.ResolveIntrinsicCalls("day() + 1", dict);
            Assert.Equal("day() + 1", result);
        }

        [Fact]
        public void ResolveIntrinsicCalls_ZeroArgIntrinsic()
        {
            // day() with no parameters
            var dict = Dict(MakeIntrinsic("day", "jd - 5"));
            string result = Natvis.ResolveIntrinsicCalls("day() + 1", dict);
            Assert.Equal("(jd - 5) + 1", result);
        }

        [Fact]
        public void ResolveIntrinsicCalls_ParametrizedIntrinsic()
        {
            // memberOffset(3) where Expression = "sizeof(int) * count", param count
            var dict = Dict(MakeIntrinsic("memberOffset", "sizeof(int) * count", ("count", "int")));
            string result = Natvis.ResolveIntrinsicCalls("memberOffset(3)", dict);
            Assert.Equal("(sizeof(int) * 3)", result);
        }

        [Fact]
        public void ResolveIntrinsicCalls_ChainedIntrinsics()
        {
            // year() = N() + 1, N() = jd / 2
            var dict = Dict(
                MakeIntrinsic("N", "jd / 2"),
                MakeIntrinsic("year", "N() + 1")
            );
            string result = Natvis.ResolveIntrinsicCalls("year()", dict);
            Assert.Equal("((jd / 2) + 1)", result);
        }

        [Fact]
        public void ResolveIntrinsicCalls_MemberAccessNotReExpanded()
        {
            // value() = _q_value.value() — after expansion the ".value()" must NOT
            // be re-expanded even though "value" is in the intrinsics dictionary.
            var dict = Dict(MakeIntrinsic("value", "_q_value.value()"));
            string result = Natvis.ResolveIntrinsicCalls("value()", dict);
            Assert.Equal("(_q_value.value())", result);
        }

        [Fact]
        public void ResolveIntrinsicCalls_ArrowAccessNotReExpanded()
        {
            // ptr->get() — "get" is an intrinsic but must not expand when after "->"
            var dict = Dict(MakeIntrinsic("get", "inner"));
            string result = Natvis.ResolveIntrinsicCalls("ptr->get()", dict);
            Assert.Equal("ptr->get()", result);
        }

        [Fact]
        public void ResolveIntrinsicCalls_ParametrizedChained()
        {
            // isEmpty(size) = size==0; hasScheme() = !isEmpty(scheme_size)
            var dict = Dict(
                MakeIntrinsic("isEmpty", "size==0", ("size", "int")),
                MakeIntrinsic("hasScheme", "!isEmpty(scheme_size)")
            );
            string result = Natvis.ResolveIntrinsicCalls("hasScheme()", dict);
            Assert.Equal("(!(scheme_size==0))", result);
        }

        [Fact]
        public void ResolveIntrinsicCalls_MultipleCallsInExpression()
        {
            var dict = Dict(
                MakeIntrinsic("x", "a + 1"),
                MakeIntrinsic("y", "b + 2")
            );
            string result = Natvis.ResolveIntrinsicCalls("x() * y()", dict);
            Assert.Equal("(a + 1) * (b + 2)", result);
        }

        [Fact]
        public void ResolveIntrinsicCalls_ExpressionWithNoCall_Unchanged()
        {
            var dict = Dict(MakeIntrinsic("day", "jd - 5"));
            // Expression references "day" but without (), so no expansion
            string result = Natvis.ResolveIntrinsicCalls("day + 1", dict);
            Assert.Equal("day + 1", result);
        }
    }
}
