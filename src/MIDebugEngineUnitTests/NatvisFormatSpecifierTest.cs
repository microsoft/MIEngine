using Xunit;
using Microsoft.MIDebugEngine.Natvis;

namespace MIDebugEngineUnitTests
{
    /// <summary>
    /// Unit tests for <see cref="Natvis.ExtractFormatSpecifier"/>,
    /// <see cref="Natvis.CleanUtf16StringValue"/> and
    /// <see cref="Natvis.CleanAsciiStringValue"/>.
    /// </summary>
    public class NatvisFormatSpecifierTest
    {
        // -- ExtractFormatSpecifier -------------------------------------------

        [Fact]
        public void ExtractFormatSpecifier_Sub_Extracted()
        {
            Assert.Equal("sub", Natvis.ExtractFormatSpecifier("schemeStr(),sub"));
        }

        [Fact]
        public void ExtractFormatSpecifier_Decimal_Extracted()
        {
            Assert.Equal("d", Natvis.ExtractFormatSpecifier("year(),d"));
        }

        [Fact]
        public void ExtractFormatSpecifier_NoSpecifier_ReturnsNull()
        {
            Assert.Null(Natvis.ExtractFormatSpecifier("cspec == 1"));
        }

        [Fact]
        public void ExtractFormatSpecifier_NvoModifierStripped()
        {
            // "nvoXb": strip "nvo" modifier, result is "Xb"
            Assert.Equal("Xb", Natvis.ExtractFormatSpecifier("data1,nvoXb"));
        }

        [Fact]
        public void ExtractFormatSpecifier_NaModifierStripped()
        {
            // "view(RecZone)na": strip "na", result is "view(RecZone)"
            Assert.Equal("view(RecZone)", Natvis.ExtractFormatSpecifier("this,view(RecZone)na"));
        }

        [Fact]
        public void ExtractFormatSpecifier_ViewSpecifierNoModifier_Extracted()
        {
            // View specifier with no trailing modifier (e.g. no "na")
            Assert.Equal("view(arr)", Natvis.ExtractFormatSpecifier("foo(),view(arr)"));
        }

        // -- IsIncludeViewMatch -----------------------------------------------

        [Fact]
        public void IsIncludeViewMatch_NullIncludeView_AlwaysMatches()
        {
            Assert.True(Natvis.IsIncludeViewMatch(null, "RecZone"));
            Assert.True(Natvis.IsIncludeViewMatch(null, null));
        }

        [Fact]
        public void IsIncludeViewMatch_EmptyIncludeView_AlwaysMatches()
        {
            Assert.True(Natvis.IsIncludeViewMatch("", "RecZone"));
        }

        [Fact]
        public void IsIncludeViewMatch_MatchingView_ReturnsTrue()
        {
            Assert.True(Natvis.IsIncludeViewMatch("RecZone", "RecZone"));
        }

        [Fact]
        public void IsIncludeViewMatch_DifferentView_ReturnsFalse()
        {
            Assert.False(Natvis.IsIncludeViewMatch("RecZone", "other"));
        }

        [Fact]
        public void IsIncludeViewMatch_NullCurrentView_ReturnsFalse()
        {
            Assert.False(Natvis.IsIncludeViewMatch("RecZone", null));
        }

        [Fact]
        public void IsIncludeViewMatch_MultipleViews_MatchesAny()
        {
            // IncludeView is a semicolon-delimited list per the natvis XSD — same as ExcludeView.
            Assert.True(Natvis.IsIncludeViewMatch("RecZone;RecZoneAbs", "RecZone"));
            Assert.True(Natvis.IsIncludeViewMatch("RecZone;RecZoneAbs", "RecZoneAbs"));
            Assert.False(Natvis.IsIncludeViewMatch("RecZone;RecZoneAbs", "other"));
        }

        // -- IsExcludeViewMatch -----------------------------------------------

        [Fact]
        public void IsExcludeViewMatch_NullExcludeView_ReturnsFalse()
        {
            Assert.False(Natvis.IsExcludeViewMatch(null, "RecZone"));
        }

