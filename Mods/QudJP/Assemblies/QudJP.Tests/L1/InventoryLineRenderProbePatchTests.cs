using System.IO;

namespace QudJP.Tests.L1;

[NUnit.Framework.TestFixture]
[NUnit.Framework.Category("L1")]
public sealed class InventoryLineRenderProbePatchTests
{
    [NUnit.Framework.Test]
    public void InventoryLineRenderProbePatch_DoesNotScheduleReplacementOverlay()
    {
        var sourcePath = Path.Combine(
            TestProjectPaths.GetRepositoryRoot(),
            "Mods",
            "QudJP",
            "Assemblies",
            "src",
            "Patches",
            "InventoryLineRenderProbePatch.cs");
        var source = File.ReadAllText(sourcePath);

        NUnit.Framework.Assert.That(
            source,
            NUnit.Framework.Does.Not.Contain("DelayedInventoryLineRepairScheduler.ScheduleRepair("));
    }

    [NUnit.Framework.Test]
    public void InventoryLineTranslationPatch_ForcesPrimaryFontAfterFinalItemText()
    {
        var sourcePath = Path.Combine(
            TestProjectPaths.GetRepositoryRoot(),
            "Mods",
            "QudJP",
            "Assemblies",
            "src",
            "Patches",
            "InventoryLineTranslationPatch.cs");
        var source = File.ReadAllText(sourcePath);

        NUnit.Framework.Assert.That(
            source,
            NUnit.Framework.Does.Contain("InventoryLineFontFixer.TryForcePrimaryFontOnTextSkin(itemTextSkin, translatedDisplayName)"));
    }

    [NUnit.Framework.Test]
    public void InventoryLineActiveTextRefreshPatch_RefreshesAfterLineBecomesActive()
    {
        var sourcePath = Path.Combine(
            TestProjectPaths.GetRepositoryRoot(),
            "Mods",
            "QudJP",
            "Assemblies",
            "src",
            "Patches",
            "InventoryLineActiveTextRefreshPatch.cs");

        NUnit.Framework.Assert.That(File.Exists(sourcePath), NUnit.Framework.Is.True);
        var source = File.ReadAllText(sourcePath);

        NUnit.Framework.Assert.That(source, NUnit.Framework.Does.Contain("\"LateUpdate\""));
        NUnit.Framework.Assert.That(source, NUnit.Framework.Does.Contain("InventoryLineFontFixer.IsActiveItemLine(__instance)"));
        NUnit.Framework.Assert.That(source, NUnit.Framework.Does.Contain("InventoryLineFontFixer.HasActiveReplacementForCurrentItemText(__instance)"));
        NUnit.Framework.Assert.That(source, NUnit.Framework.Does.Contain("InventoryLineFontFixer.TryRefreshActiveItemLine(__instance)"));
        NUnit.Framework.Assert.That(source, NUnit.Framework.Does.Contain("DelayedInventoryLineRepairScheduler.ScheduleRepairForCurrentText("));
        NUnit.Framework.Assert.That(source, NUnit.Framework.Does.Not.Contain("TextShellReplacementRenderer"));
    }

    [NUnit.Framework.Test]
    public void InventoryLineFontFixer_TreatsZeroCharactersAsRefreshFailure()
    {
        var sourcePath = Path.Combine(
            TestProjectPaths.GetRepositoryRoot(),
            "Mods",
            "QudJP",
            "Assemblies",
            "src",
            "UI",
            "InventoryLineFontFixer.cs");
        var source = File.ReadAllText(sourcePath);

        NUnit.Framework.Assert.That(
            source,
            NUnit.Framework.Does.Contain("return tmp.textInfo.characterCount > 0;"));
    }

    [NUnit.Framework.Test]
    public void DelayedInventoryLineRepairScheduler_RearmsOnlyWhenLineTextChanges()
    {
        var sourcePath = Path.Combine(
            TestProjectPaths.GetRepositoryRoot(),
            "Mods",
            "QudJP",
            "Assemblies",
            "src",
            "UI",
            "DelayedInventoryLineRepairScheduler.cs");
        var source = File.ReadAllText(sourcePath);

        NUnit.Framework.Assert.That(source, NUnit.Framework.Does.Contain("LastScheduledTextByLine"));
        NUnit.Framework.Assert.That(source, NUnit.Framework.Does.Contain("ScheduleRepairForCurrentText"));
        NUnit.Framework.Assert.That(source, NUnit.Framework.Does.Contain("AttemptCounts.TryRemove(lineId, out _)"));
        NUnit.Framework.Assert.That(source, NUnit.Framework.Does.Not.Contain("ScheduleRepair(__instance, resetAttempts: true)"));
    }
}
