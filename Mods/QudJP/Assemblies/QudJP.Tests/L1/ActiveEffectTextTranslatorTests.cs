using System.Text;
using QudJP.Patches;

namespace QudJP.Tests.L1;

[TestFixture]
[Category("L1")]
[NonParallelizable]
public sealed class ActiveEffectTextTranslatorTests
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private string tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), "qudjp-active-effect-text-l1", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        Translator.ResetForTests();
        Translator.SetDictionaryDirectoryForTests(tempDirectory);
        DynamicTextObservability.ResetForTests();
    }

    [TearDown]
    public void TearDown()
    {
        Translator.ResetForTests();
        DynamicTextObservability.ResetForTests();

        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Test]
    public void TryTranslateText_DoesNotDuplicateNestedSameColorWrapper_WhenTranslatedExactOwnsMarkup()
    {
        WriteDictionary(("wet", "{{B|濡れた}}"));

        var changed = ActiveEffectTextTranslator.TryTranslateText(
            "{{B|{{B|wet}}}}",
            "ActiveEffectTextTranslatorTests",
            "ActiveEffects.Description.LiquidCovered",
            out var translated);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(translated, Is.EqualTo("{{B|濡れた}}"));
            Assert.That(translated, Does.Not.Contain("{{B|{{B|"));
            Assert.That(translated, Does.Not.Contain("{{B}}"));
        });
    }

    [Test]
    public void TryTranslateText_DoesNotLeaveEmptyLiquidColorWrappers_WhenCoveredLiquidIsColoredByParts()
    {
        WriteDictionary(("salty water", "塩水"));

        var changed = ActiveEffectTextTranslator.TryTranslateText(
            "Covered in 43 dram of {{Y|salty}} {{B|water}}.",
            "ActiveEffectTextTranslatorTests",
            "ActiveEffects.Details.LiquidCovered",
            out var translated);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(translated, Is.EqualTo("塩水を43ドラム浴びている。"));
            Assert.That(translated, Does.Not.Contain("{{Y|}}"));
            Assert.That(translated, Does.Not.Contain("{{B|}}"));
        });
    }

    [Test]
    public void TryTranslateText_DoesNotRecordMissingKey_ForGeneratedMoveSpeedLine()
    {
        var changed = ActiveEffectTextTranslator.TryTranslateText(
            "-20 move speed.",
            "ActiveEffectTextTranslatorTests",
            "ActiveEffects.Details.Wading",
            out var translated);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Is.True);
            Assert.That(translated, Is.EqualTo("移動速度 -20。"));
            Assert.That(Translator.GetMissingKeyHitCountForTests("-20 move speed."), Is.EqualTo(0));
        });
    }

    private void WriteDictionary(params (string key, string text)[] entries)
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

        File.WriteAllText(Path.Combine(tempDirectory, "active-effect-text.ja.json"), builder.ToString(), Utf8WithoutBom);
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }
}
