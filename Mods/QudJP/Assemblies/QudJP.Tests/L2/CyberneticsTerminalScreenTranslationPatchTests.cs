using HarmonyLib;
using QudJP.Patches;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L2;

public sealed partial class Issue201OtherUiBindingPatchTests
{
    [Test]
    public void CyberneticsTerminalScreenPostfix_TranslatesFooterAndMenuOptions_WhenPatched()
    {
        WriteDictionary(
            ("System ready", "システム準備完了"),
            ("navigate", "移動"),
            ("accept", "決定"),
            ("quit", "終了"));

        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);
        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyCyberneticsTerminalScreenTarget), nameof(DummyCyberneticsTerminalScreenTarget.Show)),
                postfix: new HarmonyMethod(RequireMethod(typeof(CyberneticsTerminalScreenTranslationPatch), nameof(CyberneticsTerminalScreenTranslationPatch.Postfix))));

            var target = new DummyCyberneticsTerminalScreenTarget
            {
                FooterText = "System ready",
            };
            target.Show();

            Assert.Multiple(() =>
            {
                Assert.That(target.footerTextSkin.Text, Is.EqualTo("システム準備完了"));
                Assert.That(target.keyMenuOptions[0].Description, Is.EqualTo("移動"));
                Assert.That(target.keyMenuOptions[1].Description, Is.EqualTo("決定"));
                Assert.That(target.keyMenuOptions[2].Description, Is.EqualTo("終了"));
                Assert.That(
                    DynamicTextObservability.GetRouteFamilyHitCountForTests(nameof(CyberneticsTerminalScreenTranslationPatch), "CyberneticsTerminal.FooterText"),
                    Is.GreaterThan(0));
                Assert.That(
                    DynamicTextObservability.GetRouteFamilyHitCountForTests(nameof(CyberneticsTerminalScreenTranslationPatch), "CyberneticsTerminal.MenuOption"),
                    Is.GreaterThan(0));
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }

    [Test]
    public void CyberneticsTerminalScreenPostfix_LeavesOriginalValues_WhenNoDictionaryEntries()
    {
        var harmonyId = CreateHarmonyId();
        var harmony = new Harmony(harmonyId);
        try
        {
            harmony.Patch(
                original: RequireMethod(typeof(DummyCyberneticsTerminalScreenTarget), nameof(DummyCyberneticsTerminalScreenTarget.Show)),
                postfix: new HarmonyMethod(RequireMethod(typeof(CyberneticsTerminalScreenTranslationPatch), nameof(CyberneticsTerminalScreenTranslationPatch.Postfix))));

            var target = new DummyCyberneticsTerminalScreenTarget
            {
                FooterText = "System ready",
            };
            target.Show();

            Assert.Multiple(() =>
            {
                Assert.That(target.footerTextSkin.Text, Is.EqualTo("System ready"));
                Assert.That(target.keyMenuOptions[0].Description, Is.EqualTo("navigate"));
                Assert.That(target.keyMenuOptions[1].Description, Is.EqualTo("accept"));
                Assert.That(target.keyMenuOptions[2].Description, Is.EqualTo("quit"));
                Assert.That(target.OriginalExecuted, Is.True);
            });
        }
        finally
        {
            harmony.UnpatchAll(harmonyId);
        }
    }
}
