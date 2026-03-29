using HarmonyLib;
using QudJP.Patches;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L2;

public sealed partial class Issue201StatusScreensBatch2Tests
{
    [Test]
    public void KeybindRowPrefix_TranslatesDataRowAndCategoryRow_WhenPatched()
    {
        WriteDictionary(
            ("Interact Nearby", "近くと交互作用"),
            ("MOVEMENT", "移動"),
            ("None", "なし"));

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);
        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyKeybindRowTarget), nameof(DummyKeybindRowTarget.setData)),
                prefix: new HarmonyMethod(RequireMethod(typeof(KeybindRowTranslationPatch), nameof(KeybindRowTranslationPatch.Prefix))));

            var dataRowTarget = new DummyKeybindRowTarget();
            dataRowTarget.setData(new DummyKeybindDataRowTarget
            {
                CategoryId = "General",
                KeyId = "InteractNearby",
                KeyDescription = "Interact Nearby",
                SearchWords = "General Interact Nearby",
            });

            var categoryTarget = new DummyKeybindRowTarget();
            categoryTarget.setData(new DummyKeybindCategoryRowTarget
            {
                CategoryId = "Movement",
                CategoryDescription = "Movement",
                Collapsed = true,
            });

            Assert.Multiple(() =>
            {
                Assert.That(dataRowTarget.description.Text, Is.EqualTo("{{C|近くと交互作用}}"));
                Assert.That(dataRowTarget.box1.boxText, Is.EqualTo("{{K|なし}}"));
                Assert.That(dataRowTarget.box1.forceUpdate, Is.True);
                Assert.That(dataRowTarget.bindingDisplay.Active, Is.True);
                Assert.That(dataRowTarget.categoryDisplay.Active, Is.False);
                Assert.That(categoryTarget.categoryDescription.Text, Is.EqualTo("{{C|移動}}"));
                Assert.That(categoryTarget.categoryExpander.Text, Is.EqualTo("{{C|[+]}}"));
                Assert.That(categoryTarget.categoryDisplay.Active, Is.True);
                Assert.That(categoryTarget.bindingDisplay.Active, Is.False);
                Assert.That(
                    DynamicTextObservability.GetRouteFamilyHitCountForTests(
                        nameof(KeybindRowTranslationPatch),
                        "KeybindRow.KeyDescription"),
                    Is.GreaterThan(0));
                Assert.That(
                    DynamicTextObservability.GetRouteFamilyHitCountForTests(
                        nameof(KeybindRowTranslationPatch),
                        "KeybindRow.CategoryDescription"),
                    Is.GreaterThan(0));
                Assert.That(
                    DynamicTextObservability.GetRouteFamilyHitCountForTests(
                        nameof(KeybindRowTranslationPatch),
                        "KeybindRow.NoneBinding"),
                    Is.GreaterThan(0));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void KeybindRowPrefix_FallsBackToOriginal_OnUnsupportedInput()
    {
        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);
        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyKeybindRowTarget), nameof(DummyKeybindRowTarget.setData)),
                prefix: new HarmonyMethod(RequireMethod(typeof(KeybindRowTranslationPatch), nameof(KeybindRowTranslationPatch.Prefix))));

            var target = new DummyKeybindRowTarget();
            target.setData(new DummyFallbackKeybindRowDataTarget());

            Assert.That(target.OriginalExecuted, Is.True);
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }
}
