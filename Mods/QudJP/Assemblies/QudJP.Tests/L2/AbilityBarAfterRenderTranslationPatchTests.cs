using System.Reflection;
using System.Text;
using HarmonyLib;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L2;

[TestFixture]
[Category("L2")]
[NonParallelizable]
public sealed class AbilityBarAfterRenderTranslationPatchTests
{
    private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private string tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), "qudjp-abilitybar-afterrender-l2", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        Translator.ResetForTests();
        Translator.SetDictionaryDirectoryForTests(tempDirectory);
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
    public void Postfix_TranslatesBackingsAfterAfterRender()
    {
        WriteDictionary(
            ("ACTIVE EFFECTS:", "発動中の効果:"),
            ("poisoned", "毒状態"),
            ("TARGET:", "ターゲット:"),
            ("snapjaw", "スナップジョー"),
            ("Healthy", "健康"),
            ("calm", "平静"));

        var harmonyId = $"qudjp.tests.{Guid.NewGuid():N}";
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyAbilityBarAfterRenderTarget), nameof(DummyAbilityBarAfterRenderTarget.AfterRender)),
                postfix: new HarmonyMethod(RequirePatchMethod(
                    "QudJP.Patches.AbilityBarAfterRenderTranslationPatch",
                    "Postfix",
                    typeof(object))));

            var target = new DummyAbilityBarAfterRenderTarget
            {
                NextEffectText = "{{Y|ACTIVE EFFECTS:}} poisoned",
                NextTargetText = "{{C|TARGET: snapjaw}}",
                NextTargetHealthText = "Healthy, calm",
            };

            target.AfterRender(core: null, sb: null);

            Assert.Multiple(() =>
            {
                Assert.That(target.GetEffectText(), Is.EqualTo("{{Y|発動中の効果:}} 毒状態"));
                Assert.That(target.GetTargetText(), Is.EqualTo("{{C|ターゲット: スナップジョー}}"));
                Assert.That(target.GetTargetHealthText(), Is.EqualTo("健康、平静"));
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
        var path = Path.Combine(tempDirectory, "abilitybar-afterrender-l2.ja.json");
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
