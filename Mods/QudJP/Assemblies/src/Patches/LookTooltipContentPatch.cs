using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace QudJP.Patches;

[HarmonyPatch]
public static class LookTooltipContentPatch
{
    private const string TargetTypeName = "XRL.UI.Look";

    [HarmonyTargetMethod]
    private static MethodBase? TargetMethod()
    {
        var gameObjectType = AccessTools.TypeByName("XRL.World.GameObject");
        if (gameObjectType is not null)
        {
            var method = AccessTools.Method(TargetTypeName + ":GenerateTooltipContent", new[] { gameObjectType });
            if (method is not null)
            {
                return method;
            }
        }

        var targetType = AccessTools.TypeByName(TargetTypeName);
        if (targetType is not null)
        {
            var methods = AccessTools.GetDeclaredMethods(targetType);
            for (var index = 0; index < methods.Count; index++)
            {
                var candidate = methods[index];
                if (string.Equals(candidate.Name, "GenerateTooltipContent", StringComparison.Ordinal)
                    && candidate.ReturnType == typeof(string)
                    && candidate.GetParameters().Length == 1)
                {
                    return candidate;
                }
            }
        }

        Trace.TraceError("QudJP: Failed to resolve Look.GenerateTooltipContent(GameObject). Patch will not apply.");
        return null;
    }

    public static void Postfix(ref string __result)
    {
        try
        {
            if (string.IsNullOrEmpty(__result))
            {
                return;
            }

            __result = UITextSkinTranslationPatch.TranslatePreservingColors(__result, nameof(LookTooltipContentPatch));
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: LookTooltipContentPatch.Postfix failed: {0}", ex);
        }
    }
}
