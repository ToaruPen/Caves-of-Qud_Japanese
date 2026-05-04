using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class GameSummaryScreenShowTranslationPatch
{
    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var targetType = GameTypeResolver.FindType("Qud.UI.GameSummaryScreen", "GameSummaryScreen");
        if (targetType is null)
        {
            Trace.TraceError("QudJP: GameSummaryScreenShowTranslationPatch target type not found.");
            return null;
        }

        var method = AccessTools.Method(
            targetType,
            "_ShowGameSummary",
            new[] { typeof(string), typeof(string), typeof(string), typeof(bool) });
        if (method is null)
        {
            Trace.TraceError("QudJP: GameSummaryScreenShowTranslationPatch._ShowGameSummary(string, string, string, bool) not found.");
        }

        return method;
    }

    public static void Prefix(ref string cause, ref string details)
    {
        try
        {
            cause = GameSummaryTextTranslator.TranslateCause(cause);
            details = GameSummaryTextTranslator.TranslateDetails(details);
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: GameSummaryScreenShowTranslationPatch.Prefix failed: {0}", ex);
        }
    }
}
