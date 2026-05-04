using System.Reflection;
using System.Text;
using HarmonyLib;
using QudJP.Patches;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L2;

[TestFixture]
[Category("L2")]
[NonParallelizable]
public sealed class GameSummaryAndAsleepTranslationPatchTests
{
    private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private string tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), "qudjp-summary-asleep-l2", Guid.NewGuid().ToString("N"));
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
    public void GameSummaryShowPrefix_TranslatesCauseAndDetails_WhenPatched()
    {
        WriteDictionary(
            ("You abandoned all hope.", "あなたはすべての希望を捨てた。"),
            ("Game summary for {0}", "{0}のゲームサマリー"),
            ("You were level {0}.", "レベルは{0}だった。"));

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);
        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyGameSummaryScreenTarget), nameof(DummyGameSummaryScreenTarget.ShowGameSummary)),
                prefix: new HarmonyMethod(RequireMethod(typeof(GameSummaryScreenShowTranslationPatch), nameof(GameSummaryScreenShowTranslationPatch.Prefix))));

            var target = new DummyGameSummaryScreenTarget();
            target.ShowGameSummary(
                "Qudman",
                "You abandoned all hope.",
                "{{C|*}} Game summary for {{W|Qudman}} {{C|*}}\nYou were level {{C|1}}.",
                real: true);

            Assert.Multiple(() =>
            {
                Assert.That(target.causeText.Text, Is.EqualTo("あなたはすべての希望を捨てた。"));
                Assert.That(target.detailsText.Text, Does.Contain("{{W|Qudman}}のゲームサマリー"));
                Assert.That(target.detailsText.Text, Does.Contain("レベルは{{C|1}}だった。"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void GameSummaryShowPrefix_PreservesFallbackEmptyColorAndMarkerInputs()
    {
        WriteDictionary(("You abandoned all hope.", "あなたはすべての希望を捨てた。"));

        var cause = "Unknown ending.";
        var details = string.Join(
            "\n",
            string.Empty,
            "{{y|Unknown detail.}}",
            "\u0001You abandoned all hope.");

        GameSummaryScreenShowTranslationPatch.Prefix(ref cause, ref details);

        Assert.Multiple(() =>
        {
            Assert.That(cause, Is.EqualTo("Unknown ending."));
            Assert.That(
                details,
                Is.EqualTo(string.Join(
                    "\n",
                    string.Empty,
                    "{{y|Unknown detail.}}",
                    "\u0001You abandoned all hope.")));
        });
    }

    [Test]
    public void GameSummaryUpdateMenuBarsPostfix_TranslatesHotkeyDescriptions_WhenPatched()
    {
        WriteDictionary(
            ("Save Tombstone File", "墓碑ファイルを保存"),
            ("Exit", "終了"));

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);
        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyGameSummaryScreenTarget), nameof(DummyGameSummaryScreenTarget.UpdateMenuBars)),
                transpiler: new HarmonyMethod(RequireMethod(typeof(GameSummaryScreenMenuBarsTranslationPatch), nameof(GameSummaryScreenMenuBarsTranslationPatch.Transpiler))));

            var target = new DummyGameSummaryScreenTarget();
            target.UpdateMenuBars();

            Assert.Multiple(() =>
            {
                Assert.That(target.keyMenuOptions[0].Description, Is.EqualTo("墓碑ファイルを保存"));
                Assert.That(target.keyMenuOptions[1].Description, Is.EqualTo("終了"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void GameSummaryUpdateMenuBarsLiteral_PreservesFallbackEmptyColorAndMarkerInputs()
    {
        WriteDictionary(("Save Tombstone File", "墓碑ファイルを保存"));

        Assert.Multiple(() =>
        {
            Assert.That(
                GameSummaryScreenMenuBarsTranslationPatch.TranslateLiteral("Save Tombstone File"),
                Is.EqualTo("墓碑ファイルを保存"));
            Assert.That(
                GameSummaryScreenMenuBarsTranslationPatch.TranslateLiteral("Unknown option"),
                Is.EqualTo("Unknown option"));
            Assert.That(GameSummaryScreenMenuBarsTranslationPatch.TranslateLiteral(string.Empty), Is.EqualTo(string.Empty));
            Assert.That(
                GameSummaryScreenMenuBarsTranslationPatch.TranslateLiteral("{{y|Save Tombstone File}}"),
                Is.EqualTo("{{y|Save Tombstone File}}"));
            Assert.That(
                GameSummaryScreenMenuBarsTranslationPatch.TranslateLiteral("\u0001Save Tombstone File"),
                Is.EqualTo("\u0001Save Tombstone File"));
        });
    }

    [Test]
    public void AsleepTranspiler_TranslatesExactPlayerSleepMessages_WhenPatched()
    {
        WriteDictionary(
            ("You fall {{C|asleep}}!", "{{C|眠り}}に落ちた！"),
            ("You are asleep.", "眠っている。"));

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);
        try
        {
            var transpiler = new HarmonyMethod(
                RequireMethod(typeof(AsleepMessageTranslationPatch), nameof(AsleepMessageTranslationPatch.Transpiler)));
            harmony.Patch(
                original: RequireMethod(typeof(DummyAsleepMessageTarget), nameof(DummyAsleepMessageTarget.FallAsleep)),
                transpiler: transpiler);
            harmony.Patch(
                original: RequireMethod(typeof(DummyAsleepMessageTarget), nameof(DummyAsleepMessageTarget.AreAsleep)),
                transpiler: transpiler);

            Assert.Multiple(() =>
            {
                Assert.That(DummyAsleepMessageTarget.FallAsleep(), Is.EqualTo("{{C|眠り}}に落ちた！"));
                Assert.That(DummyAsleepMessageTarget.AreAsleep(), Is.EqualTo("眠っている。"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void AsleepLiteral_PreservesFallbackEmptyColorAndMarkerInputs()
    {
        WriteDictionary(("You are asleep.", "眠っている。"));

        Assert.Multiple(() =>
        {
            Assert.That(AsleepMessageTranslationPatch.TranslateLiteral("You are asleep."), Is.EqualTo("眠っている。"));
            Assert.That(AsleepMessageTranslationPatch.TranslateLiteral("Unknown sleep line."), Is.EqualTo("Unknown sleep line."));
            Assert.That(AsleepMessageTranslationPatch.TranslateLiteral(string.Empty), Is.EqualTo(string.Empty));
            Assert.That(
                AsleepMessageTranslationPatch.TranslateLiteral("{{y|You are asleep.}}"),
                Is.EqualTo("{{y|You are asleep.}}"));
            Assert.That(
                AsleepMessageTranslationPatch.TranslateLiteral("\u0001You are asleep."),
                Is.EqualTo("\u0001You are asleep."));
        });
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

        File.WriteAllText(Path.Combine(tempDirectory, "summary-asleep-l2.ja.json"), builder.ToString(), Utf8WithoutBom);
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
