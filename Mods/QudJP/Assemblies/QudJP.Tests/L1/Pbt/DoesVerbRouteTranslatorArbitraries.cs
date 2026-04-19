using FsCheck;
using FsCheck.Fluent;

namespace QudJP.Tests.L1.Pbt;

public sealed record DoesVerbMarkedTailCase(string Fragment, string Verb, string Tail, string Expected);

public sealed record DoesVerbEarliestVerbCase(string Source, string Expected);

public sealed record DoesVerbCanonicalMarkerCase(string Fragment, string Verb, string Tail, string Expected);

public sealed record DoesVerbUnchangedPassThroughCase(string Source);

public sealed record DoesVerbVisiblePassThroughCase(string Source, string ExpectedVisible);

public sealed record DoesVerbColorCase(string Source, string Expected);

public static class DoesVerbRouteTranslatorArbitraries
{
    public static Arbitrary<DoesVerbMarkedTailCase> MarkedTailCases()
    {
        return Gen.Elements(
                new DoesVerbMarkedTailCase("The 熊 are", "are", " stuck.", "熊は動けなくなった。"),
                new DoesVerbMarkedTailCase("The 熊 are", "are", " exhausted!", "熊は疲弊した！"))
            .ToArbitrary();
    }

    public static Arbitrary<DoesVerbEarliestVerbCase> EarliestVerbCases()
    {
        return Gen.Elements(
                new DoesVerbEarliestVerbCase(
                    "The 熊 asks about its location and is no longer lost.",
                    "熊は自分の居場所について尋ね、もう迷っていない"),
                new DoesVerbEarliestVerbCase(
                    "The 熊 asks about its location and is no longer stunned.",
                    "熊は自分の居場所について尋ね、もう気絶していない"))
            .ToArbitrary();
    }

    public static Arbitrary<DoesVerbCanonicalMarkerCase> CanonicalMarkerCases()
    {
        return Gen.Elements(
                new DoesVerbCanonicalMarkerCase("The 熊 is", "are", " stunned!", "熊は気絶した！"),
                new DoesVerbCanonicalMarkerCase("The 水筒 has", "have", " no room for more water.", "水筒はこれ以上の水を入れる余地がない。"),
                new DoesVerbCanonicalMarkerCase("The 熊 falls", "fall", " to the ground.", "熊は地面に倒れた。"))
            .ToArbitrary();
    }

    public static Arbitrary<DoesVerbUnchangedPassThroughCase> UnchangedPassThroughCases()
    {
        return Gen.Elements(
                new DoesVerbUnchangedPassThroughCase(string.Empty),
                new DoesVerbUnchangedPassThroughCase("The bear performs an unknowable act."),
                new DoesVerbUnchangedPassThroughCase("\u0002\u0003The 熊 are stuck."),
                new DoesVerbUnchangedPassThroughCase("\u0002are\u001f5\u001f9\u0003The 熊 are stuck."),
                new DoesVerbUnchangedPassThroughCase("\u0002are\u001fX\u001f9\u001f\u0003The 熊 are stuck."))
            .ToArbitrary();
    }

    public static Arbitrary<DoesVerbVisiblePassThroughCase> VisiblePassThroughCases()
    {
        return Gen.Elements(
                new DoesVerbVisiblePassThroughCase("\u0002are\u001f6\u001f5\u001f\u0003The 熊 are stuck.", "The 熊 are stuck."),
                new DoesVerbVisiblePassThroughCase("\u0002are\u001f5\u001f9\u001f\u0003The 熊 are bewildered.", "The 熊 are bewildered."))
            .ToArbitrary();
    }

    public static Arbitrary<DoesVerbColorCase> ColorCases()
    {
        return Gen.Elements(
                new DoesVerbColorCase("{{g|The 熊 is exhausted!}}", "{{g|熊は疲弊した！}}"),
                new DoesVerbColorCase("{{g|The 熊 asks about its location and is no longer lost.}}", "{{g|熊は自分の居場所について尋ね、もう迷っていない}}"),
                new DoesVerbColorCase(
                    "{{g|\u0002fall\u001f5\u001f11\u001f\u0003The 熊 falls to the ground.}}",
                    "{{g|熊は地面に倒れた。}}"))
            .ToArbitrary();
    }
}