        [Fact]
        public void IsExcludeViewMatch_NullCurrentView_ReturnsFalse()
        {
            Assert.False(Natvis.IsExcludeViewMatch("RecZone;RecZoneAbs", null));
        }

        [Fact]
        public void IsExcludeViewMatch_ViewInList_ReturnsTrue()
        {
            Assert.True(Natvis.IsExcludeViewMatch("RecZone;RecZoneAbs", "RecZone"));
            Assert.True(Natvis.IsExcludeViewMatch("RecZone;RecZoneAbs", "RecZoneAbs"));
        }

        [Fact]
        public void IsExcludeViewMatch_ViewNotInList_ReturnsFalse()
        {
            Assert.False(Natvis.IsExcludeViewMatch("RecZone;RecZoneAbs", "other"));
        }

        [Fact]
        public void IsExcludeViewMatch_SingleEntry_Matches()
        {
            Assert.True(Natvis.IsExcludeViewMatch("simple", "simple"));
        }

        // -- ExtractViewName --------------------------------------------------

        [Fact]
        public void ExtractViewName_ViewSpecifier_ReturnsName()
        {
            Assert.Equal("RecZone", Natvis.ExtractViewName("view(RecZone)"));
        }

        [Fact]
        public void ExtractViewName_ViewSpecifierWithTrailingNa_ReturnsName()
        {
            Assert.Equal("RecZone", Natvis.ExtractViewName("view(RecZone)na"));
        }

        [Fact]
        public void ExtractViewName_ShortName_ReturnsName()
        {
            Assert.Equal("arr", Natvis.ExtractViewName("view(arr)"));
        }

        [Fact]
        public void ExtractViewName_NotViewSpecifier_ReturnsNull()
        {
            Assert.Null(Natvis.ExtractViewName("d"));
            Assert.Null(Natvis.ExtractViewName("sub"));
        }

        [Fact]
        public void ExtractViewName_Null_ReturnsNull()
        {
            Assert.Null(Natvis.ExtractViewName(null));
        }

        [Fact]
        public void ExtractViewName_EmptyViewName_ReturnsNull()
        {
            // view() with no name is not a valid specifier — treat as absent.
            Assert.Null(Natvis.ExtractViewName("view()"));
        }

        // -- CleanUtf16StringValue --------------------------------------------

        [Fact]
        public void CleanUtf16StringValue_AddressAndQuotes_Stripped()
        {
            Assert.Equal("Hello World", Natvis.CleanUtf16StringValue("0x00007fff5fbff6c0 u\"Hello World\""));
        }

        [Fact]
        public void CleanUtf16StringValue_NoAddress_QuotesStripped()
        {
            Assert.Equal("Hello", Natvis.CleanUtf16StringValue("u\"Hello\""));
        }

        [Fact]
        public void CleanUtf16StringValue_UpperCaseU_QuotesStripped()
        {
            Assert.Equal("Hello", Natvis.CleanUtf16StringValue("U\"Hello\""));
        }

        [Fact]
        public void CleanUtf16StringValue_TruncatedNoClosingQuote_PrefixStripped()
        {
            Assert.Equal("Hello...", Natvis.CleanUtf16StringValue("0x00007fff u\"Hello..."));
        }

        [Fact]
        public void CleanUtf16StringValue_Empty_ReturnsEmpty()
        {
            Assert.Equal("", Natvis.CleanUtf16StringValue(""));
        }

        [Fact]
        public void CleanUtf16StringValue_NoPrefix_Unchanged()
        {
            Assert.Equal("42", Natvis.CleanUtf16StringValue("42"));
        }

        // -- CleanAsciiStringValue --------------------------------------------

        [Fact]
        public void CleanAsciiStringValue_AddressAndQuotes_Stripped()
        {
            Assert.Equal("Hello World", Natvis.CleanAsciiStringValue("0x00007fff5fbff6c0 \"Hello World\""));
        }

        [Fact]
        public void CleanAsciiStringValue_NoAddress_QuotesStripped()
        {
            Assert.Equal("Hello", Natvis.CleanAsciiStringValue("\"Hello\""));
        }

