using System.Reflection;
using System.Text;
using HarmonyLib;
using QudJP.Patches;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L2;

[TestFixture]
[Category("L2")]
[NonParallelizable]
public sealed class XDidYTranslationPatchTests
{
    private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private string tempDirectory = null!;
    private string dictionaryPath = null!;
    private string? lastMessage;
    private bool lastUsePopup;

    [SetUp]
    public void SetUp()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), "qudjp-xdidy-l2", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        dictionaryPath = Path.Combine(tempDirectory, "verbs.ja.json");

        Translator.ResetForTests();
        Translator.SetDictionaryDirectoryForTests(tempDirectory);
        File.WriteAllText(Path.Combine(tempDirectory, "ui-test.ja.json"), "{\"entries\":[]}\n", Utf8WithoutBom);
        MessageFrameTranslator.ResetForTests();
        MessageFrameTranslator.SetDictionaryPathForTests(dictionaryPath);
        XDidYTranslationPatch.SetMessageDispatcherForTests((_, message, _, usePopup) =>
        {
            lastMessage = message;
            lastUsePopup = usePopup;
        });
        DummyXDidYTarget.Reset();
        lastMessage = null;
        lastUsePopup = false;
    }

    [TearDown]
    public void TearDown()
    {
        XDidYTranslationPatch.SetMessageDispatcherForTests(null);
        MessageFrameTranslator.ResetForTests();
        Translator.ResetForTests();

        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Test]
    public void Prefix_TranslatesXDidYAndSkipsOriginalEnglishAssembly()
    {
        WriteDictionary(tier1: new[] { ("block", "防いだ") });

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyXDidYTarget), nameof(DummyXDidYTarget.XDidY)),
                prefix: new HarmonyMethod(RequireMethod(typeof(XDidYTranslationPatch), nameof(XDidYTranslationPatch.PrefixXDidYForTests))));

            DummyXDidYTarget.XDidY(
                Actor: null,
                Verb: "block",
                SubjectOverride: "熊",
                AlwaysVisible: true);

            Assert.Multiple(() =>
            {
                Assert.That(DummyXDidYTarget.OriginalExecuted, Is.False);
                Assert.That(lastMessage, Is.EqualTo("\u0001熊は防いだ。"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void Prefix_TranslatesBreatherConeXDidYAndSkipsOriginalEnglishAssembly()
    {
        WriteDictionary(tier2: new[] { ("breath", "a cone of poison gas", "毒ガスを円錐状に吐き出した") });

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyXDidYTarget), nameof(DummyXDidYTarget.XDidY)),
                prefix: new HarmonyMethod(RequireMethod(typeof(XDidYTranslationPatch), nameof(XDidYTranslationPatch.PrefixXDidYForTests))));

            DummyXDidYTarget.XDidY(
                Actor: null,
                Verb: "breath",
                Extra: "a cone of poison gas",
                EndMark: "!",
                SubjectOverride: "熊",
                AlwaysVisible: true);

            Assert.Multiple(() =>
            {
                Assert.That(DummyXDidYTarget.OriginalExecuted, Is.False);
                Assert.That(lastMessage, Is.EqualTo("\u0001熊は毒ガスを円錐状に吐き出した！"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void Prefix_TranslatesActorDisplayNameFromCurrentOneSignature()
    {
        File.WriteAllText(
            Path.Combine(tempDirectory, "ui-test.ja.json"),
            "{\"entries\":[{\"key\":\"CanvasWall\",\"text\":\"帆布壁\"}]}\n",
            Utf8WithoutBom);
        Translator.ResetForTests();
        Translator.SetDictionaryDirectoryForTests(tempDirectory);
        WriteDictionary(tier1: new[] { ("collapse", "崩れた") });

        var actor = new DummyCurrentDisplayNameTarget("CanvasWall");
        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyXDidYTarget), nameof(DummyXDidYTarget.XDidY)),
                prefix: new HarmonyMethod(RequireMethod(typeof(XDidYTranslationPatch), nameof(XDidYTranslationPatch.PrefixXDidYForTests))));

            DummyXDidYTarget.XDidY(
                Actor: actor,
                Verb: "collapse",
                AlwaysVisible: true);

            Assert.Multiple(() =>
            {
                Assert.That(DummyXDidYTarget.OriginalExecuted, Is.False);
                Assert.That(lastMessage, Is.EqualTo("\u0001帆布壁は崩れた。"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void Prefix_PromotesUsePopupFromDialogWhenHeldByPlayer()
    {
        WriteDictionary(tier1: new[] { ("block", "防いだ") });

        var playerHolder = new DummyVisibilityTarget(isPlayer: true);
        var actor = new DummyVisibilityTarget(holder: playerHolder);

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyXDidYTarget), nameof(DummyXDidYTarget.XDidY)),
                prefix: new HarmonyMethod(RequireMethod(typeof(XDidYTranslationPatch), nameof(XDidYTranslationPatch.PrefixXDidYForTests))));

            DummyXDidYTarget.XDidY(
                Actor: actor,
                Verb: "block",
                SubjectOverride: "熊",
                AlwaysVisible: true,
                FromDialog: true,
                UsePopup: false);

            Assert.Multiple(() =>
            {
                Assert.That(DummyXDidYTarget.OriginalExecuted, Is.False);
                Assert.That(lastMessage, Is.EqualTo("\u0001あなたの熊は防いだ。"));
                Assert.That(lastUsePopup, Is.True);
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void Prefix_SuppressesInvisibleMessageBeforeEnglishAssembly()
    {
        WriteDictionary(tier1: new[] { ("block", "防いだ") });

        var hiddenSource = new DummyVisibilityTarget(isVisible: false);

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyXDidYTarget), nameof(DummyXDidYTarget.XDidY)),
                prefix: new HarmonyMethod(RequireMethod(typeof(XDidYTranslationPatch), nameof(XDidYTranslationPatch.PrefixXDidYForTests))));

            DummyXDidYTarget.XDidY(
                Actor: null,
                Verb: "block",
                SubjectOverride: "熊",
                Source: hiddenSource,
                AlwaysVisible: false);

            Assert.Multiple(() =>
            {
                Assert.That(DummyXDidYTarget.OriginalExecuted, Is.False);
                Assert.That(lastMessage, Is.Null);
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void Prefix_TranslatesXDidYToZWithObjectTemplate()
    {
        WriteDictionary(tier2: new[] { ("stare", "at {0} menacingly", "{0}を睨みつけた") });

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyXDidYTarget), nameof(DummyXDidYTarget.XDidYToZ)),
                prefix: new HarmonyMethod(RequireMethod(typeof(XDidYTranslationPatch), nameof(XDidYTranslationPatch.PrefixXDidYToZForTests))));

            DummyXDidYTarget.XDidYToZ(
                Actor: null,
                Verb: "stare",
                Preposition: "at",
                Object: "タム",
                Extra: "menacingly",
                SubjectOverride: "熊",
                AlwaysVisible: true);

            Assert.Multiple(() =>
            {
                Assert.That(DummyXDidYTarget.OriginalExecuted, Is.False);
                Assert.That(lastMessage, Is.EqualTo("\u0001熊はタムを睨みつけた。"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void Prefix_TranslatesXDidYToZFlinchOutOfWayOfProjectile()
    {
        WriteDictionary(tier3: new[] { ("flinch", "out of the way of {0}", "{0}をかわした") });

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyXDidYTarget), nameof(DummyXDidYTarget.XDidYToZ)),
                prefix: new HarmonyMethod(RequireMethod(typeof(XDidYTranslationPatch), nameof(XDidYTranslationPatch.PrefixXDidYToZForTests))));

            DummyXDidYTarget.XDidYToZ(
                Actor: null,
                Verb: "flinch",
                Preposition: "out of the way of",
                Object: "木の矢",
                SubjectOverride: "ドリンクス",
                AlwaysVisible: true);

            Assert.Multiple(() =>
            {
                Assert.That(DummyXDidYTarget.OriginalExecuted, Is.False);
                Assert.That(lastMessage, Is.EqualTo("\u0001ドリンクスは木の矢をかわした。"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void Prefix_TranslatesXDidYToZWadeThroughLiquid()
    {
        WriteDictionary(tier3: new[] { ("wade", "through {0}", "{0}の中をかき分けて進んだ") });

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyXDidYTarget), nameof(DummyXDidYTarget.XDidYToZ)),
                prefix: new HarmonyMethod(RequireMethod(typeof(XDidYTranslationPatch), nameof(XDidYTranslationPatch.PrefixXDidYToZForTests))));

            DummyXDidYTarget.XDidYToZ(
                Actor: null,
                Verb: "wade",
                Preposition: "through",
                Object: "塩気のある水たまり",
                SubjectOverride: "あなた",
                AlwaysVisible: true);

            Assert.Multiple(() =>
            {
                Assert.That(DummyXDidYTarget.OriginalExecuted, Is.False);
                Assert.That(lastMessage, Is.EqualTo("\u0001あなたは塩気のある水たまりの中をかき分けて進んだ。"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void Prefix_TranslatesXDidYToZObjectDisplayNameBeforePrepositionSuffix()
    {
        File.WriteAllText(
            Path.Combine(tempDirectory, "ui-test.ja.json"),
            "{\"entries\":[{\"key\":\"Bedroll\",\"text\":\"寝袋\"}]}\n",
            Utf8WithoutBom);
        Translator.ResetForTests();
        Translator.SetDictionaryDirectoryForTests(tempDirectory);
        WriteDictionary(tier1: new[] { ("lie", "横になった"), ("rise", "起き上がった") });

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyXDidYTarget), nameof(DummyXDidYTarget.XDidYToZ)),
                prefix: new HarmonyMethod(RequireMethod(typeof(XDidYTranslationPatch), nameof(XDidYTranslationPatch.PrefixXDidYToZForTests))));

            DummyXDidYTarget.XDidYToZ(
                Actor: null,
                Verb: "lie",
                Preposition: "down on",
                Object: "Bedroll",
                SubjectOverride: "あなた",
                AlwaysVisible: true);

            Assert.That(lastMessage, Is.EqualTo("\u0001あなたは寝袋の上に横になった。"));

            DummyXDidYTarget.Reset();
            lastMessage = null;

            DummyXDidYTarget.XDidYToZ(
                Actor: null,
                Verb: "rise",
                Preposition: "from",
                Object: "Bedroll",
                SubjectOverride: "あなた",
                AlwaysVisible: true);

            Assert.That(lastMessage, Is.EqualTo("\u0001あなたは寝袋から起き上がった。"));
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void Prefix_TranslatesShrinePrayerVerbAndGeneratedStatueObject()
    {
        File.WriteAllText(
            Path.Combine(tempDirectory, "ui-test.ja.json"),
            "{\"entries\":[" +
            "{\"key\":\"desecrated\",\"text\":\"冒涜された\"}," +
            "{\"key\":\"stone\",\"text\":\"石\"}," +
            "{\"key\":\"statue\",\"text\":\"像\"}" +
            "]}\n",
            Utf8WithoutBom);
        Translator.ResetForTests();
        Translator.SetDictionaryDirectoryForTests(tempDirectory);
        WriteDictionary(tier2: new[] { ("voice", "a short prayer beneath {0}", "{0}の下で短い祈りを唱えた") });

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyXDidYTarget), nameof(DummyXDidYTarget.XDidYToZ)),
                prefix: new HarmonyMethod(RequireMethod(typeof(XDidYTranslationPatch), nameof(XDidYTranslationPatch.PrefixXDidYToZForTests))));

            DummyXDidYTarget.XDidYToZ(
                Actor: null,
                Verb: "voice",
                Preposition: "a short prayer beneath",
                Object: "desecrated stone statue of a 山羊人の種播き",
                SubjectOverride: "あなた",
                AlwaysVisible: true);

            Assert.Multiple(() =>
            {
                Assert.That(DummyXDidYTarget.OriginalExecuted, Is.False);
                Assert.That(lastMessage, Is.EqualTo("\u0001あなたは冒涜された山羊人の種播きの石の像の下で短い祈りを唱えた。"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void Prefix_TranslatesWDidXToYWithZWithTemplate()
    {
        WriteDictionary(tier3: new[] { ("strike", "{0} with {1} for {2} damage", "{1}で{0}に{2}ダメージを与えた") });

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyXDidYTarget), nameof(DummyXDidYTarget.WDidXToYWithZ)),
                prefix: new HarmonyMethod(RequireMethod(typeof(XDidYTranslationPatch), nameof(XDidYTranslationPatch.PrefixWDidXToYWithZForTests))));

            DummyXDidYTarget.WDidXToYWithZ(
                Actor: null,
                Verb: "strike",
                DirectPreposition: null,
                DirectObject: "スナップジョー",
                IndirectPreposition: "with",
                IndirectObject: "青銅の短剣",
                Extra: "for 5 damage",
                SubjectOverride: "熊",
                AlwaysVisible: true,
                EndMark: "!");

            Assert.Multiple(() =>
            {
                Assert.That(DummyXDidYTarget.OriginalExecuted, Is.False);
                Assert.That(lastMessage, Is.EqualTo("\u0001熊は青銅の短剣でスナップジョーに5ダメージを与えた！"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void Prefix_FallsBackToOriginalWhenVerbLookupIsMissing()
    {
        WriteDictionary(tier1: new[] { ("block", "防いだ") });

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);

        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyXDidYTarget), nameof(DummyXDidYTarget.XDidY)),
                prefix: new HarmonyMethod(RequireMethod(typeof(XDidYTranslationPatch), nameof(XDidYTranslationPatch.PrefixXDidYForTests))));

            DummyXDidYTarget.XDidY(
                Actor: null,
                Verb: "teleport",
                SubjectOverride: "熊",
                AlwaysVisible: true);

            Assert.Multiple(() =>
            {
                Assert.That(DummyXDidYTarget.OriginalExecuted, Is.True);
                Assert.That(lastMessage, Is.Null);
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    private void WriteDictionary(
        IEnumerable<(string verb, string text)>? tier1 = null,
        IEnumerable<(string verb, string extra, string text)>? tier2 = null,
        IEnumerable<(string verb, string extra, string text)>? tier3 = null)
    {
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine("  \"entries\": [],");
        builder.AppendLine("  \"tier1\": [");
        WriteTier1(builder, tier1);
        builder.AppendLine("  ],");
        builder.AppendLine("  \"tier2\": [");
        WriteTier2(builder, tier2);
        builder.AppendLine("  ],");
        builder.AppendLine("  \"tier3\": [");
        WriteTier2(builder, tier3);
        builder.AppendLine("  ]");
        builder.AppendLine("}");

        File.WriteAllText(dictionaryPath, builder.ToString(), Utf8WithoutBom);
    }

    private static void WriteTier1(StringBuilder builder, IEnumerable<(string verb, string text)>? entries)
    {
        if (entries is null)
        {
            return;
        }

        var first = true;
        foreach (var entry in entries)
        {
            if (!first)
            {
                builder.AppendLine(",");
            }

            first = false;
            builder.Append("    { \"verb\": \"")
                .Append(EscapeJson(entry.verb))
                .Append("\", \"text\": \"")
                .Append(EscapeJson(entry.text))
                .Append("\" }");
        }

        if (!first)
        {
            builder.AppendLine();
        }
    }

    private static void WriteTier2(StringBuilder builder, IEnumerable<(string verb, string extra, string text)>? entries)
    {
        if (entries is null)
        {
            return;
        }

        var first = true;
        foreach (var entry in entries)
        {
            if (!first)
            {
                builder.AppendLine(",");
            }

            first = false;
            builder.Append("    { \"verb\": \"")
                .Append(EscapeJson(entry.verb))
                .Append("\", \"extra\": \"")
                .Append(EscapeJson(entry.extra))
                .Append("\", \"text\": \"")
                .Append(EscapeJson(entry.text))
                .Append("\" }");
        }

        if (!first)
        {
            builder.AppendLine();
        }
    }

    private static MethodInfo RequireMethod(Type type, string methodName)
    {
        return AccessTools.Method(type, methodName)
            ?? throw new InvalidOperationException($"Method not found: {type.FullName}.{methodName}");
    }

    private static string CreateHarmonyId()
    {
        return $"qudjp.tests.xdidy.{Guid.NewGuid():N}";
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }

    private sealed class DummyCurrentDisplayNameTarget
    {
        public DummyCurrentDisplayNameTarget(string displayName)
        {
            DisplayName = displayName;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S1144:Unused private types or members",
            Justification = "Read by the reflected One signature used in the test.")]
        public string DisplayName { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Major Code Smell",
            "S1144:Unused private types or members",
            Justification = "Invoked by XDidYTranslationPatch through reflection.")]
        public string One(
            int Cutoff = int.MaxValue,
            string? Base = null,
            string? Context = null,
            bool AsIfKnown = false,
            bool Single = false,
            bool NoConfusion = false,
            bool NoColor = false,
            bool Stripped = false,
            bool WithoutTitles = true,
            bool Short = true,
            bool BaseOnly = false,
            bool WithIndefiniteArticle = false,
            string? DefaultDefiniteArticle = null,
            bool IndicateHidden = true,
            bool SecondPerson = true,
            bool Reflexive = false,
            bool? IncludeAdjunctNoun = null,
            bool AsPossessed = false,
            object? AsPossessedBy = null,
            bool Reference = false)
        {
            _ = Cutoff;
            _ = Base;
            _ = Context;
            _ = AsIfKnown;
            _ = Single;
            _ = NoConfusion;
            _ = NoColor;
            _ = Stripped;
            _ = WithoutTitles;
            _ = Short;
            _ = BaseOnly;
            _ = WithIndefiniteArticle;
            _ = DefaultDefiniteArticle;
            _ = IndicateHidden;
            _ = SecondPerson;
            _ = Reflexive;
            _ = IncludeAdjunctNoun;
            _ = AsPossessed;
            _ = AsPossessedBy;
            _ = Reference;
            return DisplayName;
        }

        public override string ToString()
        {
            return "Object";
        }
    }
}
