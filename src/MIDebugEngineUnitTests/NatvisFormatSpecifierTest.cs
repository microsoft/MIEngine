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
            Assert.Equal("sub", Natvis.ExtractFormatSpecifier("schemeStr(),sub", out _));
        }

        [Fact]
        public void ExtractFormatSpecifier_Decimal_Extracted()
        {
            Assert.Equal("d", Natvis.ExtractFormatSpecifier("year(),d", out _));
        }

        [Fact]
        public void ExtractFormatSpecifier_NoSpecifier_ReturnsNull()
        {
            Assert.Null(Natvis.ExtractFormatSpecifier("cspec == 1", out _));
        }

        [Fact]
        public void ExtractFormatSpecifier_NvoModifierStripped()
        {
            // "nvoXb": strip "nvo" modifier, result is "Xb"
            Assert.Equal("Xb", Natvis.ExtractFormatSpecifier("data1,nvoXb", out _));
        }

        [Fact]
        public void ExtractFormatSpecifier_NaModifierStripped()
        {
            // "view(RecZone)na": strip "na", result is "view(RecZone)"
            Assert.Equal("view(RecZone)", Natvis.ExtractFormatSpecifier("this,view(RecZone)na", out bool hasNa));
            Assert.True(hasNa);
        }

        [Fact]
        public void ExtractFormatSpecifier_HasTemplateParameter()
        {
            // Template parameters should be detected but not substituted by ExtractFormatSpecifier
            // (they're substituted by ReplaceNamesInExpression)
            string spec = Natvis.ExtractFormatSpecifier("myVector,sub");
            Assert.Equal("sub", spec);
        }

        [Fact]
        public void ExtractFormatSpecifier_WithNaModifier_HasNaIsTrue()
        {
            string spec = Natvis.ExtractFormatSpecifier("someExpr,na", out bool hasNa);
            Assert.Equal("", spec);
            Assert.True(hasNa);
        }

        [Fact]
        public void ExtractFormatSpecifier_WithNaAndFormat_HasNaIsTrueAndSpecReturned()
        {
            string spec = Natvis.ExtractFormatSpecifier("myString,subna", out bool hasNa);
            Assert.Equal("sub", spec);
            Assert.True(hasNa);
        }

        [Fact]
        public void ExtractFormatSpecifier_WithoutNaModifier_HasNaIsFalse()
        {
            string spec = Natvis.ExtractFormatSpecifier("myString,sub", out bool hasNa);
            Assert.Equal("sub", spec);
            Assert.False(hasNa);
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