        [Fact]
        public void CleanAsciiStringValue_TruncatedNoClosingQuote_PrefixAndOpenQuoteStripped()
        {
            Assert.Equal("Hello...", Natvis.CleanAsciiStringValue("0x00007fff \"Hello..."));
        }

        [Fact]
        public void CleanAsciiStringValue_Empty_ReturnsEmpty()
        {
            Assert.Equal("", Natvis.CleanAsciiStringValue(""));
        }

        [Fact]
        public void CleanAsciiStringValue_NoPrefix_Unchanged()
        {
            Assert.Equal("42", Natvis.CleanAsciiStringValue("42"));
        }

        // -- SubstituteLocalVars ---------------------------------------------

        [Fact]
        public void SubstituteLocalVars_EmptyExpression_Unchanged()
        {
            Assert.Equal("", Natvis.SubstituteLocalVars("", new System.Collections.Generic.Dictionary<string, string> { ["ptr"] = "head" }));
        }

        [Fact]
        public void SubstituteLocalVars_NoVars_Unchanged()
        {
            Assert.Equal("ptr->next", Natvis.SubstituteLocalVars("ptr->next", new System.Collections.Generic.Dictionary<string, string>()));
        }

        [Fact]
        public void SubstituteLocalVars_SingleVar_Substituted()
        {
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["ptr"] = "m_head" };
            Assert.Equal("(m_head)->next", Natvis.SubstituteLocalVars("ptr->next", vars));
        }

