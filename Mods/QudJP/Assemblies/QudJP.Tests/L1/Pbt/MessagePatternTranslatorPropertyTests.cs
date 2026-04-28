using FsCheck.Fluent;

using FsCheckProperty = FsCheck.Property;

namespace QudJP.Tests.L1.Pbt;

[TestFixture]
[Category("L1")]
public sealed class MessagePatternTranslatorPropertyTests
{
    private const string ReplaySeed = "975318642,24681";

    [SetUp]
    public void SetUp()
    {
        MessagePatternTranslator.ResetForTests();
        UseRepositoryPatternDictionary();
    }

    [TearDown]
    public void TearDown()
    {
        MessagePatternTranslator.ResetForTests();
    }

    [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(MessagePatternTranslatorArbitraries) }, MaxTest = 100, Replay = ReplaySeed)]
    public FsCheckProperty Translate_PreservesHitWithRollWrappers(HitWithRollPatternCase sample)
    {
        return AssertTranslated(sample.Source, sample.ExpectedTranslated);
    }

    [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(MessagePatternTranslatorArbitraries) }, MaxTest = 100, Replay = ReplaySeed)]
    public FsCheckProperty Translate_PrefersHitWithWeaponDamageBeforeGenericHitDamage(HitTargetWithWeaponDamagePatternCase sample)
    {
        return AssertTranslated(sample.Source, sample.ExpectedTranslated);
    }

    [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(MessagePatternTranslatorArbitraries) }, MaxTest = 100, Replay = ReplaySeed)]
    public FsCheckProperty Translate_PrefersWeaponMultiplierDamageBeforeGenericHitWithWeaponDamage(HitWeaponMultiplierDamagePatternCase sample)
    {
        return AssertTranslated(sample.Source, sample.ExpectedTranslated);
    }

    [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(MessagePatternTranslatorArbitraries) }, MaxTest = 100, Replay = ReplaySeed)]
    public FsCheckProperty Translate_PrefersHitMultiplierWithWeaponBeforeGenericHitWithWeapon(HitMultiplierWithWeaponPatternCase sample)
    {
        return AssertTranslated(sample.Source, sample.ExpectedTranslated);
    }

    [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(MessagePatternTranslatorArbitraries) }, MaxTest = 100, Replay = ReplaySeed)]
    public FsCheckProperty Translate_PrefersIncomingWeaponMultiplierDamageBeforeGenericIncomingWeaponDamage(
        IncomingHitWeaponMultiplierDamagePatternCase sample)
    {
        return AssertTranslated(sample.Source, sample.ExpectedTranslated);
    }

    [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(MessagePatternTranslatorArbitraries) }, MaxTest = 100, Replay = ReplaySeed)]
    public FsCheckProperty Translate_PrefersThirdPartyWeaponMultiplierDamageBeforeGenericThirdPartyWeaponDamage(
        ThirdPartyHitWeaponMultiplierDamagePatternCase sample)
    {
        return AssertTranslated(sample.Source, sample.ExpectedTranslated);
    }

    [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(MessagePatternTranslatorArbitraries) }, MaxTest = 100, Replay = ReplaySeed)]
    public FsCheckProperty Translate_PreservesWeaponMissWrappers(WeaponMissPatternCase sample)
    {
        return AssertTranslated(sample.Source, sample.ExpectedTranslated);
    }

    [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(MessagePatternTranslatorArbitraries) }, MaxTest = 100, Replay = ReplaySeed)]
    public FsCheckProperty Translate_PrefersOneOfWoundsStopBleedingBeforeGenericStopBleeding(SpecificBleedingStopPatternCase sample)
    {
        return AssertTranslated(sample.Source, sample.ExpectedTranslated);
    }

    [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(MessagePatternTranslatorArbitraries) }, MaxTest = 100, Replay = ReplaySeed)]
    public FsCheckProperty Translate_PrefersBlockedByArticleBeforeGenericBlockedBy(BlockedByArticlePatternCase sample)
    {
        Assert.That(sample.ExpectedGenericFallbackTranslated, Is.Not.EqualTo(sample.ExpectedTranslated));

        return AssertTranslated(sample.Source, sample.ExpectedTranslated);
    }

    [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(MessagePatternTranslatorArbitraries) }, MaxTest = 100, Replay = ReplaySeed)]
    public FsCheckProperty Translate_BlockedByArticleGenericFallback_UsesGenericBlockedByTranslation(
        BlockedByArticlePatternCase sample)
    {
        Assert.That(sample.ExpectedGenericFallbackTranslated, Is.Not.EqualTo(sample.ExpectedTranslated));

        var fallbackSource = sample.Source
            .Replace(" by some ", " by an ", StringComparison.Ordinal)
            .Replace(" by a ", " by an ", StringComparison.Ordinal);

        return AssertTranslated(fallbackSource, sample.ExpectedGenericFallbackTranslated);
    }

    [Test]
    public void Translate_BlockedByArticleEdgeCases_PreservesEmptyInput()
    {
        _ = AssertTranslated(string.Empty, string.Empty);
    }

    [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(MessagePatternTranslatorArbitraries) }, MaxTest = 100, Replay = ReplaySeed)]
    public FsCheckProperty Translate_BlockedByArticleEdgeCases_PreservesDirectMarker(
        BlockedByArticlePatternCase sample)
    {
        var markedSource = "\u0001" + sample.Source;
        return AssertTranslated(markedSource, markedSource);
    }

    [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(MessagePatternTranslatorArbitraries) }, MaxTest = 100, Replay = ReplaySeed)]
    public FsCheckProperty Translate_BlockedByArticleColorTags_PreserveCaptureMarkup(
        BlockedByArticleColorTagPatternCase sample)
    {
        return AssertTranslated(sample.Source, sample.ExpectedTranslated);
    }

    [FsCheck.NUnit.Property(Arbitrary = new[] { typeof(MessagePatternTranslatorArbitraries) }, MaxTest = 100, Replay = ReplaySeed)]
    public FsCheckProperty Translate_PrefersPassByArticleBeforeGenericPassBy(PassByArticlePatternCase sample)
    {
        return AssertTranslated(sample.Source, sample.ExpectedTranslated);
    }

    private static FsCheckProperty AssertTranslated(string source, string expectedTranslated)
    {
        var translated = MessagePatternTranslator.Translate(source);

        Assert.That(translated, Is.EqualTo(expectedTranslated));

        return true.ToProperty();
    }

    private static void UseRepositoryPatternDictionary()
    {
        var root = TestProjectPaths.GetRepositoryRoot();
        var repositoryPatternFile = Path.Combine(root, "Mods", "QudJP", "Localization", "Dictionaries", "messages.ja.json");
        MessagePatternTranslator.SetPatternFileForTests(repositoryPatternFile);
    }
}
