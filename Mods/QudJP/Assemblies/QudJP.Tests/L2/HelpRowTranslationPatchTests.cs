using HarmonyLib;
using QudJP.Patches;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L2;

public sealed partial class Issue201StatusScreensBatch2Tests
{
    [Test]
    public void HelpRowPrefix_TranslatesCategoryAndHelpText_WhenPatched()
    {
        WriteDictionary(
            ("MENU", "メニュー"),
            ("Walk around freely", "自由に歩き回る"));

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);
        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyHelpRowTarget), nameof(DummyHelpRowTarget.setData)),
                prefix: new HarmonyMethod(RequireMethod(typeof(HelpRowTranslationPatch), nameof(HelpRowTranslationPatch.Prefix))));

            var target = new DummyHelpRowTarget();
            target.setData(new DummyHelpDataRowTarget
            {
                Description = "Menu",
                HelpText = "Walk around freely",
                Collapsed = false,
            });

            Assert.Multiple(() =>
            {
                Assert.That(target.categoryDescription.Text, Is.EqualTo("{{C|メニュー}}"));
                Assert.That(target.description.Text, Is.EqualTo("自由に歩き回る"));
                Assert.That(target.description.gameObject.Active, Is.True);
                Assert.That(target.categoryExpander.Text, Is.EqualTo("{{C|[-]}}"));
                Assert.That(
                    DynamicTextObservability.GetRouteFamilyHitCountForTests(
                        nameof(HelpRowTranslationPatch),
                        "HelpRow.CategoryDescription"),
                    Is.GreaterThan(0));
                Assert.That(
                    DynamicTextObservability.GetRouteFamilyHitCountForTests(
                        nameof(HelpRowTranslationPatch),
                        "HelpRow.HelpText"),
                    Is.GreaterThan(0));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void HelpRowPrefix_FallsBackToOriginal_OnUnsupportedInput()
    {
        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);
        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyHelpRowTarget), nameof(DummyHelpRowTarget.setData)),
                prefix: new HarmonyMethod(RequireMethod(typeof(HelpRowTranslationPatch), nameof(HelpRowTranslationPatch.Prefix))));

            var target = new DummyHelpRowTarget();
            target.setData(new DummyFallbackHelpDataRowTarget());

            Assert.That(target.OriginalExecuted, Is.True);
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }
}
