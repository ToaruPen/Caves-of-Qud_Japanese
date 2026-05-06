using System.Text;
using QudJP.Patches;

namespace QudJP.Tests.L1;

/// <summary>
/// Tests for <see cref="ActivatedAbilityNameTranslator.TryTranslateVisibleName"/>, which
/// was modified to check the scoped skills-and-powers dictionary <em>before</em> falling
/// through to the Release&lt;Gas&gt; pattern heuristic.
/// </summary>
[TestFixture]
[Category("L1")]
[NonParallelizable]
public sealed class ActivatedAbilityNameTranslatorTests
{
    private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private string tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), "qudjp-activated-ability-l1", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        Translator.ResetForTests();
        Translator.SetDictionaryDirectoryForTests(tempDirectory);
        ScopedDictionaryLookup.ResetForTests();
    }

    [TearDown]
    public void TearDown()
    {
        Translator.ResetForTests();
        ScopedDictionaryLookup.ResetForTests();

        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    // -------------------------------------------------------------------------
    // TryTranslateVisibleName — scoped dictionary priority (new behaviour)
    // -------------------------------------------------------------------------

    [Test]
    public void TryTranslateVisibleName_ReturnsTrue_WhenKeyFoundInSkillsAndPowersDictionary()
    {
        WriteDictionaryFile("ui-skillsandpowers.ja.json", ("Akimbo", "二挺拳銃"));

        var result = ActivatedAbilityNameTranslator.TryTranslateVisibleName("Akimbo", out var translated);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(translated, Is.EqualTo("二挺拳銃"));
        });
    }

    [Test]
    public void TryTranslateVisibleName_ReturnsFalse_WhenKeyAbsentFromDictionaryAndNotReleaseGas()
    {
        WriteDictionaryFile("ui-skillsandpowers.ja.json"); // empty

        var result = ActivatedAbilityNameTranslator.TryTranslateVisibleName("Sprint", out var translated);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(translated, Is.EqualTo("Sprint"),
                "Out parameter should equal the source when no translation is found.");
        });
    }

    [Test]
    public void TryTranslateVisibleName_UsesLowerAsciiCaseFallback_WhenUppercaseKeyAbsent()
    {
        // ScopedDictionaryLookup.TranslateExactOrLowerAscii tries lowercase ASCII if exact key misses
        WriteDictionaryFile("ui-skillsandpowers.ja.json", ("akimbo", "二挺拳銃"));

        var result = ActivatedAbilityNameTranslator.TryTranslateVisibleName("Akimbo", out var translated);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(translated, Is.EqualTo("二挺拳銃"));
        });
    }

    [Test]
    public void TryTranslateVisibleName_PrefersSkillsDictionaryOverReleaseGasHeuristic()
    {
        // If the scoped dictionary has an exact entry for "Release Poison Gas", it must be
        // returned immediately without falling through to the gas-generation heuristic.
        WriteDictionaryFile(
            "ui-skillsandpowers.ja.json",
            ("Release Poison Gas", "毒ガス放出"));

        var result = ActivatedAbilityNameTranslator.TryTranslateVisibleName(
            "Release Poison Gas", out var translated);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(translated, Is.EqualTo("毒ガス放出"),
                "Scoped dictionary entry must win over the ReleaseGasPattern heuristic.");
        });
    }

    [Test]
    public void TryTranslateVisibleName_ReturnsEmpty_ForEmptySource()
    {
        WriteDictionaryFile("ui-skillsandpowers.ja.json", ("Akimbo", "二挺拳銃"));

        // TranslateExactOrLowerAscii returns null for empty / null source;
        // the ReleaseGasPattern does not match an empty string either.
        var result = ActivatedAbilityNameTranslator.TryTranslateVisibleName(string.Empty, out var translated);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(translated, Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void TryTranslateVisibleName_DictionaryFileAbsent_ReturnsFalse()
    {
        // No dictionary file written — ScopedDictionaryLookup should silently return null
        var result = ActivatedAbilityNameTranslator.TryTranslateVisibleName("Charge", out var translated);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(translated, Is.EqualTo("Charge"));
        });
    }

    [Test]
    public void TryTranslateVisibleName_NoDuplicateMissingKeyHit_WhenFoundInScopedDictionary()
    {
        WriteDictionaryFile("ui-skillsandpowers.ja.json", ("Charge", "突撃"));

        ActivatedAbilityNameTranslator.TryTranslateVisibleName("Charge", out _);

        // The global Translator must not record a missing-key hit when the scoped lookup succeeds.
        Assert.That(Translator.GetMissingKeyHitCountForTests("Charge"), Is.EqualTo(0));
    }

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private void WriteDictionaryFile(string fileName, params (string key, string text)[] entries)
    {
        var builder = new StringBuilder();
        builder.Append("{\"entries\":[");

        for (var index = 0; index < entries.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append("{\"key\":\"");
            builder.Append(EscapeJson(entries[index].key));
            builder.Append("\",\"text\":\"");
            builder.Append(EscapeJson(entries[index].text));
            builder.Append("\"}");
        }

        builder.Append("]}");
        builder.AppendLine();

        var path = Path.Combine(tempDirectory, fileName);
        File.WriteAllText(path, builder.ToString(), Utf8WithoutBom);
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}