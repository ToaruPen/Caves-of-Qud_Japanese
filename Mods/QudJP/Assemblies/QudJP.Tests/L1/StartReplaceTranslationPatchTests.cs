using QudJP.Patches;

namespace QudJP.Tests.L1;

[TestFixture]
[Category("L1")]
public sealed class StartReplaceTranslationPatchTests
{
    private string dictionaryPath = null!;

    [SetUp]
    public void SetUp()
    {
        dictionaryPath = Path.GetFullPath(
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "Localization",
                "Dictionaries",
                "templates-variable.ja.json"));
        StartReplaceTranslationPatch.ResetForTests();
        StartReplaceTranslationPatch.SetDictionaryPathForTests(dictionaryPath);
    }

    [TearDown]
    public void TearDown()
    {
        StartReplaceTranslationPatch.ResetForTests();
    }

    [TestCase("{{K|=subject.T= =verb:slip= on the ink!}}", "{{K|=subject.T=はインクで滑った！}}")]
    [TestCase("{{slimy|=subject.T= =verb:slip= on the slime!}}", "{{slimy|=subject.T=はスライムで滑った！}}")]
    [TestCase("{{C|=subject.T= =verb:slip= on the oil!}}", "{{C|=subject.T=は油で滑った！}}")]
    [TestCase("{{C|=subject.T= =verb:slip= on the ice!}}", "{{C|=subject.T=は氷で滑った！}}")]
    [TestCase("{{Y|=subject.T= =verb:slip= on the gel!}}", "{{Y|=subject.T=はゲルで滑った！}}")]
    [TestCase("Some non-matching template", "Some non-matching template")]
    [TestCase("", "")]
    [TestCase("\u0001=subject.T=はインクで滑った！", "\u0001=subject.T=はインクで滑った！")]
    public void Prefix_TranslatesStaticVariableTemplates(string source, string expected)
    {
        StartReplaceTranslationPatch.Prefix(ref source);

        Assert.That(source, Is.EqualTo(expected));
    }
}
