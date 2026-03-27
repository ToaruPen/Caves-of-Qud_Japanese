using System.Collections;
using System.Reflection;
using System.Text;
using HarmonyLib;
using QudJP.Patches;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L2;

[TestFixture]
[Category("L2")]
[NonParallelizable]
public sealed class CharGenProducerTranslationPatchTests
{
    private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private string tempRoot = null!;
    private string dictionariesDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "qudjp-chargen-producer-l2", Guid.NewGuid().ToString("N"));
        dictionariesDirectory = Path.Combine(tempRoot, "Dictionaries");
        Directory.CreateDirectory(dictionariesDirectory);

        LocalizationAssetResolver.SetLocalizationRootForTests(tempRoot);
        Translator.ResetForTests();
        Translator.SetDictionaryDirectoryForTests(dictionariesDirectory);
        ChargenStructuredTextTranslator.ResetForTests();
    }

    [TearDown]
    public void TearDown()
    {
        ChargenStructuredTextTranslator.ResetForTests();
        Translator.ResetForTests();
        LocalizationAssetResolver.SetLocalizationRootForTests(null);

        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Test]
    public void BreadcrumbPostfix_TranslatesReturnedTitle()
    {
        WriteDictionary(("Choose Game Mode", "：プレイ方式を選択："));

        var harmonyId = $"qudjp.tests.{Guid.NewGuid():N}";
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyCharGenModuleWindowTarget), nameof(DummyCharGenModuleWindowTarget.GetBreadcrumb)),
                postfix: new HarmonyMethod(RequirePatchMethod(
                    "QudJP.Patches.CharGenBreadcrumbTranslationPatch",
                    "Postfix",
                    typeof(object))));

            var result = new DummyCharGenModuleWindowTarget { BreadcrumbTitle = "Choose Game Mode" }.GetBreadcrumb();

            Assert.That(result.Title, Is.EqualTo("：プレイ方式を選択："));
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void MenuOptionPostfix_TranslatesReturnedDescriptions()
    {
        WriteDictionary(("Points Remaining:", "残りポイント:"));

        var harmonyId = $"qudjp.tests.{Guid.NewGuid():N}";
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyCharGenModuleWindowTarget), nameof(DummyCharGenModuleWindowTarget.GetKeyMenuBar)),
                postfix: new HarmonyMethod(RequirePatchMethod(
                    "QudJP.Patches.CharGenMenuOptionTranslationPatch",
                    "Postfix",
                    typeof(IEnumerable))));

            var target = new DummyCharGenModuleWindowTarget();
            target.MenuOptions.Add(new DummyCharGenMenuOption { Description = "{{y|Points Remaining: 12}}" });

            var result = target.GetKeyMenuBar().ToList();

            Assert.That(result[0].Description, Is.EqualTo("{{y|残りポイント: 12}}"));
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void ChromePrefix_TranslatesDescriptorAndCategoryTitles()
    {
        WriteDictionary(
            ("Choose Game Mode", "：プレイ方式を選択："),
            ("Choose calling", "職能を選択"));

        var harmonyId = $"qudjp.tests.{Guid.NewGuid():N}";
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(
                    typeof(DummyCharGenFrameworkScrollerTarget),
                    nameof(DummyCharGenFrameworkScrollerTarget.BeforeShow)),
                prefix: new HarmonyMethod(RequirePatchMethod(
                    "QudJP.Patches.CharGenChromeTranslationPatch",
                    "Prefix",
                    typeof(object[]),
                    typeof(MethodBase))));
            harmony.Patch(
                original: RequireMethod(
                    typeof(DummyCharGenCategoryMenuControllerTarget),
                    nameof(DummyCharGenCategoryMenuControllerTarget.setData)),
                prefix: new HarmonyMethod(RequirePatchMethod(
                    "QudJP.Patches.CharGenChromeTranslationPatch",
                    "Prefix",
                    typeof(object[]),
                    typeof(MethodBase))));

            var descriptor = new DummyEmbarkBuilderModuleWindowDescriptor { title = "Choose Game Mode" };
            var scroller = new DummyCharGenFrameworkScrollerTarget();
            scroller.BeforeShow(descriptor, selections: null);

            var category = new DummyFrameworkDataElement { Title = "Choose calling" };
            var controller = new DummyCharGenCategoryMenuControllerTarget();
            controller.setData(category);

            Assert.Multiple(() =>
            {
                Assert.That(scroller.LastTitle, Is.EqualTo("：プレイ方式を選択："));
                Assert.That(controller.LastTitle, Is.EqualTo("職能を選択"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    private static MethodInfo RequireMethod(Type type, string methodName)
    {
        var method = AccessTools.Method(type, methodName);
        Assert.That(method, Is.Not.Null, $"Method not found: {type.FullName}.{methodName}");
        return method!;
    }

    private static MethodInfo RequirePatchMethod(string typeName, string methodName, params Type[] parameterTypes)
    {
        var patchType = typeof(Translator).Assembly.GetType(typeName, throwOnError: false);
        Assert.That(patchType, Is.Not.Null, $"Patch type not found: {typeName}");

        var method = patchType!.GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: parameterTypes,
            modifiers: null);
        Assert.That(method, Is.Not.Null, $"Method not found: {typeName}.{methodName}");
        return method!;
    }

    private void WriteDictionary(params (string key, string text)[] entries)
    {
        var path = Path.Combine(dictionariesDirectory, "chargen-producer-l2.ja.json");
        using var writer = new StreamWriter(path, append: false, Utf8WithoutBom);
        writer.Write("{\"entries\":[");

        for (var index = 0; index < entries.Length; index++)
        {
            if (index > 0)
            {
                writer.Write(',');
            }

            writer.Write("{\"key\":\"");
            writer.Write(EscapeJson(entries[index].key));
            writer.Write("\",\"text\":\"");
            writer.Write(EscapeJson(entries[index].text));
            writer.Write("\"}");
        }

        writer.WriteLine("]}");
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
