using System.Text;
using QudJP;
using QudJP.Patches;

namespace QudJP.Tests.L1;

[TestFixture]
[Category("L1")]
[NonParallelizable]
public sealed class HistoricStringExpanderPatchTests
{
    private string tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), "qudjp-historic-l1", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        Translator.ResetForTests();
        Translator.SetDictionaryDirectoryForTests(tempDirectory);
        DynamicTextObservability.ResetForTests();
        SinkObservation.ResetForTests();
    }

    [TearDown]
    public void TearDown()
    {
        Translator.ResetForTests();
        DynamicTextObservability.ResetForTests();
        SinkObservation.ResetForTests();

        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Test]
    public void Postfix_TranslatesKnownExpandedText()
    {
        const string source = "In the beginning, Resheph created Qud";
        WriteDictionary((source, "はじめに、レシェフがクッドを創造した"));

        var result = source;

        HistoricStringExpanderPatch.Postfix(ref result);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("はじめに、レシェフがクッドを創造した"));
            Assert.That(
                DynamicTextObservability.GetRouteFamilyHitCountForTests(
                    nameof(HistoricStringExpanderPatch),
                    "HistoricStringExpander.ExactLeaf"),
                Is.GreaterThan(0));
            Assert.That(
                SinkObservation.GetHitCountForTests(
                    nameof(UITextSkinTranslationPatch),
                    nameof(HistoricStringExpanderPatch),
                    SinkObservation.ObservationOnlyDetail,
                    source,
                    source),
                Is.EqualTo(0));
        });
    }

    [Test]
    public void Postfix_PreservesColorWrappedText()
    {
        WriteDictionary(("Warning!", "警告！"));

        var result = "{{R|Warning!}}";

        HistoricStringExpanderPatch.Postfix(ref result);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("{{R|警告！}}"));
            Assert.That(
                DynamicTextObservability.GetRouteFamilyHitCountForTests(
                    nameof(HistoricStringExpanderPatch),
                    "HistoricStringExpander.ExactLeaf"),
                Is.GreaterThan(0));
        });
    }

    [Test]
    public void Postfix_PassesThroughUnknownExpandedText()
    {
        WriteDictionary(("Known lore line", "既知の伝承文"));

        var result = "Unknown procedurally generated line";

        HistoricStringExpanderPatch.Postfix(ref result);

        Assert.That(result, Is.EqualTo("Unknown procedurally generated line"));
    }

    [Test]
    public void Postfix_ReturnsEmptyString_WhenResultIsEmpty()
    {
        var result = string.Empty;

        HistoricStringExpanderPatch.Postfix(ref result);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Postfix_ConvertsNullResultToEmptyString()
    {
        string result = null!;

        HistoricStringExpanderPatch.Postfix(ref result);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Postfix_StripsDirectTranslationMarkerBeforeTranslation()
    {
        const string source = "In the beginning, Resheph created Qud";
        var result = source;

        HistoricStringExpanderPatch.Postfix(ref result);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("In the beginning, Resheph created Qud"));
        });
    }

    private void WriteDictionary(params (string key, string text)[] entries)
    {
        var builder = new StringBuilder();
        builder.Append('{');
        builder.Append("\"entries\":[");

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

        var path = Path.Combine(tempDirectory, "historic-l1.ja.json");
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
