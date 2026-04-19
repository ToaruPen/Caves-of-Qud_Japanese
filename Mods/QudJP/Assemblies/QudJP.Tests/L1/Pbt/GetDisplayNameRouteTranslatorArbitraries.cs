using FsCheck;
using FsCheck.Fluent;

namespace QudJP.Tests.L1.Pbt;

public sealed record DisplayNameExactCase(string Source, string Expected);

public sealed record DisplayNameTrimmedExactCase(string Source, string Expected);

public sealed record DisplayNameScopedConflictCase(string Source, string Expected);

public sealed record DisplayNameBracketedStateCase(string Source, string Expected);

public static class GetDisplayNameRouteTranslatorArbitraries
{
    public static Arbitrary<DisplayNameExactCase> ExactCases()
    {
        return Gen.Elements(
                new DisplayNameExactCase("worn bronze sword", "使い込まれた青銅の剣"),
                new DisplayNameExactCase("Water Containers", "水容器"))
            .ToArbitrary();
    }

    public static Arbitrary<DisplayNameTrimmedExactCase> TrimmedExactCases()
    {
        return Gen.Elements(
                new DisplayNameTrimmedExactCase("  worn bronze sword  ", "  使い込まれた青銅の剣  "),
                new DisplayNameTrimmedExactCase("Water Containers  ", "水容器  "),
                new DisplayNameTrimmedExactCase("  Water Containers", "  水容器"))
            .ToArbitrary();
    }

    public static Arbitrary<DisplayNameScopedConflictCase> ScopedConflictCases()
    {
        return Gen.Elements(
                new DisplayNameScopedConflictCase("water", "{{B|水の}}"),
                new DisplayNameScopedConflictCase("bloody Naruur", "{{r|血まみれの}}Naruur"))
            .ToArbitrary();
    }

    public static Arbitrary<DisplayNameBracketedStateCase> BracketedStateCases()
    {
        return Gen.Elements(
                new DisplayNameBracketedStateCase("water flask [empty]", "水袋 [空]"),
                new DisplayNameBracketedStateCase("water flask [empty, sealed]", "水袋 [空／密封]"),
                new DisplayNameBracketedStateCase("water flask [auto-collecting]", "水袋 [自動採取中]"))
            .ToArbitrary();
    }
}
