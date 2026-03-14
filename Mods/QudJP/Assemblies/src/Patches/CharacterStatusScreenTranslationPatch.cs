using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using QudJP;

namespace QudJP.Patches;

[HarmonyPatch]
public static class CharacterStatusScreenTranslationPatch
{
    private static readonly Regex AttributePointsPattern =
        new Regex("^Attribute Points: (?<rest>.+)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex MutationPointsPattern =
        new Regex("^Mutation Points: (?<rest>.+)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var targetType = GameTypeResolver.FindType("Qud.UI.CharacterStatusScreen", "CharacterStatusScreen");
        if (targetType is null)
        {
            Trace.TraceError("QudJP: CharacterStatusScreenTranslationPatch target type not found.");
            return null;
        }

        var method = AccessTools.Method(targetType, "UpdateViewFromData", Type.EmptyTypes);
        if (method is null)
        {
            Trace.TraceError("QudJP: CharacterStatusScreenTranslationPatch.UpdateViewFromData not found.");
        }

        return method;
    }

    public static void Postfix(object? ___attributePointsText, object? ___mutationPointsText)
    {
        try
        {
            UITextSkinTemplateTranslator.TranslateSinglePlaceholderText(
                ___attributePointsText,
                AttributePointsPattern,
                "Attribute Points: {0}",
                "{0}",
                nameof(CharacterStatusScreenTranslationPatch));

            UITextSkinTemplateTranslator.TranslateSinglePlaceholderText(
                ___mutationPointsText,
                MutationPointsPattern,
                "Mutation Points: {0}",
                "{0}",
                nameof(CharacterStatusScreenTranslationPatch));
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: CharacterStatusScreenTranslationPatch.Postfix failed: {0}", ex);
        }
    }
}
