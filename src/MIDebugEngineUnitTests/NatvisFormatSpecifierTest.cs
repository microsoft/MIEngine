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
    }
}
