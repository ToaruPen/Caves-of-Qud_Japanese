using System.Collections;
using System.Reflection;
using System.Text;
using HarmonyLib;
using QudJP.Patches;

namespace QudJP.Tests.L2;

[TestFixture]
[Category("L2")]
[NonParallelizable]
public sealed class QudMenuBottomContextTranslationPatchTests
{
    private string tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), "qudjp-bottom-context-l2", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        ResetTestState();
        Translator.SetDictionaryDirectoryForTests(tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        ResetTestState();
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Test]
    public void Prefix_TranslatesMenuItemText()
    {
        WriteDictionary(("Inspect", "調べる"));

        var context = new DummyQudMenuBottomContext("Inspect");
        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyQudMenuBottomContext), nameof(DummyQudMenuBottomContext.RefreshButtons)),
                prefix: new HarmonyMethod(RequireMethod(typeof(QudMenuBottomContextTranslationPatch), nameof(QudMenuBottomContextTranslationPatch.Prefix))));

            context.RefreshButtons();

            Assert.Multiple(() =>
            {
                Assert.That(((DummyMenuItem)context.items[0]!).text, Is.EqualTo("調べる"));
                Assert.That(
                    DynamicTextObservability.GetRouteFamilyHitCountForTests(
                        nameof(QudMenuBottomContextTranslationPatch),
                        "Popup.ProducerMenuItem.Exact"),
                    Is.GreaterThan(0));
                Assert.That(
                    SinkObservation.GetHitCountForTests(
                        nameof(PopupTranslationPatch),
                        nameof(QudMenuBottomContextTranslationPatch),
                        SinkObservation.ObservationOnlyDetail,
                        "Inspect",
                        "Inspect"),
                    Is.EqualTo(0));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void Prefix_StripsDirectTranslationMarker_FromMenuItemText()
    {
        var context = new DummyQudMenuBottomContext("調べる");
        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyQudMenuBottomContext), nameof(DummyQudMenuBottomContext.RefreshButtons)),
                prefix: new HarmonyMethod(RequireMethod(typeof(QudMenuBottomContextTranslationPatch), nameof(QudMenuBottomContextTranslationPatch.Prefix))));

            context.RefreshButtons();

            Assert.That(((DummyMenuItem)context.items[0]!).text, Is.EqualTo("調べる"));
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    private static string CreateHarmonyId()
    {
        return $"qudjp.tests.{Guid.NewGuid():N}";
    }

    private static MethodInfo RequireMethod(Type type, string methodName)
    {
        return AccessTools.Method(type, methodName)
            ?? throw new InvalidOperationException($"Method not found: {type.FullName}.{methodName}");
    }

    private static void ResetTestState()
    {
        Translator.ResetForTests();
        DynamicTextObservability.ResetForTests();
        SinkObservation.ResetForTests();
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

        var path = Path.Combine(tempDirectory, "bottom-context.ja.json");
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Translator.SetDictionaryDirectoryForTests(tempDirectory);
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private sealed class DummyQudMenuBottomContext
    {
        public IList items;

        public DummyQudMenuBottomContext(string text)
        {
            items = new ArrayList { new DummyMenuItem(text) };
        }

        public void RefreshButtons()
        {
        }
    }

    private sealed class DummyMenuItem
    {
        public string text;

        public DummyMenuItem(string text)
        {
            this.text = text;
        }
    }
}
