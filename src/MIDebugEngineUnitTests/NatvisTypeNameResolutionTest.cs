using Xunit;
using Microsoft.MIDebugEngine.Natvis;

namespace MIDebugEngineUnitTests
{
    /// <summary>
    /// Unit tests for <see cref="Natvis.ExtractResolvedTypeName"/> and
    /// <see cref="Natvis.StripTypeDecorations"/> (typedef / base-class name
    /// resolution used by the FindType fallback).
    /// </summary>
    public class NatvisTypeNameResolutionTest
    {
        // -- StripTypeDecorations ----------------------------------------------

        [Fact]
        public void StripTypeDecorations_ConstRef_Stripped()
        {
            Assert.Equal("TextItemList", Natvis.StripTypeDecorations("const TextItemList &"));
        }

        [Fact]
        public void StripTypeDecorations_Pointer_Stripped()
        {
            Assert.Equal("TextItemList", Natvis.StripTypeDecorations("TextItemList *"));
        }

        [Fact]
        public void StripTypeDecorations_PlainName_Unchanged()
        {
            Assert.Equal("RawItemList", Natvis.StripTypeDecorations("RawItemList"));
        }

        [Fact]
        public void StripTypeDecorations_TemplateName_Unchanged()
        {
            Assert.Equal("ItemList<TextItem>", Natvis.StripTypeDecorations("ItemList<TextItem>"));
        }

        // -- ExtractResolvedTypeName: GDB whatis --------------------------------

        [Fact]
        public void Extract_GdbWhatisTypedef_ReturnsTarget()
        {
            Assert.Equal("ItemList<ItemValue>",
                Natvis.ExtractResolvedTypeName("type = ItemList<ItemValue>", "ItemValueList"));
        }

        [Fact]
        public void Extract_GdbWhatisEchoesName_ReturnsNull()
        {
            // whatis of a class echoes the class name back — no new information.
            Assert.Null(Natvis.ExtractResolvedTypeName("type = TextItemList", "TextItemList"));
        }

        // -- ExtractResolvedTypeName: GDB ptype ---------------------------------

        [Fact]
        public void Extract_GdbPtypeSubclass_ReturnsFirstBase()
        {
            string output = "type = class TextItemList : public ItemList<TextItem> {\n  public:\n  ...\n}";
            Assert.Equal("ItemList<TextItem>", Natvis.ExtractResolvedTypeName(output, "TextItemList"));
        }

        [Fact]
        public void Extract_GdbPtypeTypedefExpanded_ReturnsHeadName()
        {
            // ptype of a typedef expands to the underlying class definition.
            string output = "type = class ItemList<ItemValue> {\n  public:\n  ...\n}";
            Assert.Equal("ItemList<ItemValue>", Natvis.ExtractResolvedTypeName(output, "ItemValueList"));
        }

        [Fact]
        public void Extract_GdbPtypeNoBase_ReturnsNull()
        {
            string output = "type = class MyPlainType {\n  int x;\n}";
            Assert.Null(Natvis.ExtractResolvedTypeName(output, "MyPlainType"));
        }

        // -- ExtractResolvedTypeName: LLDB type lookup --------------------------

        [Fact]
        public void Extract_LldbTypedef_ReturnsTarget()
        {
            Assert.Equal("ItemList<ItemValue>",
                Natvis.ExtractResolvedTypeName("typedef ItemList<ItemValue>", "ItemValueList"));
        }

        [Fact]
        public void Extract_LldbClassWithBase_ReturnsFirstBase()
        {
            string output = "class TextItemList : public ItemList<TextItem> {\n  ...\n}";
            Assert.Equal("ItemList<TextItem>", Natvis.ExtractResolvedTypeName(output, "TextItemList"));
        }

        [Fact]
        public void Extract_LldbAliasExpandedWithItsOwnBase_ReturnsHeadNotBase()
        {
            // lldb resolves an alias by printing the underlying class definition —
            // including THAT class's own base and a "template<>" prefix. The head
            // name is the resolution; the base clause belongs to the resolved type.
            string output = "template<> class ItemList<TextItem> : public ListSpecial<TextItem> {\n    typedef Something DataPointer;\n}";
            Assert.Equal("ItemList<TextItem>", Natvis.ExtractResolvedTypeName(output, "TextItemList"));
        }

        [Fact]
        public void Extract_CStyleTypedefWithAliasSuffix_ReturnsTarget()
        {
            // "typedef <target> <alias>" — the trailing alias must be stripped.
            Assert.Equal("ItemList<ItemValue>",
                Natvis.ExtractResolvedTypeName("typedef ItemList<ItemValue> ItemValueList", "ItemValueList"));
        }

        // -- ExtractResolvedTypeName: base-clause parsing edge cases ------------

        [Fact]
        public void Extract_MultipleBases_ReturnsFirstOnly()
        {
            string output = "class Derived : public BaseA<int, long>, protected BaseB {";
            Assert.Equal("BaseA<int, long>", Natvis.ExtractResolvedTypeName(output, "Derived"));
        }

        [Fact]
        public void Extract_VirtualPublicBase_KeywordsStripped()
        {
            string output = "class Derived : virtual public Base {";
            Assert.Equal("Base", Natvis.ExtractResolvedTypeName(output, "Derived"));
        }

