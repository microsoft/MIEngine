using Xunit;
using Microsoft.MIDebugEngine.Natvis;

namespace MIDebugEngineUnitTests
{
    /// <summary>
    /// Unit tests for <see cref="Natvis.StripFormatSpecifier"/>,
    /// <see cref="Natvis.ExtractFormatSpecifier"/>,
    /// <see cref="Natvis.CleanUtf16StringValue"/> and
    /// <see cref="Natvis.CleanAsciiStringValue"/>.
    /// </summary>
    public class NatvisFormatSpecifierTest
    {
        // -- no specifier -----------------------------------------------------

        [Fact]
        public void StripFormatSpecifier_NoSpecifier_Unchanged()
        {
            Assert.Equal("cspec == 1", Natvis.StripFormatSpecifier("cspec == 1"));
        }

        [Fact]
        public void StripFormatSpecifier_Empty_Unchanged()
        {
            Assert.Equal("", Natvis.StripFormatSpecifier(""));
        }

        // -- simple specifiers ------------------------------------------------

        [Fact]
        public void StripFormatSpecifier_Sub_Stripped()
        {
            Assert.Equal("schemeStr()", Natvis.StripFormatSpecifier("schemeStr(),sub"));
        }

        [Fact]
        public void StripFormatSpecifier_Decimal_Stripped()
        {
            Assert.Equal("year()", Natvis.StripFormatSpecifier("year(),d"));
        }

        [Fact]
        public void StripFormatSpecifier_HexBytes_Stripped()
        {
            Assert.Equal("data1", Natvis.StripFormatSpecifier("data1,Xb"));
        }

        [Fact]
        public void StripFormatSpecifier_NoVoidOmitXBytes_Stripped()
        {
            Assert.Equal("(data4[0])", Natvis.StripFormatSpecifier("(data4[0]),nvoXb"));
        }

        // -- comma inside parentheses is NOT a specifier boundary -------------

        [Fact]
        public void StripFormatSpecifier_CommaInsideParens_Unchanged()
        {
            // No top-level comma; the commas inside sizeof(...) are at depth > 0
            // and must not be treated as a specifier boundary.
            Assert.Equal(
                "sizeof(QAtomicInt) + sizeof(int)",
                Natvis.StripFormatSpecifier("sizeof(QAtomicInt) + sizeof(int)"));
        }

        [Fact]
        public void StripFormatSpecifier_FunctionCallWithArgs_OnlySpecifierStripped()
        {
            // memberOffset(0),sub: comma inside parens is depth>0, top-level comma is the specifier
            Assert.Equal("memberOffset(0)", Natvis.StripFormatSpecifier("memberOffset(0),sub"));
        }

        [Fact]
        public void StripFormatSpecifier_NestedParens_OnlySpecifierStripped()
        {
            Assert.Equal(
                "(msecs() % (24 * 60 * 60 * 1000ull))/(10 * 60 * 60 * 1000ull)",
                Natvis.StripFormatSpecifier("(msecs() % (24 * 60 * 60 * 1000ull))/(10 * 60 * 60 * 1000ull),d"));
        }

        // -- view specifier (contains parens) ---------------------------------

        [Fact]
        public void StripFormatSpecifier_ViewSpecifier_Stripped()
        {
            // {this,view(RecZone)na}: the specifier starts at the last top-level comma
            Assert.Equal("this", Natvis.StripFormatSpecifier("this,view(RecZone)na"));
        }

        // -- trailing whitespace trimmed --------------------------------------

        [Fact]
        public void StripFormatSpecifier_TrailingWhitespace_Trimmed()
        {
            Assert.Equal("year()", Natvis.StripFormatSpecifier("year() ,d"));
        }

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
        public void CleanAsciiStringValue_AddressStripped_QuotesKept()
        {
            Assert.Equal("\"Hello World\"", Natvis.CleanAsciiStringValue("0x00007fff5fbff6c0 \"Hello World\""));
        }

        [Fact]
        public void CleanAsciiStringValue_NoAddress_Unchanged()
        {
            Assert.Equal("\"Hello\"", Natvis.CleanAsciiStringValue("\"Hello\""));
        }

        [Fact]
        public void CleanAsciiStringValue_TruncatedNoClosingQuote_PrefixStripped()
        {
            Assert.Equal("\"Hello...", Natvis.CleanAsciiStringValue("0x00007fff \"Hello..."));
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
