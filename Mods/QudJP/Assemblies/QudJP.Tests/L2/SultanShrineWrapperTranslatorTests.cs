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
}