        [Fact]
        public void Extract_ScopedBaseName_ScopeOperatorNotMistakenForBaseClause()
        {
            string output = "class Derived : public ns::Base {";
            Assert.Equal("ns::Base", Natvis.ExtractResolvedTypeName(output, "Derived"));
        }

        [Fact]
        public void Extract_TemplateArgsContainingScopes_Parsed()
        {
            string output = "class Wrapper : public std::vector<std::string> {";
            Assert.Equal("std::vector<std::string>", Natvis.ExtractResolvedTypeName(output, "Wrapper"));
        }

        [Fact]
        public void Extract_GdbTemplateSubclassWithClause_ParamsSubstitutedInBase()
        {
            // GDB prints template types with a "[with ...]" substitution clause and
            // leaves the base clause UNsubstituted (verified against gdb 16.3).
            string output = "type = class ItemStack<int> [with T = int] : public ItemList<T> {\n  ...\n}";
            Assert.Equal("ItemList<int>", Natvis.ExtractResolvedTypeName(output, "ItemStack<int>"));
        }

        [Fact]
        public void Extract_GdbWithClauseMultipleParams_AllSubstituted()
        {
            string output = "type = class PairLike<int, long> [with K = int, V = long] : public MapBase<K, V> {";
            Assert.Equal("MapBase<int, long>", Natvis.ExtractResolvedTypeName(output, "PairLike<int, long>"));
        }

        [Fact]
        public void Extract_GdbVirtualBaseKeywordOrder_Stripped()
        {
            // gdb emits "public virtual", lldb emits "virtual public" — both must strip.
            string output = "type = class Derived : public virtual Base {";
            Assert.Equal("Base", Natvis.ExtractResolvedTypeName(output, "Derived"));
        }

        [Fact]
        public void Extract_GdbWithClauseNonTypeParam_Substituted()
        {
            // Non-type template parameters appear in the with-clause as values too.
            string output = "type = class FixedArr<int, 4> [with T = int, N = 4] : public ArrBase<T, N> {";
            Assert.Equal("ArrBase<int, 4>", Natvis.ExtractResolvedTypeName(output, "FixedArr<int, 4>"));
        }

        [Fact]
        public void Extract_GdbVirtualBaseCombinedWithClause_Substituted()
        {
            // Keyword stripping and with-clause substitution must compose.
            string output = "type = class ItemStack<int> [with T = int] : public virtual ItemList<T> {";
            Assert.Equal("ItemList<int>", Natvis.ExtractResolvedTypeName(output, "ItemStack<int>"));
        }

        [Fact]
        public void Extract_StructKeyword_Handled()
        {
            string output = "type = struct DerivedS : public BaseS {";
            Assert.Equal("BaseS", Natvis.ExtractResolvedTypeName(output, "DerivedS"));
        }

        [Fact]
        public void Extract_TypeNamedTemplateHelper_NotTreatedAsTemplatePrefix()
        {
            // A type genuinely named "template_helper" must not have its name eaten
            // by the template<...> prefix stripping.
            string output = "class template_helper : public Base {";
            Assert.Equal("Base", Natvis.ExtractResolvedTypeName(output, "template_helper"));
        }

        [Fact]
        public void Extract_ScopedTypedefTargetEndingWithAlias_TargetKept()
        {
            // The alias suffix is only stripped as a whole whitespace-separated token:
            // a target that merely ends with the alias name must stay intact.
            Assert.Equal("ns::ItemValueList",
                Natvis.ExtractResolvedTypeName("typedef ns::ItemValueList ItemValueList", "ItemValueList"));
        }

        // -- ExtractResolvedTypeName: degenerate inputs -------------------------

        [Fact]
        public void Extract_EmptyOrWhitespace_ReturnsNull()
        {
            Assert.Null(Natvis.ExtractResolvedTypeName(null, "X"));
            Assert.Null(Natvis.ExtractResolvedTypeName("", "X"));
            Assert.Null(Natvis.ExtractResolvedTypeName("   \n  ", "X"));
        }

        [Fact]
        public void Extract_ErrorText_ReturnsNullOrHarmless()
        {
            // A "no type found" style message must not be mistaken for a type name
            // that could loop; it differs from the queried name, so the caller's
            // TypeName.Parse / Scan will simply find no rule for it.
            Assert.Null(Natvis.ExtractResolvedTypeName("type = MyPlainType", "MyPlainType"));
        }

        [Fact]
        public void Extract_ErrorPrefixedOutput_ReturnsNull()
        {
            // "error: ..." has a top-level colon; without the guard, "error" would
            // parse as a head name and waste a follow-up query.
            Assert.Null(Natvis.ExtractResolvedTypeName("error: use of undeclared identifier 'x'", "Foo"));
            Assert.Null(Natvis.ExtractResolvedTypeName("warning: something", "Foo"));
        }

        [Fact]
        public void Extract_LldbNoTypeFoundMessage_ReturnsNull()
        {
            // Debugger error prose must not come back as a "resolved type name".
            string output = "no type was found in the current language c++14 matching 'Foo'; performing a global search across all languages";
            Assert.Null(Natvis.ExtractResolvedTypeName(output, "Foo"));
        }
    }
}
