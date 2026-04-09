using System.Reflection;
using System.Text;
using HarmonyLib;
using QudJP.Patches;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L2;

[TestFixture]
[Category("L2")]
[NonParallelizable]
public sealed class ChargenAttributeDescriptionTranslationPatchTests
{
    private string tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), "qudjp-chargen-attribute-owner-l2", Guid.NewGuid().ToString("N"));
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
    public void Postfix_TranslatesChargenAttributeDescriptions_WhenBeforeGetBaseAttributesRuns()
    {
        WriteDictionary((
            "Your {{W|Strength}} score determines how effectively you penetrate your opponents' armor with melee attacks, how much damage your melee attacks do, your ability to resist forced movement, and your carry capacity.",
            "あなたの{{W|筋力}}は、近接攻撃で敵の装甲を貫通する効率、近接攻撃のダメージ量、強制移動への抵抗力、所持重量を決定します。"));

        var harmonyId = $"qudjp.tests.{Guid.NewGuid():N}";
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyQudGenotypeModuleTarget), nameof(DummyQudGenotypeModuleTarget.handleUIEvent)),
                postfix: new HarmonyMethod(RequireMethod(typeof(ChargenAttributeDescriptionTranslationPatch), nameof(ChargenAttributeDescriptionTranslationPatch.Postfix))));

            var attributes = new List<DummyChargenAttributeDataElement>();
            _ = new DummyQudGenotypeModuleTarget().handleUIEvent("BeforeGetBaseAttributes", attributes);

            Assert.Multiple(() =>
            {
                Assert.That(attributes, Has.Count.EqualTo(1));
                Assert.That(attributes[0].Attribute, Is.EqualTo("STR"));
                Assert.That(attributes[0].Description, Is.EqualTo("あなたの{{W|筋力}}は、近接攻撃で敵の装甲を貫通する効率、近接攻撃のダメージ量、強制移動への抵抗力、所持重量を決定します。"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    private static MethodInfo RequireMethod(Type type, string methodName)
    {
        return AccessTools.Method(type, methodName)
            ?? throw new InvalidOperationException($"Method not found: {type.FullName}.{methodName}");
    }

    private void WriteDictionary((string key, string text) entry)
    {
        var builder = new StringBuilder();
        builder.Append("{\"entries\":[{\"key\":\"");
        builder.Append(EscapeJson(entry.key));
        builder.Append("\",\"text\":\"");
        builder.Append(EscapeJson(entry.text));
        builder.Append("\"}]}");
        builder.AppendLine();

        File.WriteAllText(
            Path.Combine(tempDirectory, "chargen-attribute-owner-l2.ja.json"),
            builder.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
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
