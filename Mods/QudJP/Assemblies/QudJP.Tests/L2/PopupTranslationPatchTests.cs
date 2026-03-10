using System.Reflection;
using System.Text;
using HarmonyLib;
using QudJP.Patches;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L2;

[TestFixture]
[Category("L2")]
[NonParallelizable]
public sealed class PopupTranslationPatchTests
{
    private string tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), "qudjp-popup-l2", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        Translator.ResetForTests();
        Translator.SetDictionaryDirectoryForTests(tempDirectory);
        DummyPopupTarget.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        Translator.ResetForTests();

        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Test]
    public void Prefix_TranslatesShowBlockMessageAndTitle()
    {
        WriteDictionary(
            ("Warning!", "警告！"),
            ("Options", "設定"));

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyPopupTarget), nameof(DummyPopupTarget.ShowBlock)),
                prefix: new HarmonyMethod(RequireMethod(typeof(PopupTranslationPatch), nameof(PopupTranslationPatch.Prefix))));

            DummyPopupTarget.ShowBlock("{{R|Warning!}}", "Options");

            Assert.Multiple(() =>
            {
                Assert.That(DummyPopupTarget.LastShowBlockMessage, Is.EqualTo("{{R|警告！}}"));
                Assert.That(DummyPopupTarget.LastShowBlockTitle, Is.EqualTo("設定"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void Prefix_TranslatesShowOptionListPayload()
    {
        WriteDictionary(
            ("Choose", "選択"),
            ("Continue", "続行"),
            ("Quit", "終了"),
            ("Prompt", "案内"),
            ("Cancel", "キャンセル"));

        var buttons = new List<DummyPopupMenuItem>
        {
            new DummyPopupMenuItem("{{W|Cancel}}"),
        };

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyPopupTarget), nameof(DummyPopupTarget.ShowOptionList)),
                prefix: new HarmonyMethod(RequireMethod(typeof(PopupTranslationPatch), nameof(PopupTranslationPatch.Prefix))));

            DummyPopupTarget.ShowOptionList(
                Title: "Choose",
                Options: new List<string> { "Continue", "{{G|Quit}}" },
                Intro: "Prompt",
                SpacingText: "Prompt",
                Buttons: buttons);

            Assert.Multiple(() =>
            {
                Assert.That(DummyPopupTarget.LastOptionListTitle, Is.EqualTo("選択"));
                Assert.That(DummyPopupTarget.LastOptionListIntro, Is.EqualTo("案内"));
                Assert.That(DummyPopupTarget.LastOptionListSpacingText, Is.EqualTo("案内"));
                Assert.That(DummyPopupTarget.LastOptionListOptions, Is.Not.Null);
                Assert.That(DummyPopupTarget.LastOptionListOptions![0], Is.EqualTo("続行"));
                Assert.That(DummyPopupTarget.LastOptionListOptions[1], Is.EqualTo("{{G|終了}}"));
                Assert.That(DummyPopupTarget.LastOptionListButtons, Is.Not.Null);
                Assert.That(DummyPopupTarget.LastOptionListButtons![0].text, Is.EqualTo("{{W|キャンセル}}"));
            });
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

        var path = Path.Combine(tempDirectory, "ui-popup.ja.json");
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