        [Fact]
        public void SubstituteLocalVars_WordBoundary_PartialNameNotReplaced()
        {
            // "ptrr" must not be replaced when the variable is "ptr"
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["ptr"] = "m_head" };
            Assert.Equal("ptrr->next", Natvis.SubstituteLocalVars("ptrr->next", vars));
        }

        [Fact]
        public void SubstituteLocalVars_MultipleVars_BothSubstituted()
        {
            var vars = new System.Collections.Generic.Dictionary<string, string>
            {
                ["lo"] = "start",
                ["hi"] = "end"
            };
            Assert.Equal("(start) + (end)", Natvis.SubstituteLocalVars("lo + hi", vars));
        }

        [Fact]
        public void SubstituteLocalVars_ParensPreservesPrecedence()
        {
            // Replacement is wrapped in () so "ptr->val * 2" becomes "((m_head)->val) * 2"
            // after two substitution passes aren't needed here — single pass is enough.
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["ptr"] = "m_head" };
            Assert.Equal("(m_head)->val", Natvis.SubstituteLocalVars("ptr->val", vars));
        }

        // -- ApplyExecToLocalVars --------------------------------------------

        [Fact]
        public void ApplyExecToLocalVars_SimpleAssignment_UpdatesVar()
        {
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["ptr"] = "m_head" };
            Natvis.ApplyExecToLocalVars("ptr = ptr->next", vars);
            Assert.Equal("(m_head)->next", vars["ptr"]);
        }

        [Fact]
        public void ApplyExecToLocalVars_UnknownLhs_NoChange()
        {
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["ptr"] = "m_head" };
            Natvis.ApplyExecToLocalVars("other = 0", vars);
            // "other" is not a declared variable; dict should be unchanged
            Assert.Equal("m_head", vars["ptr"]);
            Assert.False(vars.ContainsKey("other"));
        }

        [Fact]
        public void ApplyExecToLocalVars_EmptyExpression_NoChange()
        {
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["ptr"] = "m_head" };
            Natvis.ApplyExecToLocalVars("", vars);
            Assert.Equal("m_head", vars["ptr"]);
        }

        [Fact]
        public void ApplyExecToLocalVars_CounterIncrement_UpdatesVar()
        {
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["i"] = "0" };
            Natvis.ApplyExecToLocalVars("i = i + 1", vars);
            Assert.Equal("(0) + 1", vars["i"]);
        }

        // Assignment must not match == comparison operator (regression guard)
        [Fact]
        public void ApplyExecToLocalVars_EqualityComparison_NoChange()
        {
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["i"] = "5" };
            Natvis.ApplyExecToLocalVars("i == 1", vars);
            Assert.Equal("5", vars["i"]);
        }

        // -- ApplyExecToLocalVars — increment/decrement -----------------------

        [Fact]
        public void ApplyExecToLocalVars_PrefixIncrement_UpdatesVar()
        {
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["i"] = "3" };
            Natvis.ApplyExecToLocalVars("++i", vars);
            Assert.Equal("(3) + 1", vars["i"]);
        }

        [Fact]
        public void ApplyExecToLocalVars_PostfixIncrement_UpdatesVar()
        {
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["i"] = "3" };
            Natvis.ApplyExecToLocalVars("i++", vars);
            Assert.Equal("(3) + 1", vars["i"]);
        }

        [Fact]
        public void ApplyExecToLocalVars_PrefixDecrement_UpdatesVar()
        {
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["i"] = "3" };
            Natvis.ApplyExecToLocalVars("--i", vars);
            Assert.Equal("(3) - 1", vars["i"]);
        }

        [Fact]
        public void ApplyExecToLocalVars_PostfixDecrement_UpdatesVar()
        {
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["i"] = "3" };
            Natvis.ApplyExecToLocalVars("i--", vars);
            Assert.Equal("(3) - 1", vars["i"]);
        }

        [Fact]
        public void ApplyExecToLocalVars_IncrUnknownVar_NoChange()
        {
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["i"] = "3" };
            Natvis.ApplyExecToLocalVars("++j", vars);
            Assert.Equal("3", vars["i"]);
            Assert.False(vars.ContainsKey("j"));
        }

        [Fact]
        public void ApplyExecToLocalVars_IncrWithSpaces_UpdatesVar()
        {
            // Whitespace around the operator/operand must be tolerated.
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["i"] = "0" };
            Natvis.ApplyExecToLocalVars("  i++  ", vars);
            Assert.Equal("(0) + 1", vars["i"]);
        }

        // -- FormatCustomListItemName ----------------------------------------

        [Fact]
        public void FormatCustomListItemName_NullTemplate_ReturnsBracketedIndex()
        {
            Assert.Equal("[0]",  Natvis.FormatCustomListItemName(null, 0,  new System.Collections.Generic.Dictionary<string, string>()));
            Assert.Equal("[42]", Natvis.FormatCustomListItemName(null, 42, new System.Collections.Generic.Dictionary<string, string>()));
        }

        [Fact]
        public void FormatCustomListItemName_BracedDollarI_ReplacedWithIndex()
        {
            Assert.Equal("[7]", Natvis.FormatCustomListItemName("[{$i}]", 7, new System.Collections.Generic.Dictionary<string, string>()));
        }

        [Fact]
        public void FormatCustomListItemName_BareDollarI_ReplacedWithIndex()
        {
            Assert.Equal("item_3", Natvis.FormatCustomListItemName("item_$i", 3, new System.Collections.Generic.Dictionary<string, string>()));
        }

        [Fact]
        public void FormatCustomListItemName_NoSpecialTokens_Unchanged()
        {
            Assert.Equal("key", Natvis.FormatCustomListItemName("key", 5, new System.Collections.Generic.Dictionary<string, string>()));
        }

        [Fact]
        public void FormatCustomListItemName_LocalVar_Substituted()
        {
            // A local variable name that appears in the Name template is substituted.
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["node"] = "m_head" };
            Assert.Equal("[(m_head)]", Natvis.FormatCustomListItemName("[node]", 0, vars));
        }

        [Fact]
        public void FormatCustomListItemName_ExprToken_FallsBackToIndex()
        {
            // {expr} tokens that survive local-var substitution require debugger evaluation,
            // which is not available here.  The method must fall back to [index] rather than
            // surfacing the raw expression text (or a debugger error string) as the child name.
            var vars = new System.Collections.Generic.Dictionary<string, string> { ["iSpan"] = "0" };
            // After substituting iSpan, "[{getKey((0), 0)}]" still contains '{' -- fall back.
            Assert.Equal("[2]", Natvis.FormatCustomListItemName("[{getKey(iSpan, 0)}]", 2, vars));
        }
    }
}
