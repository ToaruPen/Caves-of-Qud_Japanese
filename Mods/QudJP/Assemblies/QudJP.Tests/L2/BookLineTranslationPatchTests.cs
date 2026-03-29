using HarmonyLib;
using QudJP.Patches;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L2;

public sealed partial class Issue201OtherUiBindingPatchTests
{
    [Test]
    public void BookLinePrefix_TranslatesPageText_WhenPatched()
    {
        WriteDictionary(("Page One", "1ページ目"));

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);
        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyBookLineTarget), nameof(DummyBookLineTarget.setData)),
                prefix: new HarmonyMethod(RequireMethod(typeof(BookLineTranslationPatch), nameof(BookLineTranslationPatch.Prefix))));

            var target = new DummyBookLineTarget();
            target.setData(new DummyBookLineDataTarget { text = "{{K|Page One}}" });

            Assert.Multiple(() =>
            {
                Assert.That(target.text.Text, Is.EqualTo("{{K|1ページ目}}"));
                Assert.That(
                    DynamicTextObservability.GetRouteFamilyHitCountForTests(nameof(BookLineTranslationPatch), "Book.LineText"),
                    Is.GreaterThan(0));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void BookLinePrefix_FallsBackToOriginal_OnUnsupportedInput()
    {
        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);
        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyBookLineTarget), nameof(DummyBookLineTarget.setData)),
                prefix: new HarmonyMethod(RequireMethod(typeof(BookLineTranslationPatch), nameof(BookLineTranslationPatch.Prefix))));

            var target = new DummyBookLineTarget();
            target.setData(new DummyFallbackBookLineDataTarget());

            Assert.Multiple(() =>
            {
                Assert.That(target.OriginalExecuted, Is.True);
                Assert.That(target.text.Text, Is.EqualTo("book line fallback"));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }
}
