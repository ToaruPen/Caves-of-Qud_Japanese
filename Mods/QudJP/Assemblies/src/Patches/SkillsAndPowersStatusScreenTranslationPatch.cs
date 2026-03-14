using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using QudJP;

namespace QudJP.Patches;

[HarmonyPatch]
public static class SkillsAndPowersStatusScreenTranslationPatch
{
    private static readonly Regex SkillPointsPattern =
        new Regex("^Skill Points \\(SP\\): (?<rest>.+)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var targetType = GameTypeResolver.FindType("Qud.UI.SkillsAndPowersStatusScreen", "SkillsAndPowersStatusScreen");
        if (targetType is null)
        {
            Trace.TraceError("QudJP: SkillsAndPowersStatusScreenTranslationPatch target type not found.");
            return null;
        }

        var method = AccessTools.Method(targetType, "UpdateViewFromData", Type.EmptyTypes);
        if (method is null)
        {
            Trace.TraceError("QudJP: SkillsAndPowersStatusScreenTranslationPatch.UpdateViewFromData not found.");
        }

        return method;
    }

    public static void Postfix(object? ___spText)
    {
        try
        {
            UITextSkinTemplateTranslator.TranslateSinglePlaceholderText(
                ___spText,
                SkillPointsPattern,
                "Skill Points (SP): {val}",
                "{val}",
                nameof(SkillsAndPowersStatusScreenTranslationPatch));
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: SkillsAndPowersStatusScreenTranslationPatch.Postfix failed: {0}", ex);
        }
    }
}
