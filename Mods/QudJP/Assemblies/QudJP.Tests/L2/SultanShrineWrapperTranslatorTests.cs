using System.IO;
using QudJP.Patches;
using QudJP.Tests.L1;

namespace QudJP.Tests.L2;

[TestFixture]
[Category("L2")]
[NonParallelizable]
public sealed class SultanShrineWrapperTranslatorTests
{
    [SetUp]
    public void SetUp()
    {
        var localizationRoot = Path.Combine(TestProjectPaths.GetRepositoryRoot(), "Mods", "QudJP", "Localization");
        var dictionaryDir = Path.Combine(localizationRoot, "Dictionaries");

        Translator.ResetForTests();
        Translator.SetDictionaryDirectoryForTests(dictionaryDir);
        LocalizationAssetResolver.SetLocalizationRootForTests(localizationRoot);
        JournalPatternTranslator.ResetForTests();
        JournalPatternTranslator.SetPatternFilesForTests(null);
        MessagePatternTranslator.ResetForTests();
        DynamicTextObservability.ResetForTests();
    }

    [TearDown]
    public void TearDown()
    {
        Translator.ResetForTests();
        JournalPatternTranslator.ResetForTests();
        MessagePatternTranslator.ResetForTests();
        LocalizationAssetResolver.SetLocalizationRootForTests(null);
        DynamicTextObservability.ResetForTests();
    }

    [Test]
    public void Translate_TranslatesPrefixGospelAndQuality_ForReshephAnnalsComposite()
    {
        const string source =
            "The shrine depicts a significant event from the life of the ancient sultan Resheph:"
            + "\n\nIn 3 AR, Resheph cleansed the marshlands of the plagues of the Gyre and taught Abram to sow watervine along its fertile tracks."
            + "\n\n{{Y|Perfect}}";

        var translated = MessagePatternTranslator.Translate(source, nameof(DescriptionLongDescriptionPatch));

        Assert.Multiple(() =>
        {
            Assert.That(translated, Does.StartWith("この祠は古のスルタン"), "wrapper prefix should be Japanese");
            Assert.That(translated, Does.Contain("レシェフ"), "sultan name should be translated");
            Assert.That(translated, Does.Contain("3年、レシェフは"), "annals gospel should be translated via JournalPatternTranslator");
            Assert.That(translated, Does.EndWith("{{Y|完璧}}"), "quality rating should be Japanese inside its color wrapper");
            Assert.That(translated, Does.Not.Contain("Resheph"), "no English sultan name should remain");
            Assert.That(translated, Does.Not.Contain("Perfect"), "no English quality word should remain");
            Assert.That(translated, Does.Not.Contain("The shrine depicts"), "no English wrapper should remain");
        });
    }

    [TestCase("Perfect", "完璧")]
    [TestCase("Fine", "良好")]
    [TestCase("Lightly Damaged", "軽微な損傷")]
    [TestCase("Damaged", "損傷")]
    [TestCase("Badly Damaged", "重度の損傷")]
    public void Translate_TranslatesAllNonOrganicQualityRatings(string qualityEn, string qualityJa)
    {
        var source =
            "The shrine depicts a significant event from the life of the ancient sultan Resheph:"
            + "\n\nIn 3 AR, Resheph cleansed the marshlands of the plagues of the Gyre and taught Abram to sow watervine along its fertile tracks."
            + "\n\n{{Y|" + qualityEn + "}}";

        var translated = MessagePatternTranslator.Translate(source, nameof(DescriptionLongDescriptionPatch));

        Assert.That(translated, Does.EndWith("{{Y|" + qualityJa + "}}"));
        Assert.That(translated, Does.Not.Contain(qualityEn));
    }

    [Test]
    public void Translate_DoesNotMatchUnrelatedComposite_LeavesPassthroughBehavior()
    {
        const string source = "The shrine depicts something completely different.";

        var translated = MessagePatternTranslator.Translate(source, nameof(DescriptionLongDescriptionPatch));

        Assert.That(translated, Is.EqualTo(source), "non-shrine-wrapper inputs must not be touched by this translator");
    }

    [Test]
    public void Translate_TranslatesArbitrarySultanName_ViaTranslatorLeaf()
    {
        const string source =
            "The shrine depicts a significant event from the life of the ancient sultan Resheph:"
            + "\n\nIn 3 AR, Resheph cleansed the marshlands of the plagues of the Gyre and taught Abram to sow watervine along its fertile tracks."
            + "\n\n{{Y|Perfect}}";

        var translated = MessagePatternTranslator.Translate(source, nameof(DescriptionLongDescriptionPatch));

        Assert.That(translated, Does.Contain("レシェフ"));
    }

    [Test]
    public void Translate_PreservesUnknownGospel_WhenAnnalsPatternMissing()
    {
        const string source =
            "The shrine depicts a significant event from the life of the ancient sultan Resheph:"
            + "\n\nUnknown gospel that does not match any annals pattern."
            + "\n\n{{Y|Perfect}}";

        var translated = MessagePatternTranslator.Translate(source, nameof(DescriptionLongDescriptionPatch));

        Assert.Multiple(() =>
        {
            Assert.That(translated, Does.StartWith("この祠は古のスルタン"));
            Assert.That(translated, Does.Contain("Unknown gospel that does not match any annals pattern."));
            Assert.That(translated, Does.EndWith("{{Y|完璧}}"));
        });
    }

    [Test]
    public void Translate_TranslatesTwoParagraphShape_WhenQualitySuffixAbsent()
    {
        // Shape produced directly by SultanShrine.ShrineInitialize (Description.Short).
        // Quality rating is appended only on the tooltip / popup path, so routes that show
        // the long description without the wound suffix must still translate the wrapper.
        const string source =
            "The shrine depicts a significant event from the life of the ancient sultan Resheph:"
            + "\n\nIn 3 AR, Resheph cleansed the marshlands of the plagues of the Gyre and taught Abram to sow watervine along its fertile tracks.";

        var translated = MessagePatternTranslator.Translate(source, nameof(DescriptionLongDescriptionPatch));

        Assert.Multiple(() =>
        {
            Assert.That(translated, Does.StartWith("この祠は古のスルタン"), "wrapper prefix should be Japanese");
            Assert.That(translated, Does.Contain("レシェフ"), "sultan name should be translated");
            Assert.That(translated, Does.Contain("3年、レシェフは"), "annals gospel should be translated");
            Assert.That(translated, Does.Not.Contain("The shrine depicts"), "no English wrapper should remain");
            Assert.That(translated, Does.Not.Contain("Resheph"), "no English sultan name should remain");
            Assert.That(translated, Does.Not.Contain("{quality}"), "quality placeholder should not leak");
            Assert.That(translated, Does.Not.EndWith("\n\n"), "no dangling separator should remain when quality is absent");
        });
    }

    [Test]
    public void Translate_DoesNotOverMatchUnrelatedTwoParagraphTextStartingWithTheShrine()
    {
        // Plausible-but-unrelated two-paragraph text that begins with "The shrine" must not
        // be claimed by the wrapper translator. Without the canonical "depicts a significant
        // event from the life of the ancient sultan ...:" prefix the regex must miss.
        const string source = "The shrine looks weathered.\n\nMoss has overgrown its base.";

        var translated = MessagePatternTranslator.Translate(source, nameof(DescriptionLongDescriptionPatch));

        Assert.That(translated, Is.EqualTo(source), "unrelated two-paragraph inputs must fall through unchanged");
    }
}
