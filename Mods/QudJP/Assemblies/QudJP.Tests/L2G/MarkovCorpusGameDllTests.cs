#if HAS_GAME_DLL
using QudJP.Patches;

namespace QudJP.Tests.L2G;

[TestFixture]
[Category("L2G")]
[NonParallelizable]
public sealed class MarkovCorpusGameDllTests
{
    private string localizationRoot = null!;

    [SetUp]
    public void SetUp()
    {
        localizationRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../Localization"));
        LocalizationAssetResolver.SetLocalizationRootForTests(localizationRoot);
        MarkovCorpusTranslationPatch.ResetForTests();
    }

    [TearDown]
    public void TearDown()
    {
        MarkovCorpusTranslationPatch.ResetForTests();
        LocalizationAssetResolver.SetLocalizationRootForTests(null);
    }

    [Test]
    public void BuildChainData_ProducesJapaneseSentenceFromProductionCorpus()
    {
        var (order, corpusText) = MarkovCorpusTranslationPatch.LoadJapaneseCorpusSource();
        var chainData = MarkovCorpusTranslationPatch.BuildChainData(corpusText, order);

        Assert.Multiple(() =>
        {
            Assert.That(order, Is.EqualTo(2));
            Assert.That(MarkovCorpusTranslationPatch.GetOpeningWordCount(chainData), Is.GreaterThan(5000));
            Assert.That(MarkovCorpusTranslationPatch.GetTransitionCount(chainData), Is.GreaterThan(100000));
        });
    }

    [Test]
    public void GenerateSentence_ProducesValidJapaneseOutput()
    {
        var (order, corpusText) = MarkovCorpusTranslationPatch.LoadJapaneseCorpusSource();
        var chainData = MarkovCorpusTranslationPatch.BuildChainData(corpusText, order);

        const int sampleSize = 200;
        var sentences = Enumerable.Range(0, sampleSize)
            .Select(_ => MarkovCorpusTranslationPatch.GenerateSentence(chainData).TrimEnd())
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(sentences.All(s => !string.IsNullOrEmpty(s)), Is.True, "All generated sentences should be non-empty.");
            Assert.That(sentences.All(s => MarkovCorpusTranslationPatch.ContainsJapaneseCharacters(s)), Is.True, "All generated sentences should contain Japanese characters.");
            Assert.That(sentences.All(s => s.EndsWith(".", StringComparison.Ordinal)), Is.True, "All generated sentences should end with '.'.");
            Assert.That(sentences.Any(s => s.Contains('。')), Is.False, "No generated sentence should contain '。'.");
            Assert.That(sentences.Any(s => s.Contains("  ", StringComparison.Ordinal)), Is.False, "No generated sentence should contain double spaces.");
            var uniqueCount = sentences.Distinct().Count();
            Assert.That((double)uniqueCount / sampleSize, Is.GreaterThan(0.8), "Generated sentences should show sufficient diversity.");
        });
    }
}
#endif
